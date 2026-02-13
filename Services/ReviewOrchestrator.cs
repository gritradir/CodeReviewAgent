using System.Text;
using CodeReviewAgent.Configuration;
using CodeReviewAgent.Models.Internal;
using CodeReviewAgent.Models.Review;
using Microsoft.Extensions.Options;

namespace CodeReviewAgent.Services;

public class ReviewOrchestrator : IReviewOrchestrator
{
    private readonly IGitService _gitService;
    private readonly IWorkItemService _workItemService;
    private readonly IReviewEngineService _reviewEngine;
    private readonly IReviewStorageService _storageService;
    private readonly CodeReviewOptions _options;
    private readonly ILogger<ReviewOrchestrator> _logger;

    private readonly string? _systemInstructions;

    public ReviewOrchestrator(
        IGitService gitService,
        IWorkItemService workItemService,
        IReviewEngineService reviewEngine,
        IReviewStorageService storageService,
        IOptions<CodeReviewOptions> options,
        ILogger<ReviewOrchestrator> logger)
    {
        _gitService = gitService;
        _workItemService = workItemService;
        _reviewEngine = reviewEngine;
        _storageService = storageService;
        _options = options.Value;
        _logger = logger;

        // Load review instructions file once at startup
        _systemInstructions = LoadInstructions();
    }

    private string? LoadInstructions()
    {
        var path = _options.ReviewInstructionsPath;
        if (string.IsNullOrWhiteSpace(path))
            return null;

        // Resolve relative paths against the app directory
        if (!Path.IsPathRooted(path))
            path = Path.Combine(AppContext.BaseDirectory, path);

        // Also check relative to the content root (project directory)
        if (!File.Exists(path))
        {
            var altPath = Path.Combine(Directory.GetCurrentDirectory(), _options.ReviewInstructionsPath);
            if (File.Exists(altPath))
                path = altPath;
        }

        if (!File.Exists(path))
        {
            _logger.LogWarning("Review instructions file not found: {Path}", _options.ReviewInstructionsPath);
            return null;
        }

        _logger.LogInformation("Loaded review instructions from {Path}", path);
        return File.ReadAllText(path);
    }

    public async Task<CodeReviewResult> ProcessPullRequestAsync(PullRequestInfo prInfo)
    {
        var branchName = prInfo.SourceBranch.Replace("refs/heads/", "");

        _logger.LogInformation("Processing PR #{PrId} from branch {Branch} in repo {Repo}",
            prInfo.PullRequestId, branchName, prInfo.RepoName);

        var result = new CodeReviewResult
        {
            PullRequestId = prInfo.PullRequestId,
            BranchName = branchName,
        };

        try
        {
            // Step 1: Extract work item key from branch name
            var workItemKey = _workItemService.ExtractWorkItemKey(branchName);
            result.WorkItemKey = workItemKey;

            // Step 2: Fetch work item details
            var workItemContext = string.Empty;
            if (!string.IsNullOrEmpty(workItemKey))
            {
                _logger.LogInformation("Found work item: {WorkItem}", workItemKey);
                workItemContext = await _workItemService.GetWorkItemContextAsync(workItemKey) ?? string.Empty;
                if (string.IsNullOrEmpty(workItemContext))
                    _logger.LogWarning("Could not fetch work item {WorkItem}, proceeding without it", workItemKey);
            }
            else
            {
                _logger.LogInformation("No work item found in branch name: {Branch}", branchName);
            }

            // Step 3: Checkout the PR branch (clone URL already has credentials injected)
            var repoPath = await _gitService.CheckoutBranchAsync(
                prInfo.RepoName, prInfo.AuthenticatedCloneUrl, prInfo.SourceBranch);

            // Step 4: Get the diff
            var diff = await _gitService.GetDiffAsync(repoPath, prInfo.TargetBranch, branchName);

            if (string.IsNullOrWhiteSpace(diff))
            {
                _logger.LogWarning("Empty diff for PR #{PrId}", prInfo.PullRequestId);
                result.ReviewContent = "No changes detected in the diff.";
                result.IsSuccess = true;
                result.FilePath = await _storageService.SaveReviewAsync(result);
                return result;
            }

            // Step 5: Build the review prompt and run review engine
            var prompt = BuildReviewPrompt(prInfo, workItemContext, diff);
            var reviewContent = await _reviewEngine.RunReviewAsync(prompt, repoPath, _systemInstructions);
            result.ReviewContent = reviewContent;
            result.IsSuccess = true;

            // Step 6: Save the review
            result.FilePath = await _storageService.SaveReviewAsync(result);

            _logger.LogInformation("Review completed for PR #{PrId}, saved to {Path}",
                prInfo.PullRequestId, result.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process PR #{PrId}: {Message}",
                prInfo.PullRequestId, ex.Message);
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private static string BuildReviewPrompt(PullRequestInfo pr, string workItemContext, string diff)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are performing a code review on a pull request. Provide a thorough, structured review.");
        sb.AppendLine();
        sb.AppendLine("## Pull Request Information");
        sb.AppendLine($"- **Title:** {pr.Title}");
        sb.AppendLine($"- **PR #:** {pr.PullRequestId}");
        sb.AppendLine($"- **Author:** {pr.AuthorName}");
        sb.AppendLine($"- **Source Branch:** {pr.SourceBranch}");
        sb.AppendLine($"- **Target Branch:** {pr.TargetBranch}");

        if (!string.IsNullOrEmpty(pr.Description))
            sb.AppendLine($"\n**PR Description:**\n{pr.Description}");

        if (!string.IsNullOrEmpty(workItemContext))
        {
            sb.AppendLine();
            sb.AppendLine(workItemContext);
        }

        sb.AppendLine();
        sb.AppendLine("## Code Changes (git diff)");
        sb.AppendLine("```diff");

        const int maxDiffLength = 100_000;
        if (diff.Length > maxDiffLength)
        {
            sb.AppendLine(diff[..maxDiffLength]);
            sb.AppendLine("... [diff truncated due to size]");
        }
        else
        {
            sb.AppendLine(diff);
        }

        sb.AppendLine("```");

        return sb.ToString();
    }
}

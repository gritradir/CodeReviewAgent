using CodeReviewAgent.Configuration;
using CodeReviewAgent.Models.AzureDevOps;
using CodeReviewAgent.Models.Internal;
using CodeReviewAgent.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CodeReviewAgent.Controllers;

[Route("api/webhook/azuredevops")]
[ApiController]
public class AzureDevOpsWebhookController : ControllerBase
{
    private readonly IReviewOrchestrator _orchestrator;
    private readonly CodeReviewOptions _options;
    private readonly AzureDevOpsOptions _adoOptions;
    private readonly ILogger<AzureDevOpsWebhookController> _logger;

    public AzureDevOpsWebhookController(
        IReviewOrchestrator orchestrator,
        IOptions<CodeReviewOptions> options,
        IOptions<AzureDevOpsOptions> adoOptions,
        ILogger<AzureDevOpsWebhookController> logger)
    {
        _orchestrator = orchestrator;
        _options = options.Value;
        _adoOptions = adoOptions.Value;
        _logger = logger;
    }

    [HttpPost]
    public IActionResult HandlePullRequest([FromBody] PullRequestWebhookPayload payload)
    {
        if (!string.IsNullOrEmpty(_options.WebhookSecret))
        {
            if (!Request.Headers.TryGetValue("X-Webhook-Secret", out var secret)
                || secret != _options.WebhookSecret)
            {
                _logger.LogWarning("Azure DevOps webhook received with invalid or missing secret");
                return Unauthorized();
            }
        }

        if (payload.EventType != "git.pullrequest.created")
        {
            _logger.LogInformation("Ignoring event type: {EventType}", payload.EventType);
            return Ok(new { status = "ignored", reason = $"Event type '{payload.EventType}' is not handled" });
        }

        var author = payload.Resource.CreatedBy;
        var isAllowed = _options.AllowedAuthors.Any(a =>
            a.Equals(author.DisplayName, StringComparison.OrdinalIgnoreCase) ||
            a.Equals(author.UniqueName, StringComparison.OrdinalIgnoreCase));

        if (!isAllowed)
        {
            _logger.LogInformation("PR by {Author} not in allowed list, skipping", author.DisplayName);
            return Ok(new { status = "skipped", reason = $"Author '{author.DisplayName}' not in allowed list" });
        }

        var resource = payload.Resource;
        var remoteUrl = resource.Repository.RemoteUrl ?? resource.Repository.Url;

        var prInfo = new PullRequestInfo
        {
            PullRequestId = resource.PullRequestId,
            Title = resource.Title,
            Description = resource.Description,
            AuthorName = author.DisplayName,
            AuthorIdentifier = author.UniqueName,
            SourceBranch = resource.SourceRefName,
            TargetBranch = resource.TargetRefName,
            RepoName = resource.Repository.Name,
            AuthenticatedCloneUrl = InjectPat(remoteUrl),
        };

        _logger.LogInformation("Processing Azure DevOps PR #{PrId} by {Author}",
            prInfo.PullRequestId, prInfo.AuthorName);

        _ = Task.Run(async () =>
        {
            try { await _orchestrator.ProcessPullRequestAsync(prInfo); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background review failed for PR #{PrId}", prInfo.PullRequestId);
            }
        });

        return Accepted(new { status = "accepted", pullRequestId = prInfo.PullRequestId });
    }

    private string InjectPat(string remoteUrl)
    {
        if (remoteUrl.StartsWith("https://"))
            return remoteUrl.Replace("https://", $"https://{_adoOptions.Pat}@");
        return remoteUrl;
    }
}

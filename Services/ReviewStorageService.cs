using CodeReviewAgent.Configuration;
using CodeReviewAgent.Models.Review;
using Microsoft.Extensions.Options;

namespace CodeReviewAgent.Services;

public class ReviewStorageService : IReviewStorageService
{
    private readonly string _outputPath;
    private readonly ILogger<ReviewStorageService> _logger;

    public ReviewStorageService(IOptions<CodeReviewOptions> options, ILogger<ReviewStorageService> logger)
    {
        _logger = logger;

        var configured = options.Value.ReviewsOutputPath;

        if (string.IsNullOrWhiteSpace(configured))
        {
            _outputPath = Path.Combine(AppContext.BaseDirectory, "reviews");
        }
        else if (!Path.IsPathRooted(configured))
        {
            // Try relative to current directory first, fall back to app base
            var candidate = Path.Combine(Directory.GetCurrentDirectory(), configured);
            _outputPath = candidate;
        }
        else
        {
            _outputPath = configured;
        }

        _logger.LogInformation("Reviews will be saved to {OutputPath}", _outputPath);
    }

    public async Task<string> SaveReviewAsync(CodeReviewResult review)
    {
        Directory.CreateDirectory(_outputPath);

        var ticketPart = review.WorkItemKey ?? "no-ticket";
        var datePart = review.CreatedAt.ToString("yyyy-MM-dd_HHmmss");
        var fileName = $"{datePart}_PR-{review.PullRequestId}_{ticketPart}.md";
        var filePath = Path.Combine(_outputPath, fileName);

        var content = $"""
            # Code Review: PR #{review.PullRequestId}

            - **Branch:** {review.BranchName}
            - **Work Item:** {review.WorkItemKey ?? "N/A"}
            - **Reviewed at:** {review.CreatedAt:yyyy-MM-dd HH:mm:ss UTC}

            ---

            {review.ReviewContent}
            """;

        await File.WriteAllTextAsync(filePath, content);
        _logger.LogInformation("Review saved to {FilePath}", filePath);
        return filePath;
    }
}

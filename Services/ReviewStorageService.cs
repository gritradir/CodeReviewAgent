using CodeReviewAgent.Configuration;
using CodeReviewAgent.Models.Review;
using Microsoft.Extensions.Options;

namespace CodeReviewAgent.Services;

public class ReviewStorageService : IReviewStorageService
{
    private readonly CodeReviewOptions _options;
    private readonly ILogger<ReviewStorageService> _logger;

    public ReviewStorageService(IOptions<CodeReviewOptions> options, ILogger<ReviewStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> SaveReviewAsync(CodeReviewResult review)
    {
        Directory.CreateDirectory(_options.ReviewsOutputPath);

        var ticketPart = review.WorkItemKey ?? "no-ticket";
        var datePart = review.CreatedAt.ToString("yyyy-MM-dd_HHmmss");
        var fileName = $"{datePart}_PR-{review.PullRequestId}_{ticketPart}.md";
        var filePath = Path.Combine(_options.ReviewsOutputPath, fileName);

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

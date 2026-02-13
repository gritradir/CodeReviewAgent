namespace CodeReviewAgent.Models.Review;

public class CodeReviewResult
{
    public int PullRequestId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string? WorkItemKey { get; set; }
    public string ReviewContent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string FilePath { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

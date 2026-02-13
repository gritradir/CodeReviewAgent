namespace CodeReviewAgent.Models.Internal;

public class PullRequestInfo
{
    public int PullRequestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorIdentifier { get; set; } = string.Empty;
    public string SourceBranch { get; set; } = string.Empty;
    public string TargetBranch { get; set; } = string.Empty;
    public string RepoName { get; set; } = string.Empty;

    /// <summary>
    /// Clone URL with authentication already embedded (e.g. https://pat@github.com/owner/repo.git).
    /// The controller is responsible for injecting the appropriate credentials.
    /// </summary>
    public string AuthenticatedCloneUrl { get; set; } = string.Empty;
}

namespace CodeReviewAgent.Services;

public interface IWorkItemService
{
    /// <summary>
    /// Extracts a work item key/ID from a branch name.
    /// Returns null if no work item identifier is found.
    /// </summary>
    string? ExtractWorkItemKey(string branchName);

    /// <summary>
    /// Fetches the work item and returns a formatted context string for the review prompt.
    /// Returns null if the work item cannot be fetched.
    /// </summary>
    Task<string?> GetWorkItemContextAsync(string workItemKey);
}

namespace CodeReviewAgent.Services;

public interface IGitService
{
    Task<string> CheckoutBranchAsync(string repoName, string remoteUrl, string branchName);
    Task<string> GetDiffAsync(string repoPath, string targetBranch, string sourceBranch);
}

using CodeReviewAgent.Models.Internal;
using CodeReviewAgent.Models.Review;

namespace CodeReviewAgent.Services;

public interface IReviewOrchestrator
{
    Task<CodeReviewResult> ProcessPullRequestAsync(PullRequestInfo prInfo);
}

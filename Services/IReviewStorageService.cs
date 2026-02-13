using CodeReviewAgent.Models.Review;

namespace CodeReviewAgent.Services;

public interface IReviewStorageService
{
    Task<string> SaveReviewAsync(CodeReviewResult review);
}

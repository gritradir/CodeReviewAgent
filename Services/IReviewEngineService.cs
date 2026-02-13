namespace CodeReviewAgent.Services;

public interface IReviewEngineService
{
    Task<string> RunReviewAsync(string prompt, string workingDirectory, string? systemInstructions = null);
}

namespace CodeReviewAgent.Configuration;

public class CodeReviewOptions
{
    public const string SectionName = "CodeReview";

    public TaskSystemType TaskSystem { get; set; } = TaskSystemType.Jira;
    public ReviewEngineType ReviewEngine { get; set; } = ReviewEngineType.Claude;
    public List<string> AllowedAuthors { get; set; } = new();
    public string RepoBasePath { get; set; } = string.Empty;
    public int RepoRetentionDays { get; set; } = 14;
    public string ReviewInstructionsPath { get; set; } = "review-instructions.md";
    public string ReviewsOutputPath { get; set; } = "reviews";
    public string? WebhookSecret { get; set; }
}

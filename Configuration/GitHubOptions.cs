namespace CodeReviewAgent.Configuration;

public class GitHubOptions
{
    public const string SectionName = "GitHub";

    public string Pat { get; set; } = string.Empty;
    public string? WebhookSecret { get; set; }
}

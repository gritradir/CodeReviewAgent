namespace CodeReviewAgent.Configuration;

public class JiraOptions
{
    public const string SectionName = "Jira";

    public string Domain { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
}

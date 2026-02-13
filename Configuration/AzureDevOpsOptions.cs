namespace CodeReviewAgent.Configuration;

public class AzureDevOpsOptions
{
    public const string SectionName = "AzureDevOps";

    public string Pat { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
}

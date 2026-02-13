using Newtonsoft.Json;

namespace CodeReviewAgent.Models.Jira;

public class JiraIssue
{
    [JsonProperty("key")]
    public string Key { get; set; } = string.Empty;

    [JsonProperty("fields")]
    public JiraIssueFields Fields { get; set; } = new();
}

public class JiraIssueFields
{
    [JsonProperty("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonProperty("description")]
    public object? Description { get; set; }

    [JsonProperty("status")]
    public JiraStatus? Status { get; set; }

    [JsonProperty("issuetype")]
    public JiraIssueType? IssueType { get; set; }

    [JsonProperty("priority")]
    public JiraPriority? Priority { get; set; }

    [JsonProperty("customfield_10037")]
    public object? AcceptanceCriteria { get; set; }
}

public class JiraStatus
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
}

public class JiraIssueType
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
}

public class JiraPriority
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
}

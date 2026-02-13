using Newtonsoft.Json;

namespace CodeReviewAgent.Models.AzureDevOps;

public class PullRequestWebhookPayload
{
    [JsonProperty("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonProperty("resource")]
    public PullRequestResource Resource { get; set; } = new();
}

public class PullRequestResource
{
    [JsonProperty("pullRequestId")]
    public int PullRequestId { get; set; }

    [JsonProperty("sourceRefName")]
    public string SourceRefName { get; set; } = string.Empty;

    [JsonProperty("targetRefName")]
    public string TargetRefName { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("createdBy")]
    public PullRequestCreatedBy CreatedBy { get; set; } = new();

    [JsonProperty("repository")]
    public PullRequestRepository Repository { get; set; } = new();
}

public class PullRequestCreatedBy
{
    [JsonProperty("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonProperty("uniqueName")]
    public string UniqueName { get; set; } = string.Empty;
}

public class PullRequestRepository
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;

    [JsonProperty("remoteUrl")]
    public string? RemoteUrl { get; set; }
}

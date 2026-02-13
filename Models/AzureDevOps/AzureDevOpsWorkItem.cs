using Newtonsoft.Json;

namespace CodeReviewAgent.Models.AzureDevOps;

public class AzureDevOpsWorkItem
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("fields")]
    public AzureDevOpsWorkItemFields Fields { get; set; } = new();
}

public class AzureDevOpsWorkItemFields
{
    [JsonProperty("System.Title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("System.Description")]
    public string? Description { get; set; }

    [JsonProperty("System.WorkItemType")]
    public string? WorkItemType { get; set; }

    [JsonProperty("System.State")]
    public string? State { get; set; }

    [JsonProperty("Microsoft.VSTS.Common.Priority")]
    public int? Priority { get; set; }

    [JsonProperty("Microsoft.VSTS.Common.AcceptanceCriteria")]
    public string? AcceptanceCriteria { get; set; }
}

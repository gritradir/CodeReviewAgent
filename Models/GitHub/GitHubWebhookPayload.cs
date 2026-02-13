using Newtonsoft.Json;

namespace CodeReviewAgent.Models.GitHub;

public class GitHubWebhookPayload
{
    [JsonProperty("action")]
    public string Action { get; set; } = string.Empty;

    [JsonProperty("pull_request")]
    public GitHubPullRequest PullRequest { get; set; } = new();
}

public class GitHubPullRequest
{
    [JsonProperty("number")]
    public int Number { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("body")]
    public string? Body { get; set; }

    [JsonProperty("head")]
    public GitHubBranchRef Head { get; set; } = new();

    [JsonProperty("base")]
    public GitHubBranchRef Base { get; set; } = new();

    [JsonProperty("user")]
    public GitHubUser User { get; set; } = new();
}

public class GitHubBranchRef
{
    [JsonProperty("ref")]
    public string Ref { get; set; } = string.Empty;

    [JsonProperty("repo")]
    public GitHubRepository? Repo { get; set; }
}

public class GitHubRepository
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("clone_url")]
    public string CloneUrl { get; set; } = string.Empty;
}

public class GitHubUser
{
    [JsonProperty("login")]
    public string Login { get; set; } = string.Empty;
}

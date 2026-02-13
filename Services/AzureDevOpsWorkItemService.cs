using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using CodeReviewAgent.Configuration;
using CodeReviewAgent.Models.AzureDevOps;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CodeReviewAgent.Services;

public partial class AzureDevOpsWorkItemService : IWorkItemService
{
    private readonly AzureDevOpsOptions _options;
    private readonly ILogger<AzureDevOpsWorkItemService> _logger;
    private readonly HttpClient _httpClient;

    // Matches: feature/12345, bugfix/12345-some-description
    [GeneratedRegex(@"(?:feature|bugfix|hotfix|task|improvement)/(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex WorkItemIdRegex();

    public AzureDevOpsWorkItemService(IOptions<AzureDevOpsOptions> options, ILogger<AzureDevOpsWorkItemService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = new HttpClient();

        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_options.Pat}"));
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
        _httpClient.DefaultRequestHeaders.Accept
            .Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public string? ExtractWorkItemKey(string branchName)
    {
        var match = WorkItemIdRegex().Match(branchName);
        return match.Success ? match.Groups[1].Value : null;
    }

    public async Task<string?> GetWorkItemContextAsync(string workItemKey)
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/{_options.Project}/_apis/wit/workitems/{workItemKey}?api-version=7.0";
        _logger.LogInformation("Fetching Azure DevOps work item #{WorkItemId}", workItemKey);

        try
        {
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch work item #{WorkItemId}: {StatusCode}",
                    workItemKey, response.StatusCode);
                return null;
            }

            var workItem = JsonConvert.DeserializeObject<AzureDevOpsWorkItem>(content);
            if (workItem == null) return null;

            return FormatWorkItem(workItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception fetching work item #{WorkItemId}", workItemKey);
            return null;
        }
    }

    private static string FormatWorkItem(AzureDevOpsWorkItem workItem)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"## Azure DevOps Work Item: #{workItem.Id}");
        sb.AppendLine($"**Title:** {workItem.Fields.Title}");

        if (!string.IsNullOrEmpty(workItem.Fields.WorkItemType))
            sb.AppendLine($"**Type:** {workItem.Fields.WorkItemType}");
        if (workItem.Fields.Priority.HasValue)
            sb.AppendLine($"**Priority:** {workItem.Fields.Priority}");
        if (!string.IsNullOrEmpty(workItem.Fields.State))
            sb.AppendLine($"**State:** {workItem.Fields.State}");

        if (!string.IsNullOrEmpty(workItem.Fields.Description))
        {
            // ADO descriptions are HTML — strip tags for the prompt
            var plainText = StripHtml(workItem.Fields.Description);
            sb.AppendLine($"\n**Description:**\n{plainText}");
        }

        if (!string.IsNullOrEmpty(workItem.Fields.AcceptanceCriteria))
        {
            var plainText = StripHtml(workItem.Fields.AcceptanceCriteria);
            sb.AppendLine($"\n**Acceptance Criteria:**\n{plainText}");
        }

        return sb.ToString();
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    private static string StripHtml(string html)
    {
        var text = HtmlTagRegex().Replace(html, " ");
        // Collapse whitespace
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return System.Net.WebUtility.HtmlDecode(text);
    }
}

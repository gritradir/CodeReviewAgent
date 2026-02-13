using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using CodeReviewAgent.Configuration;
using CodeReviewAgent.Models.Jira;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodeReviewAgent.Services;

public partial class JiraWorkItemService : IWorkItemService
{
    private readonly JiraOptions _options;
    private readonly ILogger<JiraWorkItemService> _logger;
    private readonly HttpClient _httpClient;

    [GeneratedRegex(@"(?:feature|bugfix|hotfix|task|improvement)/([A-Z]+-\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex JiraTicketRegex();

    public JiraWorkItemService(IOptions<JiraOptions> options, ILogger<JiraWorkItemService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = new HttpClient();

        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{_options.Email}:{_options.ApiToken}"));
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
        _httpClient.DefaultRequestHeaders.Accept
            .Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public string? ExtractWorkItemKey(string branchName)
    {
        var match = JiraTicketRegex().Match(branchName);
        return match.Success ? match.Groups[1].Value : null;
    }

    public async Task<string?> GetWorkItemContextAsync(string workItemKey)
    {
        var url = $"https://{_options.Domain}.atlassian.net/rest/api/3/issue/{workItemKey}";
        _logger.LogInformation("Fetching JIRA issue {IssueKey}", workItemKey);

        try
        {
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch JIRA issue {IssueKey}: {StatusCode}",
                    workItemKey, response.StatusCode);
                return null;
            }

            var issue = JsonConvert.DeserializeObject<JiraIssue>(content);
            if (issue == null) return null;

            return FormatIssue(issue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception fetching JIRA issue {IssueKey}", workItemKey);
            return null;
        }
    }

    private string FormatIssue(JiraIssue issue)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"## JIRA Ticket: {issue.Key}");
        sb.AppendLine($"**Summary:** {issue.Fields.Summary}");

        if (issue.Fields.IssueType != null)
            sb.AppendLine($"**Type:** {issue.Fields.IssueType.Name}");
        if (issue.Fields.Priority != null)
            sb.AppendLine($"**Priority:** {issue.Fields.Priority.Name}");
        if (issue.Fields.Status != null)
            sb.AppendLine($"**Status:** {issue.Fields.Status.Name}");

        if (issue.Fields.Description != null)
        {
            var descriptionText = ExtractTextFromAdf(issue.Fields.Description);
            sb.AppendLine($"\n**Description:**\n{descriptionText}");
        }

        if (issue.Fields.AcceptanceCriteria != null)
        {
            var acText = ExtractTextFromAdf(issue.Fields.AcceptanceCriteria);
            sb.AppendLine($"\n**Acceptance Criteria:**\n{acText}");
        }

        return sb.ToString();
    }

    private static string ExtractTextFromAdf(object adfObject)
    {
        if (adfObject is string str)
            return str;

        try
        {
            var token = JToken.FromObject(adfObject);
            var texts = new List<string>();
            ExtractTextRecursive(token, texts);
            return string.Join("\n", texts);
        }
        catch
        {
            return adfObject?.ToString() ?? string.Empty;
        }
    }

    private static void ExtractTextRecursive(JToken token, List<string> texts)
    {
        if (token is JObject obj)
        {
            if (obj["type"]?.ToString() == "text" && obj["text"] != null)
                texts.Add(obj["text"]!.ToString());

            if (obj["content"] is JArray contentArray)
                foreach (var child in contentArray)
                    ExtractTextRecursive(child, texts);
        }
        else if (token is JArray arr)
        {
            foreach (var child in arr)
                ExtractTextRecursive(child, texts);
        }
    }
}

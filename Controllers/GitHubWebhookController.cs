using System.Security.Cryptography;
using System.Text;
using CodeReviewAgent.Configuration;
using CodeReviewAgent.Models.GitHub;
using CodeReviewAgent.Models.Internal;
using CodeReviewAgent.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CodeReviewAgent.Controllers;

[Route("api/webhook/github")]
[ApiController]
public class GitHubWebhookController : ControllerBase
{
    private readonly IReviewOrchestrator _orchestrator;
    private readonly CodeReviewOptions _options;
    private readonly GitHubOptions _githubOptions;
    private readonly ILogger<GitHubWebhookController> _logger;

    public GitHubWebhookController(
        IReviewOrchestrator orchestrator,
        IOptions<CodeReviewOptions> options,
        IOptions<GitHubOptions> githubOptions,
        ILogger<GitHubWebhookController> logger)
    {
        _orchestrator = orchestrator;
        _options = options.Value;
        _githubOptions = githubOptions.Value;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        // Read raw body for signature validation
        var body = await new StreamReader(Request.Body).ReadToEndAsync();

        // Validate HMAC-SHA256 signature if secret is configured
        if (!string.IsNullOrEmpty(_githubOptions.WebhookSecret))
        {
            if (!Request.Headers.TryGetValue("X-Hub-Signature-256", out var signatureHeader)
                || !ValidateSignature(body, signatureHeader!, _githubOptions.WebhookSecret))
            {
                _logger.LogWarning("GitHub webhook received with invalid signature");
                return Unauthorized();
            }
        }

        // Only handle pull_request events
        var eventType = Request.Headers["X-GitHub-Event"].FirstOrDefault();
        if (eventType != "pull_request")
        {
            _logger.LogInformation("Ignoring GitHub event: {EventType}", eventType);
            return Ok(new { status = "ignored", reason = $"Event '{eventType}' is not handled" });
        }

        var payload = Newtonsoft.Json.JsonConvert.DeserializeObject<GitHubWebhookPayload>(body);
        if (payload == null)
            return BadRequest(new { status = "error", reason = "Could not parse payload" });

        // Only handle opened PRs
        if (payload.Action != "opened")
        {
            _logger.LogInformation("Ignoring PR action: {Action}", payload.Action);
            return Ok(new { status = "ignored", reason = $"Action '{payload.Action}' is not handled" });
        }

        // Filter by author
        var author = payload.PullRequest.User.Login;
        var isAllowed = _options.AllowedAuthors.Any(a =>
            a.Equals(author, StringComparison.OrdinalIgnoreCase));

        if (!isAllowed)
        {
            _logger.LogInformation("PR by {Author} not in allowed list, skipping", author);
            return Ok(new { status = "skipped", reason = $"Author '{author}' not in allowed list" });
        }

        var pr = payload.PullRequest;
        var cloneUrl = pr.Head.Repo?.CloneUrl ?? string.Empty;

        var prInfo = new PullRequestInfo
        {
            PullRequestId = pr.Number,
            Title = pr.Title,
            Description = pr.Body,
            AuthorName = author,
            AuthorIdentifier = author,
            SourceBranch = pr.Head.Ref,
            TargetBranch = pr.Base.Ref,
            RepoName = pr.Head.Repo?.Name ?? "unknown",
            AuthenticatedCloneUrl = InjectPat(cloneUrl),
        };

        _logger.LogInformation("Processing GitHub PR #{PrId} by {Author}",
            prInfo.PullRequestId, prInfo.AuthorName);

        _ = Task.Run(async () =>
        {
            try { await _orchestrator.ProcessPullRequestAsync(prInfo); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background review failed for PR #{PrId}", prInfo.PullRequestId);
            }
        });

        return Accepted(new { status = "accepted", pullRequestId = prInfo.PullRequestId });
    }

    private string InjectPat(string cloneUrl)
    {
        if (cloneUrl.StartsWith("https://"))
            return cloneUrl.Replace("https://", $"https://{_githubOptions.Pat}@");
        return cloneUrl;
    }

    private static bool ValidateSignature(string payload, string signatureHeader, string secret)
    {
        if (!signatureHeader.StartsWith("sha256="))
            return false;

        var expectedSignature = signatureHeader["sha256=".Length..];
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        var computed = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(expectedSignature));
    }
}

using System.Diagnostics;
using CodeReviewAgent.Configuration;
using Microsoft.Extensions.Options;

namespace CodeReviewAgent.Services;

public class GitService : IGitService
{
    private readonly CodeReviewOptions _reviewOptions;
    private readonly ILogger<GitService> _logger;
    private readonly string _repoBasePath;

    private const string LastUsedMarker = ".last_used";

    public GitService(
        IOptions<CodeReviewOptions> reviewOptions,
        ILogger<GitService> logger)
    {
        _reviewOptions = reviewOptions.Value;
        _logger = logger;

        // Default to {app directory}/repos when RepoBasePath is not set
        _repoBasePath = string.IsNullOrWhiteSpace(_reviewOptions.RepoBasePath)
            ? Path.Combine(AppContext.BaseDirectory, "repos")
            : _reviewOptions.RepoBasePath;
    }

    public async Task<string> CheckoutBranchAsync(string repoName, string remoteUrl, string branchName)
    {
        Directory.CreateDirectory(_repoBasePath);
        var repoPath = Path.Combine(_repoBasePath, repoName);

        if (!Directory.Exists(Path.Combine(repoPath, ".git")))
        {
            _logger.LogInformation("Cloning {RepoName} into {RepoPath}", repoName, repoPath);
            await RunGitAsync(_repoBasePath, $"clone {remoteUrl} {repoName}");
        }
        else
        {
            _logger.LogInformation("Fetching latest for {RepoName}", repoName);
            await RunGitAsync(repoPath, "fetch --all --prune");
        }

        var localBranch = branchName.Replace("refs/heads/", "");
        _logger.LogInformation("Checking out branch {Branch}", localBranch);

        await RunGitAsync(repoPath, "checkout -- .");
        await RunGitAsync(repoPath, $"checkout {localBranch}");
        await RunGitAsync(repoPath, $"pull origin {localBranch}");

        // Mark this repo as recently used
        TouchLastUsed(repoPath);

        // Opportunistically clean up stale repos
        CleanupStaleRepos(repoName);

        return repoPath;
    }

    public async Task<string> GetDiffAsync(string repoPath, string targetBranch, string sourceBranch)
    {
        var target = targetBranch.Replace("refs/heads/", "");
        var source = sourceBranch.Replace("refs/heads/", "");

        _logger.LogInformation("Getting diff {Target}...{Source}", target, source);
        await RunGitAsync(repoPath, $"fetch origin {target}");

        return await RunGitAsync(repoPath, $"diff origin/{target}...{source}");
    }

    private void TouchLastUsed(string repoPath)
    {
        try
        {
            var markerPath = Path.Combine(repoPath, LastUsedMarker);
            File.WriteAllText(markerPath, DateTime.UtcNow.ToString("o"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write last-used marker for {RepoPath}", repoPath);
        }
    }

    private void CleanupStaleRepos(string currentRepoName)
    {
        if (_reviewOptions.RepoRetentionDays <= 0)
            return;

        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-_reviewOptions.RepoRetentionDays);

            foreach (var dir in Directory.GetDirectories(_repoBasePath))
            {
                var dirName = Path.GetFileName(dir);

                // Never delete the repo we just checked out
                if (dirName.Equals(currentRepoName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var markerPath = Path.Combine(dir, LastUsedMarker);
                DateTime lastUsed;

                if (File.Exists(markerPath))
                {
                    var content = File.ReadAllText(markerPath).Trim();
                    if (!DateTime.TryParse(content, out lastUsed))
                        lastUsed = File.GetLastWriteTimeUtc(markerPath);
                }
                else
                {
                    // No marker — use directory last write time as best guess
                    lastUsed = Directory.GetLastWriteTimeUtc(dir);
                }

                if (lastUsed < cutoff)
                {
                    _logger.LogInformation("Cleaning up stale repo {RepoName} (last used: {LastUsed})",
                        dirName, lastUsed);
                    Directory.Delete(dir, recursive: true);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during stale repo cleanup");
        }
    }

    private async Task<string> RunGitAsync(string workingDirectory, string arguments)
    {
        Directory.CreateDirectory(workingDirectory);

        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        _logger.LogDebug("Running: git {Arguments} in {Dir}", arguments, workingDirectory);

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start git process");

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            _logger.LogWarning("git {Args} exited with code {Code}: {Stderr}", arguments, process.ExitCode, stderr);

        return stdout;
    }
}

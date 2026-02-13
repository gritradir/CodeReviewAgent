using System.Diagnostics;
using CodeReviewAgent.Configuration;
using CodeReviewAgent.Models.Review;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CodeReviewAgent.Services;

public class ClaudeReviewEngine : IReviewEngineService
{
    private readonly ClaudeOptions _options;
    private readonly ILogger<ClaudeReviewEngine> _logger;

    public ClaudeReviewEngine(IOptions<ClaudeOptions> options, ILogger<ClaudeReviewEngine> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> RunReviewAsync(string prompt, string workingDirectory, string? systemInstructions = null)
    {
        // Prepend system instructions to the prompt piped via stdin.
        // This avoids command-line length limits with --append-system-prompt
        // and works reliably across all platforms.
        var fullPrompt = string.IsNullOrEmpty(systemInstructions)
            ? prompt
            : $"{systemInstructions}\n\n---\n\n{prompt}";

        var psi = new ProcessStartInfo
        {
            FileName = _options.CliPath,
            Arguments = "-p --output-format json",
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        _logger.LogInformation("Starting Claude review (working dir: {Dir})", workingDirectory);

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start claude process");

        await process.StandardInput.WriteAsync(fullPrompt);
        process.StandardInput.Close();

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            _logger.LogError("Claude exited with code {Code}: {Stderr}", process.ExitCode, stderr);
            throw new InvalidOperationException($"Claude CLI failed with exit code {process.ExitCode}: {stderr}");
        }

        var claudeResponse = JsonConvert.DeserializeObject<ClaudeResponse>(stdout);
        if (claudeResponse == null || claudeResponse.IsError)
        {
            var errorMsg = claudeResponse?.Result ?? "Unknown error from Claude CLI";
            _logger.LogError("Claude returned error: {Error}", errorMsg);
            throw new InvalidOperationException($"Claude CLI error: {errorMsg}");
        }

        _logger.LogInformation("Claude review completed in {Duration}ms, {Turns} turn(s)",
            claudeResponse.DurationMs, claudeResponse.NumTurns);

        return claudeResponse.Result;
    }
}

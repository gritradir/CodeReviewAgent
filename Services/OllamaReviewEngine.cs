using System.Text;
using CodeReviewAgent.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodeReviewAgent.Services;

public class OllamaReviewEngine : IReviewEngineService
{
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaReviewEngine> _logger;
    private readonly HttpClient _httpClient;

    public OllamaReviewEngine(IOptions<OllamaOptions> options, ILogger<OllamaReviewEngine> logger)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
    }

    public async Task<string> RunReviewAsync(string prompt, string workingDirectory, string? systemInstructions = null)
    {
        var url = $"{_options.Host.TrimEnd('/')}/api/generate";

        _logger.LogInformation("Starting Ollama review with model {Model}", _options.Model);

        var request = new Dictionary<string, object>
        {
            ["model"] = _options.Model,
            ["prompt"] = prompt,
            ["stream"] = false,
        };

        // Ollama supports a dedicated system field for system-level instructions
        if (!string.IsNullOrEmpty(systemInstructions))
            request["system"] = systemInstructions;

        var requestBody = JsonConvert.SerializeObject(request);

        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Ollama returned {StatusCode}: {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"Ollama API failed: {response.StatusCode} — {responseBody}");
        }

        var json = JObject.Parse(responseBody);
        var result = json["response"]?.ToString();

        if (string.IsNullOrEmpty(result))
        {
            _logger.LogError("Ollama returned empty response");
            throw new InvalidOperationException("Ollama returned an empty response");
        }

        var totalDuration = json["total_duration"]?.Value<long>() ?? 0;
        _logger.LogInformation("Ollama review completed in {Duration}ms",
            totalDuration / 1_000_000); // nanoseconds to ms

        return result;
    }
}

using Newtonsoft.Json;

namespace CodeReviewAgent.Models.Review;

public class ClaudeResponse
{
    [JsonProperty("result")]
    public string Result { get; set; } = string.Empty;

    [JsonProperty("is_error")]
    public bool IsError { get; set; }

    [JsonProperty("duration_ms")]
    public long DurationMs { get; set; }

    [JsonProperty("duration_api_ms")]
    public long DurationApiMs { get; set; }

    [JsonProperty("num_turns")]
    public int NumTurns { get; set; }

    [JsonProperty("session_id")]
    public string SessionId { get; set; } = string.Empty;
}

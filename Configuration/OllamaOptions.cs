namespace CodeReviewAgent.Configuration;

public class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string Host { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3";
}

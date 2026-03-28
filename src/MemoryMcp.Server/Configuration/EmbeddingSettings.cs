namespace MemoryMcp.Server.Configuration;

public sealed class EmbeddingSettings
{
    public const string SectionName = "Embedding";

    public string Provider { get; init; } = "Ollama";
    public string Model { get; init; } = "nomic-embed-text";
    public string BaseUrl { get; init; } = "http://localhost:11434";
    public string ApiKey { get; init; } = string.Empty;
    public int Dimensions { get; init; } = 768;
}

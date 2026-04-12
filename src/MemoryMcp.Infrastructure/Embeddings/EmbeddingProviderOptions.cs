namespace MemoryMcp.Infrastructure.Embeddings;

public sealed class EmbeddingProviderOptions
{
    public string Provider { get; init; } = "Ollama";
    public string Model { get; init; } = "nomic-embed-text";
    public string BaseUrl { get; init; } = "http://localhost:11434";
    public string ApiKey { get; init; } = string.Empty;
    public int Dimensions { get; init; } = 256;
    public int CooldownSeconds { get; init; } = 120;
    public int CooldownFailureThreshold { get; init; } = 3;
}

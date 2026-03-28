namespace MemoryMcp.Infrastructure.Embeddings;

public sealed class EmbeddingProviderOptions
{
    public string Model { get; init; } = "nomic-embed-text";
    public int Dimensions { get; init; } = 256;
}

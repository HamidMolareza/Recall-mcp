using MemoryMcp.Domain.ValueObjects;

namespace MemoryMcp.Infrastructure.Embeddings;

public interface IRemoteEmbeddingGenerator
{
    Task<EmbeddingVector> GenerateAsync(string text, CancellationToken ct = default);
}

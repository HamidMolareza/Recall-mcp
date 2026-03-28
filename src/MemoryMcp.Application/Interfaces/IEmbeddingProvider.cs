using MemoryMcp.Domain.ValueObjects;

namespace MemoryMcp.Application.Interfaces;

public interface IEmbeddingProvider
{
    Task<EmbeddingVector> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
}

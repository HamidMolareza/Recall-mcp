using MemoryMcp.Domain.ValueObjects;

namespace MemoryMcp.Application.Interfaces;

public interface IVectorStore
{
    Task UpsertAsync(Guid memoryId, EmbeddingVector vector, CancellationToken ct = default);
    Task DeleteAsync(Guid memoryId, CancellationToken ct = default);
    Task DeleteByNamespaceAsync(string ns, CancellationToken ct = default);
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(EmbeddingVector query, int topK, CancellationToken ct = default);
}

public sealed record VectorSearchResult(Guid MemoryId, double Score);

using System.Collections.Concurrent;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Infrastructure.VectorSearch;

public sealed class InMemoryVectorStore : IVectorStore
{
    private readonly ConcurrentDictionary<Guid, EmbeddingVector> _vectors = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger<InMemoryVectorStore> _logger;

    public InMemoryVectorStore(ILogger<InMemoryVectorStore> logger)
    {
        _logger = logger;
    }

    public Task UpsertAsync(Guid memoryId, EmbeddingVector vector, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Guid memoryId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteByNamespaceAsync(string ns, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(EmbeddingVector query, int topK, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}

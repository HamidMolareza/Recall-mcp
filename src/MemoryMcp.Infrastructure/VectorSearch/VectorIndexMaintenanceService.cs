using MemoryMcp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MemoryMcp.Infrastructure.VectorSearch;

public sealed class VectorIndexMaintenanceService
{
    private readonly MemoryDbContext _dbContext;
    private readonly InMemoryVectorStore _vectorStore;

    public VectorIndexMaintenanceService(
        MemoryDbContext dbContext,
        InMemoryVectorStore vectorStore)
    {
        _dbContext = dbContext;
        _vectorStore = vectorStore;
    }

    public async Task<VectorIndexRebuildResult> RebuildAsync(CancellationToken ct = default)
    {
        var entries = await _dbContext.Memories
            .AsNoTracking()
            .Where(memory => memory.Embedding != null)
            .Select(memory => new VectorIndexEntry(
                memory.Id,
                memory.Embedding!,
                memory.Namespace.Value))
            .ToArrayAsync(ct);

        await _vectorStore.ReplaceAsync(entries, ct);
        var snapshot = _vectorStore.GetSnapshot();

        return new VectorIndexRebuildResult(entries.Length, snapshot);
    }
}

public sealed record VectorIndexRebuildResult(
    int PersistedEmbeddingCount,
    VectorIndexSnapshot Snapshot);

using MemoryMcp.Application.Interfaces;
using MemoryMcp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Infrastructure.VectorSearch;

public sealed class VectorIndexMaintenanceService
{
    private readonly MemoryDbContext _dbContext;
    private readonly InMemoryVectorStore _vectorStore;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly ILogger<VectorIndexMaintenanceService> _logger;

    public VectorIndexMaintenanceService(
        MemoryDbContext dbContext,
        InMemoryVectorStore vectorStore,
        IEmbeddingProvider embeddingProvider,
        ILogger<VectorIndexMaintenanceService> logger)
    {
        _dbContext = dbContext;
        _vectorStore = vectorStore;
        _embeddingProvider = embeddingProvider;
        _logger = logger;
    }

    public async Task<VectorIndexRebuildResult> RebuildAsync(bool regenerateEmbeddings = false, CancellationToken ct = default)
    {
        var reembeddedMemoryCount = 0;
        if (regenerateEmbeddings)
        {
            var memories = await _dbContext.Memories.ToArrayAsync(ct);
            foreach (var memory in memories)
            {
                ct.ThrowIfCancellationRequested();
                memory.SetEmbedding(await _embeddingProvider.GenerateEmbeddingAsync(memory.Content, ct));
            }

            reembeddedMemoryCount = memories.Length;
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation(
                "{IsAudit} Re-generated embeddings for {MemoryCount} persisted memories using the configured embedding provider",
                true,
                reembeddedMemoryCount);
        }

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

        return new VectorIndexRebuildResult(entries.Length, reembeddedMemoryCount, snapshot);
    }
}

public sealed record VectorIndexRebuildResult(
    int PersistedEmbeddingCount,
    int ReembeddedMemoryCount,
    VectorIndexSnapshot Snapshot);

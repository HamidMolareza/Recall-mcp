using MemoryMcp.Application.DTOs;
using MemoryMcp.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Application.Services;

public sealed class StalenessService
{
    private readonly IMemoryRepository _repository;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<StalenessService> _logger;

    public StalenessService(
        IMemoryRepository repository,
        IVectorStore vectorStore,
        ILogger<StalenessService> logger)
    {
        _repository = repository;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MemoryResponse>> FindStaleAsync(int thresholdDays, CancellationToken ct = default)
    {
        var entries = await _repository.ListAsync(new MemoryQueryFilter
        {
            Page = 1,
            PageSize = 10_000
        }, ct);

        return entries
            .Where(entry => entry.IsStale(thresholdDays))
            .OrderBy(entry => entry.LastAccessedAt)
            .Select(entry => entry.ToResponse())
            .ToArray();
    }

    public async Task<int> CleanupExpiredAsync(CancellationToken ct = default)
    {
        var entries = await _repository.ListAsync(new MemoryQueryFilter
        {
            Page = 1,
            PageSize = 10_000
        }, ct);

        var expired = entries.Where(entry => entry.IsExpired()).ToArray();
        foreach (var entry in expired)
        {
            ct.ThrowIfCancellationRequested();
            await _repository.DeleteAsync(entry.Id, ct);
            await _vectorStore.DeleteAsync(entry.Id, ct);
        }

        _logger.LogInformation("{IsAudit} Cleaned up {Count} expired memories", true, expired.Length);
        return expired.Length;
    }
}

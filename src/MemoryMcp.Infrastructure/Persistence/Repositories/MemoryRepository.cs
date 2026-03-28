using MemoryMcp.Application.Interfaces;
using MemoryMcp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Infrastructure.Persistence.Repositories;

public sealed class MemoryRepository : IMemoryRepository
{
    private readonly MemoryDbContext _dbContext;
    private readonly ILogger<MemoryRepository> _logger;

    public MemoryRepository(MemoryDbContext dbContext, ILogger<MemoryRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task<MemoryEntry?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return QueryWithDetails()
            .FirstOrDefaultAsync(memory => memory.Id == id, ct);
    }

    public async Task<IReadOnlyList<MemoryEntry>> ListAsync(MemoryQueryFilter filter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var page = Math.Max(filter.Page, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, 500);
        var candidates = await ApplyDatabaseFilter(QueryWithDetails().AsNoTracking(), filter)
            .OrderByDescending(memory => memory.UpdatedAt)
            .ThenByDescending(memory => memory.CreatedAt)
            .ToArrayAsync(ct);

        var filtered = ApplyInMemoryFilter(candidates, filter)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        _logger.LogDebug(
            "ListAsync returned {Count} memories for namespace={Namespace}, type={Type}, page={Page}, pageSize={PageSize}",
            filtered.Length,
            filter.Namespace,
            filter.Type,
            page,
            pageSize);

        return filtered;
    }

    public async Task<int> CountAsync(MemoryQueryFilter filter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var candidates = await ApplyDatabaseFilter(QueryWithDetails().AsNoTracking(), filter)
            .ToArrayAsync(ct);

        return ApplyInMemoryFilter(candidates, filter).Count();
    }

    public async Task AddAsync(MemoryEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await _dbContext.Memories.AddAsync(entry, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(MemoryEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (_dbContext.Entry(entry).State == EntityState.Detached)
        {
            _dbContext.Memories.Update(entry);
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entry = await _dbContext.Memories.FirstOrDefaultAsync(memory => memory.Id == id, ct);
        if (entry is null)
        {
            return;
        }

        _dbContext.Memories.Remove(entry);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteByNamespaceAsync(string ns, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ns);

        var normalized = ns.Trim().ToLowerInvariant();
        var entries = await _dbContext.Memories
            .Where(memory => !memory.ExpiresAt.HasValue || memory.ExpiresAt > DateTime.UtcNow)
            .ToArrayAsync(ct);

        entries = entries
            .Where(memory =>
                memory.Namespace.Value == normalized ||
                memory.Namespace.Value.StartsWith($"{normalized}/", StringComparison.Ordinal))
            .ToArray();

        if (entries.Length == 0)
        {
            return;
        }

        _dbContext.Memories.RemoveRange(entries);
        await _dbContext.SaveChangesAsync(ct);
    }

    private IQueryable<MemoryEntry> QueryWithDetails()
    {
        return _dbContext.Memories
            .Include(memory => memory.Tags)
            .Include(memory => memory.Versions)
            .Include(memory => memory.OutgoingLinks);
    }

    private static IQueryable<MemoryEntry> ApplyDatabaseFilter(IQueryable<MemoryEntry> query, MemoryQueryFilter filter)
    {
        var now = DateTime.UtcNow;
        query = query.Where(memory => !memory.ExpiresAt.HasValue || memory.ExpiresAt > now);

        if (filter.Type.HasValue)
        {
            query = query.Where(memory => memory.Type == filter.Type.Value);
        }

        if (filter.Tags is { Count: > 0 })
        {
            foreach (var tag in filter.Tags
                         .Where(static tag => !string.IsNullOrWhiteSpace(tag))
                         .Select(static tag => tag.Trim().ToLowerInvariant())
                         .Distinct(StringComparer.Ordinal))
            {
                var normalizedTag = tag;
                query = query.Where(memory => memory.Tags.Any(memoryTag => memoryTag.Label == normalizedTag));
            }
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(memory => memory.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(memory => memory.CreatedAt <= filter.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var searchText = filter.SearchText.Trim();
            var pattern = $"%{searchText}%";
            query = query.Where(memory => EF.Functions.Like(memory.Content, pattern));
        }

        return query;
    }

    private static IEnumerable<MemoryEntry> ApplyInMemoryFilter(IEnumerable<MemoryEntry> entries, MemoryQueryFilter filter)
    {
        var filtered = entries;

        if (!string.IsNullOrWhiteSpace(filter.Namespace))
        {
            var normalized = filter.Namespace.Trim().ToLowerInvariant();
            filtered = filtered.Where(memory =>
                memory.Namespace.Value == normalized ||
                memory.Namespace.Value.StartsWith($"{normalized}/", StringComparison.Ordinal));
        }

        return filtered;
    }
}

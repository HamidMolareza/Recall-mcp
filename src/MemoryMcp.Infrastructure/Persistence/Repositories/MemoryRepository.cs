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

        var query = ApplyFilter(QueryWithDetails().AsNoTracking(), filter)
            .OrderByDescending(memory => memory.UpdatedAt)
            .ThenByDescending(memory => memory.CreatedAt);

        var page = Math.Max(filter.Page, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, 500);

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(ct);
    }

    public Task<int> CountAsync(MemoryQueryFilter filter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        return ApplyFilter(_dbContext.Memories.AsNoTracking(), filter)
            .CountAsync(ct);
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
            .Where(memory =>
                EF.Property<string>(memory, nameof(MemoryEntry.Namespace)) == normalized ||
                EF.Property<string>(memory, nameof(MemoryEntry.Namespace)).StartsWith($"{normalized}/"))
            .ToArrayAsync(ct);

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

    private static IQueryable<MemoryEntry> ApplyFilter(IQueryable<MemoryEntry> query, MemoryQueryFilter filter)
    {
        var now = DateTime.UtcNow;
        query = query.Where(memory => !memory.ExpiresAt.HasValue || memory.ExpiresAt > now);

        if (!string.IsNullOrWhiteSpace(filter.Namespace))
        {
            var normalized = filter.Namespace.Trim().ToLowerInvariant();
            query = query.Where(memory =>
                EF.Property<string>(memory, nameof(MemoryEntry.Namespace)) == normalized ||
                EF.Property<string>(memory, nameof(MemoryEntry.Namespace)).StartsWith($"{normalized}/"));
        }

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
}

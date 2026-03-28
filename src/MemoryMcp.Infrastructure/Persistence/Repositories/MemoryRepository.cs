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

        var trackedEntityCount = _dbContext.ChangeTracker.Entries().Count();
        if (trackedEntityCount > 0)
        {
            _logger.LogDebug(
                "Clearing {TrackedEntityCount} tracked entities before merging memory aggregate {MemoryId}",
                trackedEntityCount,
                entry.Id);
        }

        _dbContext.ChangeTracker.Clear();

        var trackedEntry = await QueryWithDetails()
            .FirstOrDefaultAsync(memory => memory.Id == entry.Id, ct);

        if (trackedEntry is null)
        {
            throw new InvalidOperationException($"Memory entry {entry.Id} was not found.");
        }

        _logger.LogDebug("Merging memory aggregate {MemoryId} into a fresh DbContext graph", entry.Id);
        MergeScalarValues(trackedEntry, entry);
        SynchronizeTags(trackedEntry, entry);
        SynchronizeVersions(trackedEntry, entry);
        SynchronizeLinks(trackedEntry, entry);

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
            .AsSplitQuery()
            .Include(memory => memory.Tags)
            .Include(memory => memory.Versions)
            .Include(memory => memory.OutgoingLinks);
    }

    private static IQueryable<MemoryEntry> ApplyDatabaseFilter(IQueryable<MemoryEntry> query, MemoryQueryFilter filter)
    {
        var now = DateTime.UtcNow;
        if (!filter.IncludeExpired)
        {
            query = query.Where(memory => !memory.ExpiresAt.HasValue || memory.ExpiresAt > now);
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

    private void MergeScalarValues(MemoryEntry trackedEntry, MemoryEntry entry)
    {
        var trackedValues = _dbContext.Entry(trackedEntry);
        trackedValues.Property(memory => memory.Content).CurrentValue = entry.Content;
        trackedValues.Property(memory => memory.Type).CurrentValue = entry.Type;
        trackedValues.Property(memory => memory.Namespace).CurrentValue = entry.Namespace;
        trackedValues.Property(memory => memory.Embedding).CurrentValue = entry.Embedding;
        trackedValues.Property(memory => memory.UpdatedAt).CurrentValue = entry.UpdatedAt;
        trackedValues.Property(memory => memory.LastAccessedAt).CurrentValue = entry.LastAccessedAt;
        trackedValues.Property(memory => memory.ExpiresAt).CurrentValue = entry.ExpiresAt;
    }

    private void SynchronizeTags(MemoryEntry trackedEntry, MemoryEntry entry)
    {
        var existingTags = trackedEntry.Tags.ToDictionary(tag => tag.Label, StringComparer.Ordinal);
        var desiredLabels = entry.Tags
            .Select(tag => tag.Label)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        var tagsToRemove = trackedEntry.Tags
            .Where(tag => !desiredLabels.Contains(tag.Label))
            .ToArray();

        if (tagsToRemove.Length > 0)
        {
            _dbContext.Tags.RemoveRange(tagsToRemove);
        }

        var tagsToAdd = entry.Tags
            .Where(tag => !existingTags.ContainsKey(tag.Label))
            .ToArray();

        foreach (var tag in tagsToAdd)
        {
            _dbContext.Entry(tag).Property(nameof(Tag.MemoryEntryId)).CurrentValue = trackedEntry.Id;
            _dbContext.Tags.Add(tag);
        }
    }

    private void SynchronizeVersions(MemoryEntry trackedEntry, MemoryEntry entry)
    {
        var existingVersionIds = trackedEntry.Versions
            .Select(version => version.Id)
            .ToHashSet();

        foreach (var version in entry.Versions.Where(version => !existingVersionIds.Contains(version.Id)))
        {
            _dbContext.Entry(version).Property(nameof(MemoryVersion.MemoryEntryId)).CurrentValue = trackedEntry.Id;
            _dbContext.Versions.Add(version);
        }
    }

    private void SynchronizeLinks(MemoryEntry trackedEntry, MemoryEntry entry)
    {
        var existingLinkIds = trackedEntry.OutgoingLinks
            .Select(link => link.Id)
            .ToHashSet();

        foreach (var link in entry.OutgoingLinks.Where(link => !existingLinkIds.Contains(link.Id)))
        {
            _dbContext.Entry(link).Property(nameof(MemoryLink.SourceId)).CurrentValue = trackedEntry.Id;
            _dbContext.Links.Add(link);
        }
    }
}

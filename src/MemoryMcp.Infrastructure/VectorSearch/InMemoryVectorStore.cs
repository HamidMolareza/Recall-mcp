using System.Collections.Concurrent;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Infrastructure.VectorSearch;

public sealed class InMemoryVectorStore : IVectorStore
{
    private readonly ConcurrentDictionary<Guid, EmbeddingVector> _vectors = new();
    private readonly ConcurrentDictionary<Guid, string> _namespaces = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger<InMemoryVectorStore> _logger;

    public InMemoryVectorStore(ILogger<InMemoryVectorStore> logger)
    {
        _logger = logger;
    }

    public Task UpsertAsync(Guid memoryId, EmbeddingVector vector, string? ns = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(vector);
        ct.ThrowIfCancellationRequested();

        _lock.EnterWriteLock();
        try
        {
            _vectors[memoryId] = vector;
            if (!string.IsNullOrWhiteSpace(ns))
            {
                _namespaces[memoryId] = ns.Trim().ToLowerInvariant();
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid memoryId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _lock.EnterWriteLock();
        try
        {
            _vectors.TryRemove(memoryId, out _);
            _namespaces.TryRemove(memoryId, out _);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task DeleteByNamespaceAsync(string ns, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ns);
        ct.ThrowIfCancellationRequested();

        var normalized = ns.Trim().ToLowerInvariant();

        _lock.EnterWriteLock();
        try
        {
            foreach (var pair in _namespaces
                         .Where(pair => pair.Value == normalized || pair.Value.StartsWith($"{normalized}/", StringComparison.Ordinal))
                         .ToArray())
            {
                _namespaces.TryRemove(pair.Key, out _);
                _vectors.TryRemove(pair.Key, out _);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(EmbeddingVector query, int topK, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ct.ThrowIfCancellationRequested();

        KeyValuePair<Guid, EmbeddingVector>[] snapshot;
        _lock.EnterReadLock();
        try
        {
            snapshot = _vectors.ToArray();
        }
        finally
        {
            _lock.ExitReadLock();
        }

        var results = snapshot
            .AsParallel()
            .WithCancellation(ct)
            .Select(pair => new VectorSearchResult(pair.Key, query.CosineSimilarity(pair.Value)))
            .OrderByDescending(result => result.Score)
            .Take(Math.Clamp(topK, 1, 500))
            .ToArray();

        return Task.FromResult<IReadOnlyList<VectorSearchResult>>(results);
    }

    public VectorIndexSnapshot GetSnapshot()
    {
        _lock.EnterReadLock();
        try
        {
            var namespaceCounts = _namespaces.Values
                .GroupBy(static ns => ns, StringComparer.Ordinal)
                .ToDictionary(static group => group.Key, static group => group.Count(), StringComparer.Ordinal);
            var unscopedEntryCount = _vectors.Keys.Count(id => !_namespaces.ContainsKey(id));

            return new VectorIndexSnapshot(
                _vectors.Count,
                _namespaces.Count,
                unscopedEntryCount,
                namespaceCounts);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}

public sealed record VectorIndexSnapshot(
    int EntryCount,
    int NamespacedEntryCount,
    int UnscopedEntryCount,
    IReadOnlyDictionary<string, int> NamespaceCounts);

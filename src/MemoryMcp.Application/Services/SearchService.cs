using MemoryMcp.Application.DTOs;
using MemoryMcp.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Application.Services;

public sealed class SearchService
{
    private readonly IMemoryRepository _repository;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<SearchService> _logger;

    public SearchService(
        IMemoryRepository repository,
        IEmbeddingProvider embeddingProvider,
        IVectorStore vectorStore,
        ILogger<SearchService> logger)
    {
        _repository = repository;
        _embeddingProvider = embeddingProvider;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SearchResult>> SemanticSearchAsync(SearchMemoryRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return [];
        }

        var queryVector = await _embeddingProvider.GenerateEmbeddingAsync(request.Query, ct);
        var overfetch = Math.Clamp(request.TopK * 5, request.TopK, 500);
        var matches = await _vectorStore.SearchAsync(queryVector, overfetch, ct);

        var results = new List<SearchResult>(request.TopK);
        foreach (var match in matches)
        {
            ct.ThrowIfCancellationRequested();

            var entry = await _repository.GetByIdAsync(match.MemoryId, ct);
            if (entry is null || !Matches(entry, request))
            {
                continue;
            }

            results.Add(new SearchResult
            {
                Memory = entry.ToResponse(match.Score),
                Score = match.Score
            });

            if (results.Count >= request.TopK)
            {
                break;
            }
        }

        _logger.LogDebug("Semantic search for '{Query}' returned {Count} results", request.Query, results.Count);

        return results;
    }

    public async Task<IReadOnlyList<MemoryResponse>> FullTextSearchAsync(string query, int topK = 10, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var entries = await _repository.ListAsync(new MemoryQueryFilter
        {
            SearchText = query,
            Page = 1,
            PageSize = Math.Clamp(topK, 1, 100)
        }, ct);

        return entries
            .Select(entry => entry.ToResponse())
            .ToArray();
    }

    private static bool Matches(Domain.Entities.MemoryEntry entry, SearchMemoryRequest request)
    {
        if (entry.IsExpired())
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(request.Namespace))
        {
            var normalized = request.Namespace.Trim().ToLowerInvariant();
            if (entry.Namespace.Value != normalized &&
                !entry.Namespace.Value.StartsWith($"{normalized}/", StringComparison.Ordinal))
            {
                return false;
            }
        }

        if (request.Type.HasValue && entry.Type != request.Type.Value)
        {
            return false;
        }

        if (request.Tags is { Count: > 0 })
        {
            var tags = entry.Tags.Select(tag => tag.Label).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (request.Tags.Any(tag => !tags.Contains(tag.Trim().ToLowerInvariant())))
            {
                return false;
            }
        }

        if (request.FromDate.HasValue && entry.CreatedAt < request.FromDate.Value)
        {
            return false;
        }

        if (request.ToDate.HasValue && entry.CreatedAt > request.ToDate.Value)
        {
            return false;
        }

        return true;
    }
}

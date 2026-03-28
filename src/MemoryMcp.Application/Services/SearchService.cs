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

    public Task<IReadOnlyList<SearchResult>> SemanticSearchAsync(SearchMemoryRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<MemoryResponse>> FullTextSearchAsync(string query, int topK = 10, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}

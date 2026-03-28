using MemoryMcp.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Application.Services;

public sealed class DeduplicationService
{
    private readonly IMemoryRepository _repository;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<DeduplicationService> _logger;

    public DeduplicationService(
        IMemoryRepository repository,
        IEmbeddingProvider embeddingProvider,
        IVectorStore vectorStore,
        ILogger<DeduplicationService> logger)
    {
        _repository = repository;
        _embeddingProvider = embeddingProvider;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public Task<IReadOnlyList<Guid>> FindDuplicatesAsync(string content, double threshold = 0.95, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}

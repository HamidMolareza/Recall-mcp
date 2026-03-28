using MemoryMcp.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Application.Services;

public sealed class ContradictionDetector
{
    private readonly IMemoryRepository _repository;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<ContradictionDetector> _logger;

    public ContradictionDetector(
        IMemoryRepository repository,
        IEmbeddingProvider embeddingProvider,
        IVectorStore vectorStore,
        ILogger<ContradictionDetector> logger)
    {
        _repository = repository;
        _embeddingProvider = embeddingProvider;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public Task<IReadOnlyList<Guid>> DetectContradictionsAsync(string content, string ns, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}

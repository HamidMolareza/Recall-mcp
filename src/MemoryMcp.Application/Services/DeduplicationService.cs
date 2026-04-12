using MemoryMcp.Application.Interfaces;
using MemoryMcp.Domain.ValueObjects;
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

    public async Task<IReadOnlyList<Guid>> FindDuplicatesAsync(
        string content,
        EmbeddingVector? embedding = null,
        double threshold = 0.95,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var vector = embedding ?? await _embeddingProvider.GenerateEmbeddingAsync(content, ct);
        var candidates = await _vectorStore.SearchAsync(vector, 20, ct);

        var duplicates = new List<Guid>();
        foreach (var candidate in candidates.Where(c => c.Score >= threshold))
        {
            ct.ThrowIfCancellationRequested();

            var entry = await _repository.GetByIdAsync(candidate.MemoryId, ct);
            if (entry is null || entry.IsExpired())
            {
                continue;
            }

            duplicates.Add(candidate.MemoryId);
        }

        _logger.LogDebug("Duplicate detection found {Count} candidates", duplicates.Count);
        return duplicates;
    }
}

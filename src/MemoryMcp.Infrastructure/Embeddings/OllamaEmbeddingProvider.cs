using MemoryMcp.Application.Interfaces;
using MemoryMcp.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Infrastructure.Embeddings;

public sealed class OllamaEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaEmbeddingProvider> _logger;

    public OllamaEmbeddingProvider(HttpClient httpClient, ILogger<OllamaEmbeddingProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<EmbeddingVector> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}

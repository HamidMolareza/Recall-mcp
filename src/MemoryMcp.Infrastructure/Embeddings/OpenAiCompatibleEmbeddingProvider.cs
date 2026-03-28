using MemoryMcp.Application.Interfaces;
using MemoryMcp.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Embeddings;
using System.ClientModel;

namespace MemoryMcp.Infrastructure.Embeddings;

public sealed class OpenAiCompatibleEmbeddingProvider : IEmbeddingProvider
{
    private readonly EmbeddingClient _client;
    private readonly ILogger<OpenAiCompatibleEmbeddingProvider> _logger;

    public OpenAiCompatibleEmbeddingProvider(
        EmbeddingProviderOptions options,
        ILogger<OpenAiCompatibleEmbeddingProvider> logger)
    {
        _logger = logger;

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("Embedding API key is required for the OpenAI-compatible embedding provider.");
        }

        _client = new EmbeddingClient(
            model: options.Model,
            credential: new ApiKeyCredential(options.ApiKey),
            options: new OpenAIClientOptions
            {
                Endpoint = new Uri(NormalizeBaseUrl(options.BaseUrl))
            });
    }

    public async Task<EmbeddingVector> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        try
        {
            OpenAIEmbedding embedding = await _client.GenerateEmbeddingAsync(text, cancellationToken: ct);
            return new EmbeddingVector(embedding.ToFloats().ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI-compatible embedding generation failed");
            throw;
        }
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        return baseUrl.TrimEnd('/') + "/";
    }
}

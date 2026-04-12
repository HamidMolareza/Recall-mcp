using MemoryMcp.Domain.ValueObjects;
using OpenAI;
using OpenAI.Embeddings;
using System.ClientModel;

namespace MemoryMcp.Infrastructure.Embeddings;

public sealed class OpenAiCompatibleRemoteEmbeddingGenerator : IRemoteEmbeddingGenerator
{
    private readonly EmbeddingClient _client;

    public OpenAiCompatibleRemoteEmbeddingGenerator(EmbeddingProviderOptions options)
    {
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

    public async Task<EmbeddingVector> GenerateAsync(string text, CancellationToken ct = default)
    {
        OpenAIEmbedding embedding = await _client.GenerateEmbeddingAsync(text, cancellationToken: ct);
        return new EmbeddingVector(embedding.ToFloats().ToArray());
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        return baseUrl.TrimEnd('/') + "/";
    }
}

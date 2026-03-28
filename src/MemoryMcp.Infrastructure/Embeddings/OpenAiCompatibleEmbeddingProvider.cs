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
    private readonly EmbeddingProviderOptions _options;
    private readonly ILogger<OpenAiCompatibleEmbeddingProvider> _logger;

    public OpenAiCompatibleEmbeddingProvider(
        EmbeddingProviderOptions options,
        ILogger<OpenAiCompatibleEmbeddingProvider> logger)
    {
        _options = options;
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
            _logger.LogWarning(
                ex,
                "OpenAI-compatible embedding generation failed. Falling back to deterministic local embeddings.");
            return CreateDeterministicEmbedding(text);
        }
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        return baseUrl.TrimEnd('/') + "/";
    }

    private EmbeddingVector CreateDeterministicEmbedding(string text)
    {
        var dimensions = Math.Clamp(_options.Dimensions, 32, 2048);
        var values = new float[dimensions];

        foreach (var token in Tokenize(text))
        {
            var hash = GetDeterministicHash(token);
            var index = Math.Abs(hash % dimensions);
            values[index] += 1f;
        }

        if (values.All(value => value == 0f))
        {
            values[0] = 1f;
        }

        return new EmbeddingVector(values);
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        return text
            .ToLowerInvariant()
            .Split([' ', '\t', '\r', '\n', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '/', '\\', '"', '\''],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static token => token.Length > 0);
    }

    private static int GetDeterministicHash(string value)
    {
        unchecked
        {
            var hash = 23;
            foreach (var ch in value)
            {
                hash = (hash * 31) + ch;
            }

            return hash;
        }
    }
}

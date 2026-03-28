using MemoryMcp.Application.Interfaces;
using MemoryMcp.Domain.ValueObjects;
using MemoryMcp.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace MemoryMcp.Infrastructure.Embeddings;

public sealed class OllamaEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly EmbeddingProviderOptions _options;
    private readonly IOperationNoticeSink _operationNoticeSink;
    private readonly ILogger<OllamaEmbeddingProvider> _logger;

    public OllamaEmbeddingProvider(
        HttpClient httpClient,
        EmbeddingProviderOptions options,
        IOperationNoticeSink operationNoticeSink,
        ILogger<OllamaEmbeddingProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _operationNoticeSink = operationNoticeSink;
        _logger = logger;
    }

    public async Task<EmbeddingVector> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        try
        {
            _logger.LogDebug(
                "Generating Ollama embedding using model {Model} and base address {BaseAddress}",
                _options.Model,
                _httpClient.BaseAddress);

            var embedRequest = new
            {
                model = _options.Model,
                input = text
            };

            using var response = await _httpClient.PostAsJsonAsync("/api/embed", embedRequest, ct);
            if (response.IsSuccessStatusCode)
            {
                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
                if (TryReadEmbedding(document.RootElement, out var vector))
                {
                    return new EmbeddingVector(vector);
                }
            }
            else
            {
                _operationNoticeSink.AddWarning(new OperationWarning(
                    "embedding_provider_http_error",
                    $"The {_options.Provider} embedding request returned HTTP {(int)response.StatusCode}. Local deterministic embeddings were used for this request.",
                    "Check the provider endpoint, model name, and server health before retrying.",
                    $"StatusCode={(int)response.StatusCode}; Model={_options.Model}; BaseAddress={_httpClient.BaseAddress}"));
                _logger.LogWarning(
                    "Ollama embedding request failed with status code {StatusCode} for model {Model}",
                    response.StatusCode,
                    _options.Model);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            var warning = ExternalServiceFailureClassifier.CreateFallbackWarning(
                ex,
                "embedding request",
                _options.Provider,
                _options.Model,
                "Local deterministic embeddings were used for this request.");
            _operationNoticeSink.AddWarning(warning);
            _logger.LogWarning(
                ex,
                "{WarningMessage} Model={Model}, BaseAddress={BaseAddress}",
                warning.Message,
                _options.Model,
                _httpClient.BaseAddress);
        }

        return CreateDeterministicEmbedding(text);
    }

    private bool TryReadEmbedding(JsonElement root, out float[] vector)
    {
        if (root.TryGetProperty("embeddings", out var embeddings) &&
            embeddings.ValueKind == JsonValueKind.Array &&
            embeddings.GetArrayLength() > 0)
        {
            var first = embeddings[0];
            if (TryReadVector(first, out vector))
            {
                return true;
            }
        }

        if (root.TryGetProperty("embedding", out var embedding) && TryReadVector(embedding, out vector))
        {
            return true;
        }

        vector = [];
        return false;
    }

    private static bool TryReadVector(JsonElement element, out float[] vector)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            vector = [];
            return false;
        }

        vector = element.EnumerateArray()
            .Select(static item => item.GetSingle())
            .ToArray();

        return vector.Length > 0;
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

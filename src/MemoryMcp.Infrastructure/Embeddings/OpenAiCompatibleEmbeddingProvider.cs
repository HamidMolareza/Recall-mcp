using MemoryMcp.Application.Diagnostics;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Domain.ValueObjects;
using MemoryMcp.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Infrastructure.Embeddings;

public sealed class OpenAiCompatibleEmbeddingProvider : IEmbeddingProvider
{
    private readonly IRemoteEmbeddingGenerator _remoteEmbeddingGenerator;
    private readonly EmbeddingProviderOptions _options;
    private readonly IOperationNoticeSink _operationNoticeSink;
    private readonly EmbeddingProviderCooldownState _cooldownState;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OpenAiCompatibleEmbeddingProvider> _logger;

    public OpenAiCompatibleEmbeddingProvider(
        IRemoteEmbeddingGenerator remoteEmbeddingGenerator,
        EmbeddingProviderOptions options,
        IOperationNoticeSink operationNoticeSink,
        EmbeddingProviderCooldownState cooldownState,
        TimeProvider timeProvider,
        ILogger<OpenAiCompatibleEmbeddingProvider> logger)
    {
        _remoteEmbeddingGenerator = remoteEmbeddingGenerator;
        _options = options;
        _operationNoticeSink = operationNoticeSink;
        _cooldownState = cooldownState;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<EmbeddingVector> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var now = _timeProvider.GetUtcNow();
        var snapshot = _cooldownState.GetSnapshot(now);
        if (snapshot.JustExpired)
        {
            _logger.LogInformation(
                "Embedding provider cooldown expired for provider {Provider}, model {Model}, base URL {BaseUrl}; remote embedding attempts will resume.",
                _options.Provider,
                _options.Model,
                _options.BaseUrl);
        }

        if (snapshot.IsActive)
        {
            var warning = CreateCooldownWarning(snapshot);
            _operationNoticeSink.AddWarning(warning);
            _logger.LogWarning(
                "Skipping remote embedding request because cooldown is active until {CooldownUntil} for provider {Provider}, model {Model}, reason {Reason}.",
                snapshot.CooldownUntil,
                _options.Provider,
                _options.Model,
                snapshot.Reason);
            return CreateDeterministicEmbedding(text);
        }

        try
        {
            var embedding = await _remoteEmbeddingGenerator.GenerateAsync(text, ct);
            _cooldownState.RecordSuccess();
            return embedding;
        }
        catch (Exception ex)
        {
            var analysis = ExternalServiceFailureAnalysis.Analyze(ex);
            var warning = ExternalServiceFailureClassifier.CreateFallbackWarning(
                analysis,
                "embedding request",
                _options.Provider,
                _options.Model,
                "Local deterministic embeddings were used for this request.");
            _operationNoticeSink.AddWarning(warning);

            var transition = _cooldownState.RecordFailure(
                analysis.Kind,
                now,
                TimeSpan.FromSeconds(Math.Max(1, _options.CooldownSeconds)),
                _options.CooldownFailureThreshold);
            if (transition.EnteredCooldown)
            {
                _logger.LogWarning(
                    "Embedding provider cooldown activated until {CooldownUntil} for provider {Provider}, model {Model}, base URL {BaseUrl}, reason {Reason}.",
                    transition.CooldownUntil,
                    _options.Provider,
                    _options.Model,
                    _options.BaseUrl,
                    transition.Reason);
            }

            _logger.LogWarning(
                ex,
                "{WarningMessage}",
                warning.Message);
            return CreateDeterministicEmbedding(text);
        }
    }

    private OperationWarning CreateCooldownWarning(EmbeddingProviderCooldownSnapshot snapshot)
    {
        var reasonDescription = DescribeFailure(snapshot.Reason);
        return new OperationWarning(
            "embedding_provider_cooldown_active",
            $"The {_options.Provider} embedding provider for model '{_options.Model}' is temporarily skipping remote requests until {snapshot.CooldownUntil:O} after recent {reasonDescription}. Local deterministic embeddings were used for this request.",
            SuggestedActionFor(snapshot.Reason),
            $"Provider={_options.Provider}; Model={_options.Model}; BaseUrl={_options.BaseUrl}; CooldownUntil={snapshot.CooldownUntil:O}; Reason={snapshot.Reason}");
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

    private static string DescribeFailure(ExternalServiceFailureKind failureKind)
    {
        return failureKind switch
        {
            ExternalServiceFailureKind.ModelUnavailable => "configured model unavailability",
            ExternalServiceFailureKind.ConnectionFailed => "connectivity failures",
            ExternalServiceFailureKind.RequestTimedOut => "request timeouts",
            _ => "provider failures"
        };
    }

    private static string SuggestedActionFor(ExternalServiceFailureKind failureKind)
    {
        return failureKind switch
        {
            ExternalServiceFailureKind.ModelUnavailable => "Verify the configured model name and provider access, or switch to a supported model before retrying.",
            ExternalServiceFailureKind.ConnectionFailed => "Check network access, DNS, firewall rules, and the configured provider base URL before retrying.",
            ExternalServiceFailureKind.RequestTimedOut => "Check provider latency or switch to a faster endpoint before retrying.",
            _ => "Inspect the provider response and server logs before retrying remote embeddings."
        };
    }
}

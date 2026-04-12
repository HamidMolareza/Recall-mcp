using MemoryMcp.Application.Interfaces;
using MemoryMcp.Infrastructure.Embeddings;
using MemoryMcp.Infrastructure.Export;
using MemoryMcp.Infrastructure.Persistence;
using MemoryMcp.Infrastructure.Persistence.Repositories;
using MemoryMcp.Infrastructure.Runtime;
using MemoryMcp.Infrastructure.Text;
using MemoryMcp.Infrastructure.VectorSearch;
using MemoryMcp.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MemoryMcp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = RuntimePathResolver.ResolveSqliteConnectionString(
            configuration.GetSection("Storage")["ConnectionString"]
            ?? "Data Source=app-data/memory.db");
        var embeddingSection = configuration.GetSection("Embedding");
        var aiSection = configuration.GetSection("AI");
        var embeddingDimensions = int.TryParse(embeddingSection["Dimensions"], out var parsedDimensions)
            ? parsedDimensions
            : 256;
        var embeddingCooldownSeconds = int.TryParse(embeddingSection["CooldownSeconds"], out var parsedCooldownSeconds)
            ? parsedCooldownSeconds
            : 120;
        var embeddingCooldownFailureThreshold = int.TryParse(embeddingSection["CooldownFailureThreshold"], out var parsedCooldownFailureThreshold)
            ? parsedCooldownFailureThreshold
            : 3;
        var embeddingModel = embeddingSection["Model"] ?? "nomic-embed-text";
        var embeddingProvider = embeddingSection["Provider"] ?? "Ollama";
        var embeddingBaseUrl = embeddingSection["BaseUrl"] ?? "http://localhost:11434";
        var embeddingApiKey = ResolveSecret(
            embeddingSection,
            "ApiKey",
            "MEMORYMCP_EMBEDDING_API_KEY",
            "GAPGPT_API_KEY",
            "OPENAI_API_KEY");
        var aiProvider = aiSection["Provider"] ?? "Simple";
        var aiModel = aiSection["Model"] ?? "gapgpt-qwen-3.5";
        var aiBaseUrl = aiSection["BaseUrl"] ?? "https://api.gapgpt.app/v1";
        var aiApiKey = ResolveSecret(
            aiSection,
            "ApiKey",
            "MEMORYMCP_AI_API_KEY",
            "GAPGPT_API_KEY",
            "OPENAI_API_KEY");

        services.AddDbContext<MemoryDbContext>(options =>
            options
                .UseSqlite(
                    connectionString,
                    sqliteOptions => sqliteOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

        services.AddScoped<IMemoryRepository, MemoryRepository>();
        services.AddSingleton<IVectorStore, InMemoryVectorStore>();
        services.AddScoped<IExportService, JsonExportService>();
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton(new EmbeddingProviderOptions
        {
            Provider = embeddingProvider,
            Model = embeddingModel,
            BaseUrl = embeddingBaseUrl,
            ApiKey = embeddingApiKey,
            Dimensions = embeddingDimensions,
            CooldownSeconds = embeddingCooldownSeconds,
            CooldownFailureThreshold = embeddingCooldownFailureThreshold
        });
        services.AddSingleton(new AiSummarizerOptions
        {
            Provider = aiProvider,
            Model = aiModel,
            BaseUrl = aiBaseUrl,
            ApiKey = aiApiKey
        });

        services.AddSingleton<ITextSummarizer>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<AiSummarizerOptions>();
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("MemoryMcp.Infrastructure.ProviderSelection");

            logger.LogDebug(
                "Configuring summarizer provider {Provider} with model {Model} and endpoint {BaseUrl}",
                options.Provider,
                options.Model,
                options.BaseUrl);

            return options.Provider.ToLowerInvariant() switch
            {
                "gapgpt" or "openaicompatible" or "openai-compatible" when !string.IsNullOrWhiteSpace(options.ApiKey)
                    => ActivatorUtilities.CreateInstance<OpenAiResponseSummarizer>(serviceProvider),
                _ => ActivatorUtilities.CreateInstance<SimpleTextSummarizer>(serviceProvider)
            };
        });

        services.AddHttpClient<OllamaEmbeddingProvider>(client =>
        {
            client.BaseAddress = new Uri(embeddingBaseUrl);
        });
        services.AddSingleton<EmbeddingProviderCooldownState>();
        services.AddScoped<IRemoteEmbeddingGenerator, OpenAiCompatibleRemoteEmbeddingGenerator>();
        services.AddScoped<DeterministicEmbeddingProvider>();
        services.AddScoped<IEmbeddingProvider>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<EmbeddingProviderOptions>();
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("MemoryMcp.Infrastructure.ProviderSelection");
            var providerKey = options.Provider.Trim().ToLowerInvariant();

            logger.LogDebug(
                "Configuring embedding provider {Provider} with model {Model} and endpoint {BaseUrl}",
                options.Provider,
                options.Model,
                options.BaseUrl);

            return providerKey switch
            {
                "gapgpt" or "openaicompatible" or "openai-compatible" when !string.IsNullOrWhiteSpace(options.ApiKey)
                    => LogSelectedEmbeddingProvider(
                        logger,
                        ActivatorUtilities.CreateInstance<OpenAiCompatibleEmbeddingProvider>(serviceProvider),
                        "OpenAI-compatible"),
                "gapgpt" or "openaicompatible" or "openai-compatible"
                    => LogSelectedEmbeddingProvider(
                        logger,
                        serviceProvider.GetRequiredService<DeterministicEmbeddingProvider>(),
                        "Deterministic (missing API key)",
                        LogLevel.Warning,
                        "Embedding provider {Provider} is configured without an API key; using deterministic local embeddings.",
                        options.Provider),
                "ollama"
                    => LogSelectedEmbeddingProvider(
                        logger,
                        serviceProvider.GetRequiredService<OllamaEmbeddingProvider>(),
                        "Ollama"),
                _ => LogSelectedEmbeddingProvider(
                    logger,
                    serviceProvider.GetRequiredService<DeterministicEmbeddingProvider>(),
                    "Deterministic (unknown provider)",
                    LogLevel.Warning,
                    "Embedding provider {Provider} is not recognized; using deterministic local embeddings.",
                    options.Provider)
            };
        });

        return services;
    }

    private static string ResolveSecret(
        IConfigurationSection section,
        string keyName,
        string environmentVariableName,
        params string[] fallbackEnvironmentVariableNames)
    {
        var explicitValue = Environment.GetEnvironmentVariable(environmentVariableName);
        if (!string.IsNullOrWhiteSpace(explicitValue))
        {
            return explicitValue;
        }

        foreach (var fallbackName in fallbackEnvironmentVariableNames)
        {
            var fallbackValue = Environment.GetEnvironmentVariable(fallbackName);
            if (!string.IsNullOrWhiteSpace(fallbackValue))
            {
                return fallbackValue;
            }
        }

        return section[keyName] ?? string.Empty;
    }

    private static IEmbeddingProvider LogSelectedEmbeddingProvider(
        ILogger logger,
        IEmbeddingProvider provider,
        string resolvedProvider,
        LogLevel logLevel = LogLevel.Information,
        string? messageTemplate = null,
        string? configuredProvider = null)
    {
        logger.Log(logLevel, messageTemplate ?? "Resolved embedding provider implementation {ResolvedProvider}", configuredProvider ?? resolvedProvider);
        if (messageTemplate is not null)
        {
            logger.LogDebug("Resolved embedding provider implementation {ResolvedProvider}", resolvedProvider);
        }

        return provider;
    }
}

file sealed class DeterministicEmbeddingProvider : IEmbeddingProvider
{
    private readonly EmbeddingProviderOptions _options;
    private readonly IOperationNoticeSink _operationNoticeSink;

    public DeterministicEmbeddingProvider(
        EmbeddingProviderOptions options,
        IOperationNoticeSink operationNoticeSink)
    {
        _options = options;
        _operationNoticeSink = operationNoticeSink;
    }

    public Task<EmbeddingVector> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var providerKey = _options.Provider.Trim().ToLowerInvariant();
        if (providerKey is "gapgpt" or "openaicompatible" or "openai-compatible")
        {
            _operationNoticeSink.AddWarning(new OperationWarning(
                "embedding_provider_not_configured",
                $"The {_options.Provider} embedding provider is not fully configured, so local deterministic embeddings were used.",
                "Set the provider API key and verify the provider configuration if you need remote embeddings.",
                $"Provider={_options.Provider}; Model={_options.Model}; BaseUrl={_options.BaseUrl}"));
        }
        else if (providerKey != "ollama")
        {
            _operationNoticeSink.AddWarning(new OperationWarning(
                "embedding_provider_unknown",
                $"The configured embedding provider '{_options.Provider}' is not recognized, so local deterministic embeddings were used.",
                "Use a supported provider name such as Ollama or GapGpt/OpenAI-compatible.",
                $"Provider={_options.Provider}; Model={_options.Model}; BaseUrl={_options.BaseUrl}"));
        }

        return Task.FromResult(CreateDeterministicEmbedding(text));
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

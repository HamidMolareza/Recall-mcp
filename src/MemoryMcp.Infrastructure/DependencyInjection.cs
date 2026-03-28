using MemoryMcp.Application.Interfaces;
using MemoryMcp.Infrastructure.Embeddings;
using MemoryMcp.Infrastructure.Export;
using MemoryMcp.Infrastructure.Persistence;
using MemoryMcp.Infrastructure.Persistence.Repositories;
using MemoryMcp.Infrastructure.Text;
using MemoryMcp.Infrastructure.VectorSearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MemoryMcp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetSection("Storage")["ConnectionString"]
                               ?? "Data Source=memory.db";
        var embeddingSection = configuration.GetSection("Embedding");
        var aiSection = configuration.GetSection("AI");
        var embeddingDimensions = int.TryParse(embeddingSection["Dimensions"], out var parsedDimensions)
            ? parsedDimensions
            : 256;
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
            options.UseSqlite(connectionString));

        services.AddScoped<IMemoryRepository, MemoryRepository>();
        services.AddSingleton<IVectorStore, InMemoryVectorStore>();
        services.AddScoped<IExportService, JsonExportService>();
        services.AddSingleton(new EmbeddingProviderOptions
        {
            Provider = embeddingProvider,
            Model = embeddingModel,
            BaseUrl = embeddingBaseUrl,
            ApiKey = embeddingApiKey,
            Dimensions = embeddingDimensions
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
        services.AddScoped<IEmbeddingProvider>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<EmbeddingProviderOptions>();
            return options.Provider.ToLowerInvariant() switch
            {
                "gapgpt" or "openaicompatible" or "openai-compatible" when !string.IsNullOrWhiteSpace(options.ApiKey)
                    => ActivatorUtilities.CreateInstance<OpenAiCompatibleEmbeddingProvider>(serviceProvider),
                _ => ActivatorUtilities.CreateInstance<OllamaEmbeddingProvider>(serviceProvider)
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
}

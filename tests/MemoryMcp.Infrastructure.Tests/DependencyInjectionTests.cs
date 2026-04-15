using FluentAssertions;
using MemoryMcp.Application;
using MemoryMcp.Application.Interfaces;
using MemoryMcp.Infrastructure.Embeddings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MemoryMcp.Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_WithMetisaiProviderAndApiKey_ResolvesOpenAiCompatibleEmbeddingProvider()
    {
        using var provider = CreateServiceProvider(new Dictionary<string, string?>
        {
            ["Embedding:Provider"] = "Metisai",
            ["Embedding:Model"] = "text-embedding-3-small",
            ["Embedding:BaseUrl"] = "https://api.metisai.ir/api/v1/wrapper/metis",
            ["Embedding:ApiKey"] = "test-key"
        });

        using var scope = provider.CreateScope();

        var embeddingProvider = scope.ServiceProvider.GetRequiredService<IEmbeddingProvider>();

        embeddingProvider.Should().BeOfType<OpenAiCompatibleEmbeddingProvider>();
    }

    [Fact]
    public async Task AddInfrastructure_WithMetisaiProviderAndMissingApiKey_UsesConfiguredProviderWarning()
    {
        using var provider = CreateServiceProvider(new Dictionary<string, string?>
        {
            ["Embedding:Provider"] = "Metisai",
            ["Embedding:Model"] = "text-embedding-3-small",
            ["Embedding:BaseUrl"] = "https://api.metisai.ir/api/v1/wrapper/metis",
            ["Embedding:ApiKey"] = string.Empty
        });

        using var scope = provider.CreateScope();

        var embeddingProvider = scope.ServiceProvider.GetRequiredService<IEmbeddingProvider>();
        var operationNoticeSink = scope.ServiceProvider.GetRequiredService<IOperationNoticeSink>();

        _ = await embeddingProvider.GenerateEmbeddingAsync("provider alias smoke test");
        var warnings = operationNoticeSink.DrainWarnings();

        warnings.Should().ContainSingle(warning => warning.Code == "embedding_provider_not_configured");
        warnings.Should().NotContain(warning => warning.Code == "embedding_provider_unknown");
    }

    private static ServiceProvider CreateServiceProvider(IReadOnlyDictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new ServiceCollection()
            .AddLogging()
            .AddApplication()
            .AddInfrastructure(configuration)
            .BuildServiceProvider();
    }
}

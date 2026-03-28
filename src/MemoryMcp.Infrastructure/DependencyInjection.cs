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
        var embeddingDimensions = int.TryParse(configuration.GetSection("Embedding")["Dimensions"], out var parsedDimensions)
            ? parsedDimensions
            : 256;
        var embeddingModel = configuration.GetSection("Embedding")["Model"] ?? "nomic-embed-text";

        services.AddDbContext<MemoryDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IMemoryRepository, MemoryRepository>();
        services.AddSingleton<IVectorStore, InMemoryVectorStore>();
        services.AddScoped<IExportService, JsonExportService>();
        services.AddSingleton(new EmbeddingProviderOptions
        {
            Model = embeddingModel,
            Dimensions = embeddingDimensions
        });
        services.AddSingleton<ITextSummarizer, SimpleTextSummarizer>();

        services.AddHttpClient<OllamaEmbeddingProvider>(client =>
        {
            var baseUrl = configuration.GetSection("Embedding")["BaseUrl"] ?? "http://localhost:11434";
            client.BaseAddress = new Uri(baseUrl);
        });
        services.AddScoped<IEmbeddingProvider, OllamaEmbeddingProvider>();

        return services;
    }
}

using MemoryMcp.Application.Interfaces;
using MemoryMcp.Infrastructure.Embeddings;
using MemoryMcp.Infrastructure.Export;
using MemoryMcp.Infrastructure.Persistence;
using MemoryMcp.Infrastructure.Persistence.Repositories;
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

        services.AddDbContext<MemoryDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IMemoryRepository, MemoryRepository>();
        services.AddSingleton<IVectorStore, InMemoryVectorStore>();
        services.AddScoped<IExportService, JsonExportService>();

        services.AddHttpClient<OllamaEmbeddingProvider>(client =>
        {
            var baseUrl = configuration.GetSection("Embedding")["BaseUrl"] ?? "http://localhost:11434";
            client.BaseAddress = new Uri(baseUrl);
        });
        services.AddScoped<IEmbeddingProvider, OllamaEmbeddingProvider>();

        return services;
    }
}

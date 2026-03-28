using FluentValidation;
using MemoryMcp.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MemoryMcp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<MemoryService>();
        services.AddScoped<SearchService>();
        services.AddScoped<ContradictionDetector>();
        services.AddScoped<DeduplicationService>();
        services.AddScoped<StalenessService>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}

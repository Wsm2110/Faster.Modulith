using Faster.Modulith.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Faster.Modulith;

public static class MOdulithExtensions
{
    public static IServiceCollection AddModulith(this IServiceCollection services)
    {
        // Register the implementation as Scoped so it can resolve other Scoped services (like EF Core DbContexts)
        services.AddScoped<IOrchestrator, Orchestrator>();
        return services;
    }
}


using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Module.Kitchen.Infrastructure; // Note that we have to use this namespace.

/// <summary>
/// Provides extension methods for configuring kitchen-related services in the dependency injection container.
/// </summary>
/// <remarks>This class contains methods that facilitate the registration of kitchen services, such as the
/// database context, into the service collection. It is intended to be used in the startup configuration of an
/// application.</remarks>
public partial class KitchenExtensions
{
    /// <summary>
    /// Configures infrastructure-related services for the Kitchen module and registers them with the dependency
    /// injection container.
    /// </summary>
    /// <remarks>This method is typically called during application startup to ensure that required
    /// infrastructure services, such as the KitchenDbContext, are available for dependency injection throughout the
    /// application.</remarks>
    /// <param name="services">The service collection to which infrastructure services are added. Cannot be null.</param>
    static partial void AddInfrastructure(IServiceCollection services)
    {
        services.AddScoped<KitchenDbContext>();

        // 4. Register your DbContext here
        services.AddDbContext<KitchenDbContext>(options =>
        {
            // Hardcoded for example, usually you'd resolve IConfiguration to get connection string
            options.UseMemoryCache(new MemoryCache(new MemoryDistributedCacheOptions()));
        });
    }
}
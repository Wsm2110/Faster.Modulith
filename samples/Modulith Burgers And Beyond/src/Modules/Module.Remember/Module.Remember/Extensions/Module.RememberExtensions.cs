using Microsoft.Extensions.DependencyInjection;
using Module.Remember.Infrastructure;

namespace Module.Remember.Infrastructure;

public static partial class RememberExtensions
{
    static partial void AddInfrastructure(IServiceCollection services)
    {
        // Automatically generated DbContext registration
        services.AddScoped<RememberDbContext>();
    }
}

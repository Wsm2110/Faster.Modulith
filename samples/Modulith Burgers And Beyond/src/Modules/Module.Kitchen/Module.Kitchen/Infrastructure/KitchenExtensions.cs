using Microsoft.Extensions.DependencyInjection;
using Module.Kitchen.Infrastructure;

namespace Faster.Modulith; // Note that we have to use this namespace.

public partial class KitchenExtensions
{
    static partial void AddInfrastructure(IServiceCollection services)
    {
        services.AddScoped<KitchenDbContext>();
    }
}
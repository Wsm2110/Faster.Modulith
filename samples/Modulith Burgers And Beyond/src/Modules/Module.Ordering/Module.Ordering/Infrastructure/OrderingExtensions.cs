using Microsoft.Extensions.DependencyInjection;
using Module.Ordering.Domain;

namespace Module.Ordering.Infrastructure; // Note that we have to use this namespace.

public partial class OrderingExtensions
{
    static partial void AddInfrastructure(IServiceCollection services)
    {
        services.AddScoped<OrderingDbContext>();
    }
}


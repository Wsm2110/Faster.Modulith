using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Module.Ordering.Infrastructure; // Note that we have to use this namespace.

public partial class OrderingExtensions
{
    static partial void AddInfrastructure(IServiceCollection services)
    {
        services.AddScoped<OrderingDbContext>();

        services.AddDbContext<OrderingDbContext>(options =>
        {
            // Configuration logic matches your previous InMemory intent
            options.UseInMemoryDatabase("OrderingDb");
        });
    }
}


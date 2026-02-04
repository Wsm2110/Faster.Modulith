using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Module.Ordering.Domain;

namespace Faster.Modulith; // Note that we have to use this namespace.

public partial class OrderingExtensions
{
    static partial void AddInfrastructure(IServiceCollection services)
    {
        services.AddScoped<OrderingDbContext>();
    }
}


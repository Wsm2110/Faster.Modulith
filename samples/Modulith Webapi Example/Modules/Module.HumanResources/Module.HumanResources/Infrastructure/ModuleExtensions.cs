using Faster.Modulith.Behaviors;
using Faster.Modulith.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Module.HumanResources.Infrastructure;

//Note keep in mind while using a partial method we have to use same namespace as defined in the generated code
namespace Faster.Modulith;

// Partial class to extend the generated ModulithExtensions class
public static partial class HumanResourcesExtensions
{
    // Implement the partial method to add your Repositories
    static partial void AddInfrastructure(IServiceCollection services)
    {
        services.AddScoped<EmployeeRepository>();
      //  services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    }
}
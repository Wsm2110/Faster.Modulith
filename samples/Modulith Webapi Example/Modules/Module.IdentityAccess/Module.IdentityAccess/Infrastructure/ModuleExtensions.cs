using Microsoft.Extensions.DependencyInjection;
using Module.IdentityAccess.Infrastructure;

//Note keep in mind while using a partial method we have to use same namespace as defined in the generated code
namespace Faster.Modulith;

public static partial class IdentityAccessExtensions
{
    // Implement the partial method to add your Repositories
    static partial void AddInfrastructure(IServiceCollection services)
    {
        services.AddScoped<ActiveDirectoryGateway>();
    }
}

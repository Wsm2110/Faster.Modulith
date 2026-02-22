using Microsoft.Extensions.DependencyInjection;

namespace Module.MyFirstModule.Infrastructure;

/// <summary>
/// Extension methods for registering module-specific dependencies.
/// </summary>
public static partial class MyFirstModuleExtensions
{
    /// <summary>
    /// Adds infrastructure dependencies.
    /// </summary>
    static partial void AddInfrastructure(IServiceCollection services)
    {
    }
}

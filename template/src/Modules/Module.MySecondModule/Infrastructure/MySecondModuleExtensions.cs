using Microsoft.Extensions.DependencyInjection;

namespace Module.MySecondModule.Infrastructure;

/// <summary>
/// Extension methods for registering module-specific dependencies.
/// </summary>
public static partial class MySecondModuleExtensions
{
    /// <summary>
    /// Adds infrastructure dependencies.
    /// </summary>
    static partial void AddInfrastructure(IServiceCollection services)
    {
    }
}

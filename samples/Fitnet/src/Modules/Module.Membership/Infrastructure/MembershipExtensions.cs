using Microsoft.Extensions.DependencyInjection;
using Module.Membership.Infrastructure;

namespace Module.Membership.Infrastructure;

/// <summary>
/// Extension methods for registering module-specific dependencies.
/// </summary>
public static partial class MembershipExtensions
{
    /// <summary>
    /// Adds infrastructure dependencies including the DbContext.
    /// </summary>
    static partial void AddInfrastructure(IServiceCollection services)
    {
       
    }
}

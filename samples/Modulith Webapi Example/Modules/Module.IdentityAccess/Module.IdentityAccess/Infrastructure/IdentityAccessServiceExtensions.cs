using Microsoft.Extensions.DependencyInjection; // Access to IServiceCollection
using Module.IdentityAccess.Infrastructure;

// NOTICE: The Namespace
// This file usually lives in the 'Host' or 'WebAPI' project (the startup project), 
// OR in the Module's own assembly if you want to keep the configuration self-contained.
namespace Faster.Modulith;

/// <summary>
/// The "Plug" for the Identity Module.
/// <para>
/// <b>Why exists?</b> To keep the main `Program.cs` clean. 
/// Instead of writing 500 lines of `builder.Services.AddScoped...` for every single class in every module,
/// each module provides one simple extension method: "AddIdentityAccess()".
/// </para>
/// </summary>
public static class IdentityAccessServiceExtensions
{
    /// <summary>
    /// Extension method to register all dependencies for IdentityAccess.
    /// </summary>
    // SYNTAX: 'this IServiceCollection services' makes this an "Extension Method".
    // It allows you to write 'services.AddIdentityAccessInfrastructure()' in Program.cs.
    public static IServiceCollection AddIdentityAccessInfrastructure(this IServiceCollection services)
    {
        // HOW: We register the Gateway implementation.
        // LIFECYCLE: 'AddScoped' means "Create one instance per HTTP Request".
        // This is the standard for database connections and gateways.

        // VISIBILITY TRICK:
        // 'ActiveDirectoryGateway' is an INTERNAL class. 
        // Normal code in Program.cs cannot see it to register it.
        // However, because this Extension Class is likely inside the same project/assembly as the Gateway,
        // it *can* see the internal class. It acts as a bridge, exposing the *ability* to register it 
        // without exposing the class itself.
        services.AddScoped<ActiveDirectoryGateway>();

        // MISSING REGISTRATIONS?
        // In a full implementation, you would also register your Handlers and Validators here.
        // Libraries like 'Scrutor' or 'Faster.Modulith' often do this automatically by scanning the assembly.
        // e.g. services.AddValidatorsFromAssemblyContaining<SomeValidator>();

        return services;
    }
}
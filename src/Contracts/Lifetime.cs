using System;
using Microsoft.Extensions.DependencyInjection;

namespace Faster.Modulith.Contracts;

/// <summary>
/// Specifies the service lifetime for an automatically registered handler.
/// This attribute is consumed by the Source Generator to determine how to register 
/// the handler in the <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>
/// If this attribute is not applied, the Source Generator defaults to <see cref="ServiceLifetime.Transient"/>.
/// </remarks>
/// <example>
/// <code>
/// [HandlerLifetime(ServiceLifetime.Scoped)]
/// public class CreateMissionHandler : ICommandHandler&lt;CreateMissionCommand, Guid&gt;
/// {
///     // This handler will now be registered as Scoped in the DI container.
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class HandlerLifetimeAttribute : Attribute
{
    /// <summary>
    /// Gets the specified <see cref="ServiceLifetime"/> for the handler.
    /// </summary>
    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HandlerLifetimeAttribute"/> class.
    /// </summary>
    /// <param name="lifetime">
    /// The <see cref="ServiceLifetime"/> to use for registration (Singleton, Scoped, or Transient).
    /// </param>
    public HandlerLifetimeAttribute(ServiceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}
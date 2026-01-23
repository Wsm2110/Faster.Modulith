using System;

namespace Faster.Modulith.Contracts;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class EnrichWithAttribute : Attribute
{
    // The type of the middleware (e.g., typeof(LoggingBehavior<,>))
    public Type BehaviorType { get; }

    public EnrichWithAttribute(Type behaviorType)
    {
        BehaviorType = behaviorType;
    }
}
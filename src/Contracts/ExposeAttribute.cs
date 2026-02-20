using System;
using System.Collections.Generic;
using System.Text;

namespace Faster.Modulith.Contracts;

[global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class ExposeAttribute(string route, string httpVerb = "POST") : global::System.Attribute
{
    public string Route { get; } = route;
    public string HttpVerb { get; } = httpVerb;
}

using Microsoft.CodeAnalysis;

namespace Faster.Modulith.Analyzers.Generators;

[Generator]
public class BypassAttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "ArchitectureBypassAttribute.g.cs",
            @"using System;

namespace Faster.Modulith
{
    /// <summary>
    /// suppress architectural violations.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class ArchitectureBypassAttribute : Attribute
    {
        public string RuleId { get; }
        public string Reason { get; }
        public string WorkItem { get; }

        /// <param name=""ruleId"">The MODxxx rule being bypassed.</param>
        /// <param name=""reason"">Technical justification.</param>
        /// <param name=""workItem"">Jira/DevOps Ticket ID (e.g., 'PO-123').</param>
        public ArchitectureBypassAttribute(string ruleId, string reason, string workItem)
        {
            RuleId = ruleId;
            Reason = reason;
            WorkItem = workItem;
        }
    }
}"));
    }
}
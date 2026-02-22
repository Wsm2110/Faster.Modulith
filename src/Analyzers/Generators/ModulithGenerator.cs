using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Faster.Modulith.Analyzers.Generators
{
    /// <summary>
    /// Represents the different artifact types that can be discovered
    /// during assembly scanning.
    /// </summary>
    public enum ArtifactType
    {
        /// <summary>Represents a use case definition or handler.</summary>
        UseCase,

        /// <summary>Represents a command handler.</summary>
        Command,

        /// <summary>Represents an event or event handler.</summary>
        Event,

        /// <summary>Represents a pipeline behavior.</summary>
        Pipeline
    }

    /// <summary>
    /// Represents a constructor or method parameter.
    /// </summary>
    /// <param name="Type">Fully qualified parameter type name.</param>
    /// <param name="Name">Parameter name.</param>
    public record ParameterInfo(string Type, string Name);

    /// <summary>
    /// Contains metadata about HTTP exposure for a use case.
    /// </summary>
    /// <param name="RawAttributeLine">
    /// Fully qualified attribute declaration used for code generation.
    /// </param>
    /// <param name="Route">The HTTP route template.</param>
    /// <param name="HttpMethod">The HTTP method (GET, POST, etc.).</param>
    public record ExposeInfo(string RawAttributeLine, string Route, string HttpMethod);

    /// <summary>
    /// Shared context object passed to generator components
    /// to avoid excessive parameter lists.
    /// </summary>
    /// <param name="ModuleName">Logical module name.</param>
    /// <param name="AssemblyName">Current assembly name.</param>
    /// <param name="HasManualPipelines">
    /// Indicates whether manual IPipelineBehavior registrations were detected.
    /// </param>
    /// <param name="IsAspNetCore">True if ASP.NET Core types are referenced.</param>
    /// <param name="IsApiProject">True if assembly is an API project.</param>
    /// <param name="IsModuleProject">True if assembly is a module project.</param>
    internal record GeneratorContext(
        string ModuleName,
        string AssemblyName,
        bool HasManualPipelines,
        bool IsAspNetCore,
        bool IsApiProject,
        bool IsModuleProject
    );

    /// <summary>
    /// Represents a discovered handler, use case definition,
    /// event definition, or pipeline behavior.
    /// </summary>
    public record HandlerInfo(
        string ImplementationType,
        string ServiceInterfaceType,
        string SimpleRequestName,
        string ResponseType,
        ArtifactType Type,
        bool IsHandler,
        List<ParameterInfo> Parameters,
        List<string> PipelineBehaviors,
        List<string> Attributes,
        ExposeInfo ExposeData = null)
    {
        /// <summary>
        /// Gets the simplified event name derived from generic interface arguments.
        /// </summary>
        public string EventSimpleName
        {
            get
            {
                var clean = ServiceInterfaceType;

                if (clean.Contains("<") && clean.Contains(">"))
                {
                    int start = clean.IndexOf("<") + 1;
                    int end = clean.LastIndexOf(">");
                    if (end > start)
                        clean = clean.Substring(start, end - start);
                }

                return clean.Split('.').Last();
            }
        }

        /// <summary>
        /// Gets the simple handler class name.
        /// </summary>
        public string HandlerSimpleName =>
            ImplementationType?.Split('.').Last() ?? SimpleRequestName;

        /// <summary>
        /// Overrides default record equality to evaluate underlying collection contents,
        /// preventing infinite Roslyn generation loops.
        /// </summary>
        public virtual bool Equals(HandlerInfo other)
        {
            if (other is null) return false;

            return ImplementationType == other.ImplementationType &&
                   ServiceInterfaceType == other.ServiceInterfaceType &&
                   SimpleRequestName == other.SimpleRequestName &&
                   ResponseType == other.ResponseType &&
                   Type == other.Type &&
                   IsHandler == other.IsHandler &&
                   (ExposeData?.Equals(other.ExposeData) ?? other.ExposeData is null) &&
                   Parameters.SequenceEqual(other.Parameters) &&
                   PipelineBehaviors.SequenceEqual(other.PipelineBehaviors) &&
                   Attributes.SequenceEqual(other.Attributes);
        }

        /// <summary>
        /// Generates a deterministic hash code based on primary immutable identifiers.
        /// Uses unchecked prime multiplication for .NET Standard 2.0 compatibility.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (ImplementationType?.GetHashCode() ?? 0);
                hash = hash * 31 + (ServiceInterfaceType?.GetHashCode() ?? 0);
                hash = hash * 31 + Type.GetHashCode();
                hash = hash * 31 + IsHandler.GetHashCode();
                return hash;
            }
        }
    }

    /// <summary>
    /// Main incremental source generator responsible for:
    /// - Scanning the compilation
    /// - Building module context
    /// - Coordinating diagram and code generation
    /// </summary>
    [Generator]
    public sealed class ModulithGenerator : IIncrementalGenerator
    {
        /// <summary>
        /// Compiled regular expression for extracting module names from assembly boundaries.
        /// </summary>
        private static readonly Regex ModuleNameRegex = new(
            @"(?:^|\.)Modules?\.(\w+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Configures the incremental generator pipeline.
        /// </summary>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var scanResult = context.CompilationProvider
                .Select((c, ct) => Scanner.ScanTypes(c, ct));

            var assembly = context.CompilationProvider
                .Select((c, ct) => c.AssemblyName);

            var fullData = scanResult
                .Combine(assembly)
                .Combine(context.CompilationProvider);

            context.RegisterSourceOutput(fullData, (spc, source) =>
            {
                var ((scanInfo, asmName), compilation) = source;

                Execute(
                    spc,
                    scanInfo.Items,
                    scanInfo.HasManualPipelines,
                    compilation);
            });
        }

        /// <summary>
        /// Entry point for generation execution.
        /// Responsible for delegating to specialized generators.
        /// </summary>
        private static void Execute(
            SourceProductionContext context,
            List<HandlerInfo> items,
            bool hasManualPipelines,
            Compilation compilation)
        {
            string assemblyName = compilation.AssemblyName;

            if (string.IsNullOrWhiteSpace(assemblyName) ||
                assemblyName.IndexOf("test", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return;
            }

            var match = ModuleNameRegex.Match(assemblyName);

            string moduleName = match.Success
                ? match.Groups[1].Value
                : assemblyName.Split('.').Last();

            bool isAspNetCore =
                compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Builder.IEndpointRouteBuilder") != null ||
                compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.IResult") != null;

            var ctx = new GeneratorContext(
                ModuleName: moduleName,
                AssemblyName: assemblyName,
                HasManualPipelines: hasManualPipelines,
                IsAspNetCore: isAspNetCore,
                IsApiProject: assemblyName.EndsWith(".Api", StringComparison.OrdinalIgnoreCase),
                IsModuleProject: assemblyName.IndexOf("Module", StringComparison.OrdinalIgnoreCase) >= 0
            );

            // Diagram generation
            var referencedModules = compilation.SourceModule.ReferencedAssemblySymbols
                .Where(a =>
                    a.Name.Contains("Module") &&
                    !a.Name.Contains("Faster.Modulith") &&
                    !a.Name.Contains("Analyzers"))
                .ToList();

            if (referencedModules.Any() || ctx.IsModuleProject)
            {
                DiagramGenerator.Generate(context, referencedModules, assemblyName);
            }

            // Artifact grouping unified to reduce iteration overhead
            var definitions = new List<HandlerInfo>();
            var handlers = new List<HandlerInfo>();
            var commands = new List<HandlerInfo>();
            var events = new List<HandlerInfo>();

            string apiNamespacePart = assemblyName.Replace(".Module", ".Api");

            foreach (var item in items)
            {
                if (item.IsHandler)
                {
                    handlers.Add(item);
                    if (item.Type == ArtifactType.Command)
                    {
                        commands.Add(item);
                    }
                }
                else
                {
                    if (item.Type == ArtifactType.UseCase)
                    {
                        definitions.Add(item);
                    }
                    else if (item.Type == ArtifactType.Event && item.ServiceInterfaceType.Contains(apiNamespacePart))
                    {
                        events.Add(item);
                    }
                }
            }

            if (ctx.IsApiProject)
            {
                // Strict API Boundary: Generates the public interface, concrete facade module, and extensions
                ModuleApiGenerator.Generate(context, definitions, ctx);
            }
            else if (ctx.IsModuleProject)
            {
                // Module Internals
                DispatcherGenerator.Generate(context, commands, events, ctx);
                RegistrationGenerator.Generate(context, handlers, ctx);

                // Endpoints mapped directly to internal application execution
                if (ctx.IsAspNetCore)
                {
                    EndpointGenerator.Generate(context, handlers, items, ctx);
                }
            }
        }
    }
}
using Microsoft.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Faster.Modulith.Analyzers.Generators;

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
            return;

        var match = Regex.Match(
            assemblyName,
            @"(?:^|\.)Modules?\.(\w+)",
            RegexOptions.IgnoreCase);

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

        // Artifact grouping
        var definitions = items
            .Where(x => !x.IsHandler && x.Type == ArtifactType.UseCase)
            .ToList();

        var handlers = items
            .Where(x => x.IsHandler)
            .ToList();

        var commands = handlers
            .Where(h => h.Type == ArtifactType.Command)
            .ToList();

        string apiNamespacePart = assemblyName.Replace(".Module", ".Api");

        var events = items
            .Where(x =>
                x.Type == ArtifactType.Event &&
                !x.IsHandler &&
                x.ServiceInterfaceType.Contains(apiNamespacePart))
            .ToList();

        if (ctx.IsApiProject)
        {
            InterfaceGenerator.Generate(context, definitions, ctx);
        }
        else if (ctx.IsModuleProject)
        {
            DispatcherGenerator.Generate(context, commands, events, ctx);
            RegistrationGenerator.Generate(context, handlers, ctx);
            ModuleFacadeGenerator.Generate(context, definitions, ctx);

            var interfaceType = compilation
                .GetTypeByMetadataName($"Faster.Modulith.I{moduleName}Module");

            if (interfaceType == null)
            {
                InterfaceGenerator.Generate(context, definitions, ctx);
            }

            if (ctx.IsAspNetCore)
            {
                EndpointGenerator.Generate(context, handlers, items, ctx);
            }
        }
    }
}
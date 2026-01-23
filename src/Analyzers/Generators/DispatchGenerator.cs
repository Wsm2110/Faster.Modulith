using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Faster.Modulith.Analyzers.Generators;

// ==========================================
// 1. DATA STRUCTURES
// ==========================================
public enum ArtifactType { UseCase, Command, Event, Pipeline }

public record ParameterInfo(string Type, string Name);

public record HandlerInfo(
    string ImplementationType,
    string ServiceInterfaceType,
    string SimpleRequestName,
    string ResponseType,
    ArtifactType Type,
    bool IsHandler,
    List<ParameterInfo> Parameters,
    List<string> PipelineBehaviors
);

// ==========================================
// 2. THE SCANNER 
// ==========================================
public static class Scanner
{
    private static readonly SymbolDisplayFormat StrictFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included)
        .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers)
        .WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType);

    private static string GetFullTypeName(ITypeSymbol symbol)
    {
        if (symbol == null) return "void";
        return symbol.ToDisplayString(StrictFormat);
    }

    private static string GetUnboundTypeName(INamedTypeSymbol symbol)
    {
        if (symbol.IsGenericType)
            return symbol.ConstructUnboundGenericType().ToDisplayString(StrictFormat);
        return symbol.ToDisplayString(StrictFormat);
    }

    public static (List<HandlerInfo> Items, bool HasManualPipelines) ScanLocalTypes(Compilation compilation, CancellationToken ct)
    {
        var results = new List<HandlerInfo>();
        bool hasManualPipelines = false;

        foreach (var tree in compilation.SyntaxTrees)
        {
            var root = tree.GetRoot(ct);

            // 1. INSPECT USER CODE FOR MANUAL REGISTRATIONS
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Where(m => m.Identifier.Text == "AddInfrastructure");

            foreach (var method in methods)
            {
                if (method.Body != null)
                {
                    bool distinctUsage = method.Body.DescendantNodes()
                        .Any(node =>
                            (node is IdentifierNameSyntax id && id.Identifier.Text == "IPipelineBehavior") ||
                            (node is GenericNameSyntax gen && gen.Identifier.Text == "IPipelineBehavior"));

                    if (distinctUsage) hasManualPipelines = true;
                }
                else if (method.ExpressionBody != null)
                {
                    bool distinctUsage = method.ExpressionBody.DescendantNodes()
                         .Any(node =>
                            (node is IdentifierNameSyntax id && id.Identifier.Text == "IPipelineBehavior") ||
                            (node is GenericNameSyntax gen && gen.Identifier.Text == "IPipelineBehavior"));

                    if (distinctUsage) hasManualPipelines = true;
                }
            }

            // 2. STANDARD TYPE SCANNING
            var semanticModel = compilation.GetSemanticModel(tree);
            var types = root.DescendantNodes().OfType<TypeDeclarationSyntax>();

            foreach (var typeDecl in types)
            {
                if (semanticModel.GetDeclaredSymbol(typeDecl, ct) is not INamedTypeSymbol symbol)
                    continue;

                if (symbol.IsAbstract || symbol.IsStatic) continue;

                // Scan for Pipeline Implementations
                foreach (var iface in symbol.AllInterfaces)
                {
                    if (iface.Name == "IPipelineBehavior")
                    {
                        bool isGeneric = symbol.IsGenericType;
                        string implType = isGeneric ? GetUnboundTypeName(symbol) : GetFullTypeName(symbol);
                        string interfaceType = isGeneric
                            ? "global::Faster.Modulith.Contracts.IPipelineBehavior<,>"
                            : $"global::Faster.Modulith.Contracts.IPipelineBehavior<{GetFullTypeName(iface.TypeArguments[0])}, {GetFullTypeName(iface.TypeArguments[1])}>";

                        results.Add(new HandlerInfo(
                            ImplementationType: implType,
                            ServiceInterfaceType: interfaceType,
                            SimpleRequestName: symbol.Name,
                            ResponseType: "void",
                            Type: ArtifactType.Pipeline,
                            IsHandler: false,
                            Parameters: new(),
                            PipelineBehaviors: new()
                        ));
                    }
                }

                // Scan Handlers & UseCases
                foreach (var iface in symbol.AllInterfaces)
                {
                    if (iface.Name == "IEvent")
                    {
                        AddDefinition(results, symbol, null, ArtifactType.Event);
                        continue;
                    }

                    if (!iface.IsGenericType) continue;

                    if (iface.Name == "IUseCase")
                    {
                        AddDefinition(results, symbol, iface.TypeArguments[0], ArtifactType.UseCase);
                        continue;
                    }

                    ArtifactType? handlerType = iface.Name switch
                    {
                        "ICommandHandler" => ArtifactType.Command,
                        "IUseCaseHandler" => ArtifactType.UseCase,
                        "IEventHandler" => ArtifactType.Event,
                        _ => null
                    };

                    if (handlerType == null) continue;
                    if (iface.TypeArguments.Length == 0) continue;

                    var requestSymbol = iface.TypeArguments[0];
                    var responseSymbol = iface.TypeArguments.Length > 1 ? iface.TypeArguments[1] : null;

                    var handlerBehaviors = ExtractPipelineBehaviors(symbol);
                    var requestBehaviors = ExtractPipelineBehaviors(requestSymbol);
                    var allBehaviors = handlerBehaviors.Concat(requestBehaviors).Distinct().ToList();

                    results.Add(Create(
                        implType: GetFullTypeName(symbol),
                        reqSymbol: requestSymbol,
                        resSymbol: responseSymbol,
                        type: handlerType.Value,
                        isHandler: true,
                        behaviors: allBehaviors
                    ));
                }
            }
        }
        return (results, hasManualPipelines);
    }

    private static List<string> ExtractPipelineBehaviors(ISymbol symbol)
    {
        var behaviors = new List<string>();
        if (symbol == null) return behaviors;

        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "EnrichWithAttribute" ||
                attr.AttributeClass?.Name == "EnrichWith")
            {
                if (attr.ConstructorArguments.Length > 0 &&
                    attr.ConstructorArguments[0].Kind == TypedConstantKind.Type &&
                    attr.ConstructorArguments[0].Value is ITypeSymbol typeSymbol)
                {
                    if (typeSymbol is INamedTypeSymbol named && named.IsGenericType)
                        behaviors.Add(GetUnboundTypeName(named));
                    else
                        behaviors.Add(GetFullTypeName(typeSymbol));
                }
                else if (attr.AttributeClass.IsGenericType && attr.AttributeClass.TypeArguments.Length > 0)
                {
                    behaviors.Add(GetFullTypeName(attr.AttributeClass.TypeArguments[0]));
                }
            }
        }
        return behaviors;
    }

    private static void AddDefinition(List<HandlerInfo> results, INamedTypeSymbol requestSymbol, ITypeSymbol responseSymbol, ArtifactType type)
    {
        var behaviors = ExtractPipelineBehaviors(requestSymbol);
        results.Add(Create(
            implType: null,
            reqSymbol: requestSymbol,
            resSymbol: responseSymbol,
            type: type,
            isHandler: false,
            behaviors: behaviors
        ));
    }

    private static HandlerInfo Create(string implType, ITypeSymbol reqSymbol, ITypeSymbol resSymbol, ArtifactType type, bool isHandler, List<string> behaviors)
    {
        var paramsList = new List<ParameterInfo>();
        if (reqSymbol is INamedTypeSymbol namedReq)
        {
            var ctor = namedReq.InstanceConstructors
                .Where(c => c.DeclaredAccessibility == Accessibility.Public)
                .OrderByDescending(c => c.Parameters.Length)
                .FirstOrDefault();

            if (ctor != null)
            {
                foreach (var p in ctor.Parameters)
                {
                    paramsList.Add(new ParameterInfo(GetFullTypeName(p.Type), "@" + p.Name));
                }
            }
        }

        return new HandlerInfo(
            ImplementationType: implType,
            ServiceInterfaceType: GetFullTypeName(reqSymbol),
            SimpleRequestName: reqSymbol.Name,
            ResponseType: GetFullTypeName(resSymbol),
            Type: type,
            IsHandler: isHandler,
            Parameters: paramsList,
            PipelineBehaviors: behaviors
        );
    }
}

// ==========================================
// 3. THE GENERATOR
// ==========================================
[Generator]
public sealed class DispatchGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var scanResult = context.CompilationProvider.Select((c, ct) => Scanner.ScanLocalTypes(c, ct));
        var assembly = context.CompilationProvider.Select((c, ct) => c.AssemblyName);
        var source = scanResult.Combine(assembly);

        context.RegisterSourceOutput(source, (spc, data) => Execute(spc, data.Left.Items, data.Left.HasManualPipelines, data.Right));
    }

    private static void Execute(SourceProductionContext context, List<HandlerInfo> items, bool hasManualPipelines, string assemblyName)
    {
        var match = Regex.Match(assemblyName, @"(?:^|\.)Modules?\.(\w+)");
        string moduleName = match.Success ? match.Groups[1].Value : assemblyName.Split('.').Last();
        bool isApi = assemblyName.EndsWith(".Api", StringComparison.OrdinalIgnoreCase);

        if (isApi)
        {
            var definitions = items.Where(x => !x.IsHandler && x.Type != ArtifactType.Pipeline).ToList();
            GenerateApi(context, definitions, moduleName, assemblyName);
        }
        else
        {
            var handlers = items.Where(x => x.IsHandler).ToList();
            var commands = handlers.Where(h => h.Type == ArtifactType.Command).ToList();

            GenerateInternalDispatcher(context, commands, moduleName, hasManualPipelines);

            if (handlers.Any())
            {
                GenerateModuleRegistration(context, handlers, moduleName);
            }
        }
    }

    private static void GenerateInternalDispatcher(SourceProductionContext context, List<HandlerInfo> handlers, string moduleName, bool hasManualPipelines)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");

        sb.AppendLine($@"
namespace Faster.Modulith;

internal interface I{moduleName}Dispatcher {{");

        foreach (var h in handlers)
        {
            string methodName = h.SimpleRequestName.Replace("Command", "");
            string retType = h.ResponseType == "void" ? "ValueTask" : $"ValueTask<{h.ResponseType}>";
            sb.AppendLine($"    {retType} {methodName}({h.ServiceInterfaceType} command, CancellationToken ct = default);");
        }
        sb.AppendLine("}");

        sb.AppendLine($@"
internal sealed class {moduleName}Dispatcher : I{moduleName}Dispatcher 
{{
    private readonly System.IServiceProvider _sp;
    public {moduleName}Dispatcher(System.IServiceProvider sp) => _sp = sp;
");
        foreach (var h in handlers)
        {
            string methodName = h.SimpleRequestName.Replace("Command", "");
            string retType = h.ResponseType == "void" ? "ValueTask" : $"ValueTask<{h.ResponseType}>";
            string responseType = h.ResponseType == "void" ? "int" : h.ResponseType;

            if (h.PipelineBehaviors.Any() || hasManualPipelines)
            {
                // NOTE: Double curly braces {{ }} needed for literal code blocks in interpolated strings
                sb.AppendLine($@"
    public async {retType} {methodName}({h.ServiceInterfaceType} command, CancellationToken ct) 
    {{
        var handler = _sp.GetRequiredService<global::Faster.Modulith.Contracts.ICommandHandler<{h.ServiceInterfaceType}, {responseType}>>();
        
        var behaviors = _sp.GetServices<global::Faster.Modulith.Contracts.IPipelineBehavior<{h.ServiceInterfaceType}, {responseType}>>().Reverse();
        
        global::Faster.Modulith.Contracts.RequestHandlerDelegate<{responseType}> pipeline = () => handler.Handle(command, ct);
        
        foreach (var behavior in behaviors)
        {{
            var currentBehavior = behavior;
            var next = pipeline;
            pipeline = () => currentBehavior.Handle(command, next, ct);
        }}
        
        {(h.ResponseType == "void" ? "await pipeline();" : "return await pipeline();")}
    }}");
            }
            else
            {
                if (h.ResponseType == "void")
                {
                    sb.AppendLine($"    public async {retType} {methodName}({h.ServiceInterfaceType} command, CancellationToken ct) {{ await _sp.GetRequiredService<global::Faster.Modulith.Contracts.ICommandHandler<{h.ServiceInterfaceType}, {responseType}>>().Handle(command, ct); }}");
                }
                else
                {
                    sb.AppendLine($"    public {retType} {methodName}({h.ServiceInterfaceType} command, CancellationToken ct) {{ return _sp.GetRequiredService<global::Faster.Modulith.Contracts.ICommandHandler<{h.ServiceInterfaceType}, {responseType}>>().Handle(command, ct); }}");
                }
            }
        }
        sb.AppendLine("}");
        context.AddSource($"{moduleName}Dispatcher.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void GenerateModuleRegistration(SourceProductionContext context, List<HandlerInfo> handlers, string moduleName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");

        // NOTE: Double curly braces {{ }} used for the method body
        sb.AppendLine($@"
namespace Faster.Modulith;

public static partial class {moduleName}Extensions 
{{
    public static IServiceCollection Add{moduleName}Module(this IServiceCollection services) 
    {{
        {(handlers.Any(h => h.Type == ArtifactType.Command) ? $"services.AddScoped<global::Faster.Modulith.I{moduleName}Dispatcher, global::Faster.Modulith.{moduleName}Dispatcher>();" : "")}
        {(handlers.Any(h => h.Type == ArtifactType.UseCase) ? $"services.AddScoped<global::Faster.Modulith.I{moduleName}Module, global::Faster.Modulith.{moduleName}Module>();" : "")}
");

        // 1. Register Handlers
        foreach (var h in handlers)
        {
            string interfaceName = h.Type switch
            {
                ArtifactType.Command => $"global::Faster.Modulith.Contracts.ICommandHandler<{h.ServiceInterfaceType}, {(h.ResponseType == "void" ? "int" : h.ResponseType)}>",
                ArtifactType.UseCase => $"global::Faster.Modulith.Contracts.IUseCaseHandler<{h.ServiceInterfaceType}, {h.ResponseType}>",
                _ => $"global::Faster.Modulith.Contracts.IEventHandler<{h.ServiceInterfaceType}>"
            };
            sb.AppendLine($"        services.AddScoped<{interfaceName}, {h.ImplementationType}>();");
        }

        // 2. Register [EnrichWith] Pipelines ONLY
        var behaviorsByHandler = handlers.Where(h => h.PipelineBehaviors.Any()).ToList();
        if (behaviorsByHandler.Any())
        {
            sb.AppendLine();
            sb.AppendLine("        // Register [EnrichWith] Pipelines");
            foreach (var h in behaviorsByHandler)
            {
                string responseType = h.ResponseType == "void" ? "int" : h.ResponseType;
                foreach (var behavior in h.PipelineBehaviors)
                {
                    string finalBehavior = behavior;
                    if (behavior.Contains("<,>"))
                    {
                        finalBehavior = behavior.Replace("<,>", $"<{h.ServiceInterfaceType}, {responseType}>");
                    }
                    sb.AppendLine($"        services.AddScoped<global::Faster.Modulith.Contracts.IPipelineBehavior<{h.ServiceInterfaceType}, {responseType}>, {finalBehavior}>();");
                }
            }
        }

        // NOTE: Closing curly braces }} also doubled
        sb.AppendLine($@"        
        AddInfrastructure(services);
        return services;
    }}
    static partial void AddInfrastructure(IServiceCollection services);
}}");

        context.AddSource($"{moduleName}Extensions.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void GenerateApi(SourceProductionContext context, List<HandlerInfo> definitions, string moduleName, string assemblyName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine($"// Assembly: {assemblyName}");

        if (!definitions.Any())
        {
            sb.AppendLine("// No definitions found.");
            context.AddSource($"{moduleName}Module.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            return;
        }

        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using System.Threading;");

        sb.AppendLine($@"
namespace Faster.Modulith;

public interface I{moduleName}Module
{{");

        foreach (var uc in definitions.Where(d => d.Type == ArtifactType.UseCase))
        {
            string methodName = uc.SimpleRequestName.Replace("UseCase", "");
            string retType = uc.ResponseType == "void" ? "ValueTask" : $"ValueTask<{uc.ResponseType}>";
            var methodParams = string.Join(", ", uc.Parameters.Select(p => $"{p.Type} {p.Name}"));
            if (!string.IsNullOrEmpty(methodParams)) methodParams += ", ";

            sb.AppendLine($"    {retType} {methodName}({methodParams}CancellationToken ct = default);");
        }

        foreach (var evt in definitions.Where(d => d.Type == ArtifactType.Event))
        {
            string methodName = evt.SimpleRequestName.Replace("Event", "");
            var methodParams = string.Join(", ", evt.Parameters.Select(p => $"{p.Type} {p.Name}"));
            sb.AppendLine($"    void Publish{methodName}({methodParams});");
        }

        sb.AppendLine(@"}");

        sb.AppendLine($@"
public sealed class {moduleName}Module : I{moduleName}Module
{{
    private readonly global::Faster.Modulith.Contracts.IOrchestrator _orchestrator;

    public {moduleName}Module(global::Faster.Modulith.Contracts.IOrchestrator orchestrator) 
    {{
        _orchestrator = orchestrator;
    }}
");
        foreach (var uc in definitions.Where(d => d.Type == ArtifactType.UseCase))
        {
            string methodName = uc.SimpleRequestName.Replace("UseCase", "");
            string retType = uc.ResponseType == "void" ? "ValueTask" : $"ValueTask<{uc.ResponseType}>";

            var methodParams = string.Join(", ", uc.Parameters.Select(p => $"{p.Type} {p.Name}"));
            if (!string.IsNullOrEmpty(methodParams)) methodParams += ", ";
            var ctorArgs = string.Join(", ", uc.Parameters.Select(p => p.Name));

            sb.AppendLine($@"
    public {retType} {methodName}({methodParams}CancellationToken ct = default)
    {{
        var request = new {uc.ServiceInterfaceType}({ctorArgs});
        return _orchestrator.Dispatch<{uc.ServiceInterfaceType}, {uc.ResponseType}>(request, ct);
    }}");
        }

        foreach (var evt in definitions.Where(d => d.Type == ArtifactType.Event))
        {
            string methodName = evt.SimpleRequestName.Replace("Event", "");
            var methodParams = string.Join(", ", evt.Parameters.Select(p => $"{p.Type} {p.Name}"));
            var ctorArgs = string.Join(", ", evt.Parameters.Select(p => p.Name));

            sb.AppendLine($@"
    public void Publish{methodName}({methodParams})
    {{
        var evt = new {evt.ServiceInterfaceType}({ctorArgs});
        _orchestrator.Publish(evt);
    }}");
        }

        sb.AppendLine("}");
        context.AddSource($"{moduleName}Module.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }
}
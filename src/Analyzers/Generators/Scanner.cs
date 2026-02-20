using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Faster.Modulith.Analyzers.Generators;

/// <summary>
/// Scans a Roslyn compilation or assembly for Faster.Modulith artifacts
/// such as use cases, commands, events, and pipeline behaviors.
/// </summary>
public static class Scanner
{
    /// <summary>
    /// Strict fully-qualified symbol display format used for deterministic code generation.
    /// </summary>
    private static readonly SymbolDisplayFormat StrictFormat =
        SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included)
            .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers)
            .WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType);

    /// <summary>
    /// Returns the fully-qualified name of a type symbol.
    /// </summary>
    private static string GetFullTypeName(ITypeSymbol symbol)
    {
        if (symbol == null) return "void";
        return symbol.ToDisplayString(StrictFormat);
    }

    /// <summary>
    /// Returns the unbound generic type name for open generic registrations.
    /// </summary>
    private static string GetUnboundTypeName(INamedTypeSymbol symbol)
    {
        if (symbol.IsGenericType)
            return symbol.ConstructUnboundGenericType().ToDisplayString(StrictFormat);

        return symbol.ToDisplayString(StrictFormat);
    }

    /// <summary>
    /// Scans a compilation for handlers, definitions, and pipeline registrations.
    /// </summary>
    /// <returns>
    /// Tuple containing discovered artifacts and whether manual pipelines were detected.
    /// </returns>
    public static (List<HandlerInfo> Items, bool HasManualPipelines)
        ScanTypes(Compilation compilation, CancellationToken ct)
    {
        var results = new List<HandlerInfo>();
        bool hasManualPipelines = false;

        // Detect manual IPipelineBehavior registrations in AddInfrastructure methods
        foreach (var tree in compilation.SyntaxTrees)
        {
            var root = tree.GetRoot(ct);

            var methods = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Identifier.Text == "AddInfrastructure");

            foreach (var method in methods)
            {
                var bodyNodes = method.Body?.DescendantNodes()
                                ?? method.ExpressionBody?.DescendantNodes();

                if (bodyNodes != null)
                {
                    bool distinctUsage = bodyNodes.Any(node =>
                        (node is IdentifierNameSyntax id &&
                         id.Identifier.Text == "IPipelineBehavior") ||
                        (node is GenericNameSyntax gen &&
                         gen.Identifier.Text == "IPipelineBehavior"));

                    if (distinctUsage)
                        hasManualPipelines = true;
                }
            }
        }

        var allSymbols = GetAllNamedTypes(compilation.GlobalNamespace);
        ScanSymbols(allSymbols, results);

        return (results, hasManualPipelines);
    }

    /// <summary>
    /// Scans an assembly symbol for artifacts.
    /// </summary>
    public static List<HandlerInfo> ScanAssembly(IAssemblySymbol assembly)
    {
        var results = new List<HandlerInfo>();
        var allSymbols = GetAllNamedTypes(assembly.GlobalNamespace);

        ScanSymbols(allSymbols, results);

        return results;
    }

    /// <summary>
    /// Performs symbol inspection and artifact classification.
    /// </summary>
    private static void ScanSymbols(
        IEnumerable<INamedTypeSymbol> allSymbols,
        List<HandlerInfo> results)
    {
        foreach (var symbol in allSymbols)
        {
            if (symbol.IsAbstract || symbol.IsStatic)
                continue;

            // Pipeline behaviors
            foreach (var iface in symbol.AllInterfaces)
            {
                if (iface.Name == "IPipelineBehavior")
                {
                    bool isGeneric = symbol.IsGenericType;

                    string implType = isGeneric
                        ? GetUnboundTypeName(symbol)
                        : GetFullTypeName(symbol);

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
                        PipelineBehaviors: new(),
                        Attributes: new(),
                        ExposeData: null
                    ));
                }
            }

            // UseCase definitions
            foreach (var iface in symbol.AllInterfaces)
            {
                if (iface.IsGenericType && iface.Name == "IUseCase")
                {
                    AddDefinition(results, symbol, iface.TypeArguments[0], ArtifactType.UseCase);
                }
            }

            // Command / UseCase / Event handlers
            foreach (var iface in symbol.AllInterfaces)
            {
                if (!iface.IsGenericType || iface.Name == "IUseCase")
                    continue;

                ArtifactType? handlerType = iface.Name switch
                {
                    "ICommandHandler" => ArtifactType.Command,
                    "IUseCaseHandler" => ArtifactType.UseCase,
                    "IEventHandler" => ArtifactType.Event,
                    _ => null
                };

                if (handlerType == null || iface.TypeArguments.Length == 0)
                    continue;

                var requestSymbol = iface.TypeArguments[0];
                var responseSymbol = iface.TypeArguments.Length > 1
                    ? iface.TypeArguments[1]
                    : null;

                var exposeInfo = ExtractExposeInfo(symbol);

                var handlerBehaviors = ExtractPipelineBehaviors(symbol);
                var requestBehaviors = ExtractPipelineBehaviors(requestSymbol);

                var allBehaviors = handlerBehaviors
                    .Concat(requestBehaviors)
                    .Distinct()
                    .ToList();

                var handler = Create(
                    implType: GetFullTypeName(symbol),
                    reqSymbol: requestSymbol,
                    resSymbol: responseSymbol,
                    type: handlerType.Value,
                    isHandler: true,
                    behaviors: allBehaviors,
                    expose: exposeInfo,
                    source: symbol
                );

                results.Add(handler);

                if (handlerType == ArtifactType.UseCase)
                {
                    AddDefinition(results,
                        (INamedTypeSymbol)requestSymbol,
                        responseSymbol,
                        ArtifactType.UseCase);
                }
            }

            // Event definitions
            if (symbol.AllInterfaces.Any(i => i.Name == "IEvent"))
            {
                AddDefinition(results, symbol, null, ArtifactType.Event);
            }
        }
    }

    /// <summary>
    /// Recursively enumerates all named types in a namespace hierarchy.
    /// </summary>
    private static IEnumerable<INamedTypeSymbol> GetAllNamedTypes(INamespaceSymbol ns)
    {
        foreach (var type in ns.GetTypeMembers())
            yield return type;

        foreach (var subNs in ns.GetNamespaceMembers())
        {
            foreach (var type in GetAllNamedTypes(subNs))
                yield return type;
        }
    }

    /// <summary>
    /// Extracts pipeline behaviors declared via EnrichWith attributes.
    /// </summary>
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
                    if (typeSymbol is INamedTypeSymbol named &&
                        named.IsGenericType)
                        behaviors.Add(GetUnboundTypeName(named));
                    else
                        behaviors.Add(GetFullTypeName(typeSymbol));
                }
            }
        }

        return behaviors;
    }

    /// <summary>
    /// Extracts Expose attribute metadata from a symbol.
    /// </summary>
    private static ExposeInfo ExtractExposeInfo(ISymbol symbol)
    {
        var attr = symbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name is "ExposeAttribute" or "Expose");

        if (attr == null)
            return null;

        return new ExposeInfo(
            $"[global::Faster.Modulith.Contracts.Expose(\"{attr.ConstructorArguments[0].Value}\", \"{attr.ConstructorArguments[1].Value}\")]",
            attr.ConstructorArguments[0].Value?.ToString(),
            attr.ConstructorArguments[1].Value?.ToString());
    }

    /// <summary>
    /// Adds a request or event definition if not already present.
    /// </summary>
    private static void AddDefinition(
        List<HandlerInfo> results,
        INamedTypeSymbol requestSymbol,
        ITypeSymbol responseSymbol,
        ArtifactType type)
    {
        if (results.Any(r =>
                r.ServiceInterfaceType == GetFullTypeName(requestSymbol) &&
                r.Type == type &&
                !r.IsHandler))
            return;

        var behaviors = ExtractPipelineBehaviors(requestSymbol);

        results.Add(Create(
            implType: null,
            reqSymbol: requestSymbol,
            resSymbol: responseSymbol,
            type: type,
            isHandler: false,
            behaviors: behaviors,
            expose: null,
            source: requestSymbol
        ));
    }

    /// <summary>
    /// Creates a HandlerInfo instance from symbol metadata.
    /// </summary>
    private static HandlerInfo Create(
        string implType,
        ITypeSymbol reqSymbol,
        ITypeSymbol resSymbol,
        ArtifactType type,
        bool isHandler,
        List<string> behaviors,
        ExposeInfo expose,
        ISymbol source)
    {
        var paramsList = new List<ParameterInfo>();

        if (source is INamedTypeSymbol namedSource)
        {
            var ctor = namedSource.InstanceConstructors
                .Where(c => c.DeclaredAccessibility == Accessibility.Public)
                .OrderByDescending(c => c.Parameters.Length)
                .FirstOrDefault();

            if (ctor != null)
            {
                foreach (var p in ctor.Parameters)
                {
                    paramsList.Add(new ParameterInfo(
                        GetFullTypeName(p.Type),
                        "@" + p.Name));
                }
            }
        }

        var attributes = new List<string>();
        if (expose != null)
            attributes.Add(expose.RawAttributeLine);

        return new HandlerInfo(
            ImplementationType: implType,
            ServiceInterfaceType: GetFullTypeName(reqSymbol),
            SimpleRequestName: reqSymbol.Name,
            ResponseType: GetFullTypeName(resSymbol),
            Type: type,
            IsHandler: isHandler,
            Parameters: paramsList,
            PipelineBehaviors: behaviors,
            Attributes: attributes,
            ExposeData: expose
        );
    }
}




using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Faster.Modulith.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ModularMonolithApiAnalyzer : DiagnosticAnalyzer
{
    // =================================================================================================
    // 1. RULE DEFINITIONS (API SPECIFIC)
    // =================================================================================================

    // API001: Visibility
    private static readonly DiagnosticDescriptor RulePublicContracts = new(
        "API001",
        "API Artifacts must be Public",
        "The type '{0}' is defined in an API project but is not 'public'. Contracts must be visible to the Orchestrator and other Modules.",
        "Architecture",
        DiagnosticSeverity.Error,
        true);

    // API002: Immutability
    private static readonly DiagnosticDescriptor RuleUseRecords = new(
        "API002",
        "Contracts must be Records",
        "The contract '{0}' is a Class. API messages (UseCases, Events, DTOs) must be 'record' to ensure immutability.",
        "Best Practice",
        DiagnosticSeverity.Warning,
        true);

    // API003: Naming
    private static readonly DiagnosticDescriptor RuleNaming = new(
        "API003",
        "Incorrect Naming Suffix",
        "The type '{0}' implements {1} but does not end with '{2}'.",
        "Naming",
        DiagnosticSeverity.Warning,
        true);

    // API004: Purity (No Handlers in API)
    private static readonly DiagnosticDescriptor RuleNoHandlers = new(
        "API004",
        "Handlers Forbidden in API",
        "The type '{0}' appears to be a Handler. API projects should only contain Definitions (UseCases/Events), not Implementations.",
        "Architecture",
        DiagnosticSeverity.Error,
        true);

    // API005: Dependencies
    private static readonly DiagnosticDescriptor RuleNoModuleReference = new(
        "API005",
        "Illegal Module Reference",
        "The API project '{0}' references a Module project '{1}'. API projects must be standalone contracts.",
        "Architecture",
        DiagnosticSeverity.Error,
        true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        RulePublicContracts, RuleUseRecords, RuleNaming, RuleNoHandlers, RuleNoModuleReference);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // 1. Dependency Analysis
        context.RegisterCompilationAction(AnalyzeApiDependencies);

        // 2. Type Analysis
        context.RegisterSymbolAction(AnalyzeTypeDefinition, SymbolKind.NamedType);
    }

    // =================================================================================================
    // LOGIC
    // =================================================================================================

    private void AnalyzeApiDependencies(CompilationAnalysisContext context)
    {
        var compilation = context.Compilation;
        string assemblyName = compilation.AssemblyName ?? "";

        // Only run on .Api projects
        if (!assemblyName.EndsWith(".Api") && !assemblyName.Contains(".Api.")) return;

        foreach (var reference in compilation.ReferencedAssemblyNames)
        {
            // API projects cannot reference .Module projects (Circular dependency risk / leakage)
            if (reference.Name.Contains(".Module.") && !reference.Name.Contains(".Contracts"))
            {
                context.ReportDiagnostic(Diagnostic.Create(RuleNoModuleReference, Location.None, assemblyName, reference.Name));
            }
        }
    }

    private void AnalyzeTypeDefinition(SymbolAnalysisContext context)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        if (symbol.IsAbstract || symbol.IsImplicitlyDeclared) return;

        string assemblyName = symbol.ContainingAssembly.Name;

        // FILTER: strictly for API projects
        if (!assemblyName.EndsWith(".Api") && !assemblyName.Contains(".Api.")) return;

        // 1. Check for Forbidden Handlers (API004)
        if (symbol.Name.EndsWith("Handler") || symbol.AllInterfaces.Any(i => i.Name.Contains("Handler")))
        {
            context.ReportDiagnostic(Diagnostic.Create(RuleNoHandlers, symbol.Locations[0], symbol.Name));
            return;
        }

        // 2. Check Contracts (IUseCase, IEvent, DTOs)
        bool isContract = symbol.AllInterfaces.Any(i => i.Name == "IUseCase" || i.Name == "IEvent");

        // Also assume any public type in an API project is likely a DTO if it's not a static helper
        if (!isContract && !symbol.IsStatic && symbol.TypeKind != TypeKind.Interface)
        {
            isContract = true; // Treat as DTO
        }

        if (isContract)
        {
            // API001: Must be Public
            if (symbol.DeclaredAccessibility != Accessibility.Public)
            {
                context.ReportDiagnostic(Diagnostic.Create(RulePublicContracts, symbol.Locations[0], symbol.Name));
            }

            // API002: Must be Record
            if (!symbol.IsRecord && symbol.TypeKind == TypeKind.Class)
            {
                context.ReportDiagnostic(Diagnostic.Create(RuleUseRecords, symbol.Locations[0], symbol.Name));
            }

            // API003: Naming Conventions
            foreach (var iface in symbol.AllInterfaces)
            {
                if (iface.Name == "IUseCase" && !symbol.Name.EndsWith("UseCase"))
                {
                    context.ReportDiagnostic(Diagnostic.Create(RuleNaming, symbol.Locations[0], symbol.Name, "IUseCase", "UseCase"));
                }
                if (iface.Name == "IEvent" && !symbol.Name.EndsWith("Event"))
                {
                    context.ReportDiagnostic(Diagnostic.Create(RuleNaming, symbol.Locations[0], symbol.Name, "IEvent", "Event"));
                }
            }
        }
    }
}
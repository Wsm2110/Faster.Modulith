using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Faster.Modulith.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ArchitectureLocationAnalyzer : DiagnosticAnalyzer
{
    // =================================================================================================
    // RULE DEFINITIONS
    // =================================================================================================

    // MOD030: UseCase Location
    private static readonly DiagnosticDescriptor RuleUseCaseLocation = new(
        "MOD030",
        "UseCase defined in non-API project",
        "The UseCase '{0}' is defined in project '{1}'. UseCases must be placed in a project named like 'Api.*', '*.Api.*', or ending in '.Api'.",
        "Architecture",
        DiagnosticSeverity.Error,
        true);

    // MOD031: Handler Location
    private static readonly DiagnosticDescriptor RuleHandlerLocation = new(
        "MOD031",
        "Handler defined in non-Module project",
        "The Handler '{0}' is defined in project '{1}'. Handlers must be placed in a project named like 'Module.*', '*.Module.*', or ending in '.Module'.",
        "Architecture",
        DiagnosticSeverity.Error,
        true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(RuleUseCaseLocation, RuleHandlerLocation);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeTypeLocation, SymbolKind.NamedType);
    }

    private void AnalyzeTypeLocation(SymbolAnalysisContext context)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        if (symbol.IsAbstract || symbol.IsImplicitlyDeclared) return;

        var assemblyName = symbol.ContainingAssembly.Name;

        // 1. CHECK USE CASES (Flexible 'Api' placement)
        if (symbol.AllInterfaces.Any(i => i.Name == "IUseCase"))
        {
            bool isValidApiProject =
                // Starts With: "Api.Facilities"
                assemblyName.StartsWith("Api.", StringComparison.OrdinalIgnoreCase) ||

                // Contains: "MySystem.Api.HR"
                assemblyName.IndexOf(".Api.", StringComparison.OrdinalIgnoreCase) >= 0 ||

                // Ends With: "MySystem.HR.Api"
                assemblyName.EndsWith(".Api", StringComparison.OrdinalIgnoreCase);

            if (!isValidApiProject)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    RuleUseCaseLocation,
                    symbol.Locations[0],
                    symbol.Name,
                    assemblyName));
            }
        }

        // 2. CHECK HANDLERS (Flexible 'Module' placement)
        bool isHandler = symbol.AllInterfaces.Any(i =>
            i.Name.StartsWith("ICommandHandler") ||
            i.Name.StartsWith("IUseCaseHandler") ||
            i.Name.StartsWith("IEventHandler"));

        if (isHandler)
        {
            bool isValidModuleProject =
                // Starts With: "Module.Facilities"
                assemblyName.StartsWith("Module.", StringComparison.OrdinalIgnoreCase) ||
                assemblyName.StartsWith("Modules.", StringComparison.OrdinalIgnoreCase) ||

                // Contains: "MySystem.Module.HR"
                assemblyName.IndexOf(".Module.", StringComparison.OrdinalIgnoreCase) >= 0 ||
                assemblyName.IndexOf(".Modules.", StringComparison.OrdinalIgnoreCase) >= 0 ||

                // Ends With: "MySystem.HR.Module"
                assemblyName.EndsWith(".Module", StringComparison.OrdinalIgnoreCase) ||
                assemblyName.EndsWith(".Modules", StringComparison.OrdinalIgnoreCase);

            if (!isValidModuleProject)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    RuleHandlerLocation,
                    symbol.Locations[0],
                    symbol.Name,
                    assemblyName));
            }
        }
    }
}
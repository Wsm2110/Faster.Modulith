using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Faster.Modulith.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ProjectConventionAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MOD000";

    private static readonly DiagnosticDescriptor RuleInvalidProjectName = new(
        DiagnosticId, "Invalid Modulith Project Name", "The project '{0}' references 'Faster.Modulith' but violates the naming convention. Rename the project to include '.Api', '.Module.', '.Host', or '.Tests'.", "Configuration", DiagnosticSeverity.Error, true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuleInvalidProjectName);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeProjectNaming);
    }

    private void AnalyzeProjectNaming(CompilationAnalysisContext context)
    {
        var compilation = context.Compilation;
        var assemblyName = compilation.AssemblyName;

        if (string.IsNullOrEmpty(assemblyName)) return;

        bool referencesModulith = compilation.ReferencedAssemblyNames.Any(r => r.Name.Equals("Faster.Modulith", StringComparison.OrdinalIgnoreCase));
        if (assemblyName == "Faster.Modulith") return;
        if (!referencesModulith) return;

        bool isValidApi = assemblyName.IndexOf("Api", StringComparison.OrdinalIgnoreCase) >= 0;
        bool isValidModule = assemblyName.IndexOf("Module", StringComparison.OrdinalIgnoreCase) >= 0;
        bool isValidTests = assemblyName.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase) || assemblyName.Contains(".IntegrationTests");
        bool isValidHost = assemblyName.EndsWith(".Host", StringComparison.OrdinalIgnoreCase) || assemblyName.EndsWith(".Web", StringComparison.OrdinalIgnoreCase);

        if (!isValidApi && !isValidModule && !isValidTests && !isValidHost)
        {
            context.ReportDiagnostic(Diagnostic.Create(RuleInvalidProjectName, Location.None, assemblyName));
        }
    }
}
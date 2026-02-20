using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Faster.Modulith.Analyzers;

/// <summary>
/// A diagnostic analyzer responsible for enforcing strict architectural and structural rules 
/// across the modular monolith. It validates module isolation, encapsulation, and dependency injection patterns.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ModulithAnalyzer : DiagnosticAnalyzer
{
    // =================================================================================================
    // RULE DEFINITIONS
    // =================================================================================================

    private static readonly DiagnosticDescriptor RuleIllegalReference = new("MOD005", "Illegal Cross-Module Reference", "Strict Isolation: You are using '{0}' which belongs to Module '{1}'. You may ONLY use types from your own Module or '.Api' projects.", "Architecture", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor RuleServiceLocator = new("MOD025", "Service Locator Pattern Detected", "The usage of '{0}' is forbidden. Do not inject 'IServiceProvider'; use explicit Constructor Injection.", "Architecture", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor RuleDirectInstantiation = new("MOD026", "Illegal Direct Instantiation", "You cannot manually instantiate '{0}' from Module '{1}'.", "Architecture", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor RuleProfileInstantiation = new("MOD027", "Manual Profile Instantiation", "Do not manually instantiate AutoMapper Profiles ('{0}'). Let the dependency injection container scan for them.", "Best Practice", DiagnosticSeverity.Warning, true);

    private static readonly DiagnosticDescriptor RuleInternalMessages = new("MOD001", "Internal Messages are declared Public", "The {0} '{1}' is declared public. It must be 'internal' to strictly encapsulate domain logic (unless it is in the .Api project).", "Architecture", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor RulePublicUseCases = new("MOD002", "UseCases are declared Internal", "The UseCase '{0}' must be 'public' because it resides in the .Api contract layer.", "Architecture", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor RuleInternalHandlers = new("MOD003", "Handlers are declared Public", "The handler class '{0}' must be 'internal'.", "Architecture", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor RuleNoControllersInModule = new("MOD033", "Controllers Forbidden in Module", "Controllers must reside in the Host/Web project.", "Architecture", DiagnosticSeverity.Error, true);

    private static readonly DiagnosticDescriptor RuleRecursiveCall = new("MOD004", "Recursive Call Detected", "Infinite Loop Risk.", "Logic", DiagnosticSeverity.Warning, true);
    private static readonly DiagnosticDescriptor RuleMaxDepth = new("MOD012", "Call Depth Violation", "Too many chained Orchestrator calls (>3).", "Logic", DiagnosticSeverity.Warning, true);
    private static readonly DiagnosticDescriptor RuleTooManyCommands = new("MOD014", "Too Many Commands", "High coupling detected (>5 commands).", "Complexity", DiagnosticSeverity.Warning, true);
    private static readonly DiagnosticDescriptor RuleCommandChaining = new("MOD019", "Command Chaining Detected", "Handlers should not chain commands.", "Architecture", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor RuleEventChaining = new("MOD020", "Event Chaining Detected", "Handlers should not trigger events directly.", "Architecture", DiagnosticSeverity.Warning, true);

    private static readonly DiagnosticDescriptor RuleUseRecords = new("MOD006", "Messages should be Records", "Use 'record' for immutability in Api contracts.", "Best Practice", DiagnosticSeverity.Warning, true);
    private static readonly DiagnosticDescriptor RuleNaming = new("MOD007", "Incorrect Naming Suffix", "Type '{0}' should end with '{1}'.", "Naming", DiagnosticSeverity.Warning, true);
    private static readonly DiagnosticDescriptor RuleUseCaseLocation = new("MOD008", "Invalid Namespace", "Move to .Application.UseCases.", "Organization", DiagnosticSeverity.Warning, true);
    private static readonly DiagnosticDescriptor RuleCommandLocation = new("MOD009", "Invalid Namespace", "Move to .Application.CommandHandlers.", "Organization", DiagnosticSeverity.Warning, true);
    private static readonly DiagnosticDescriptor RuleInterfaceLocation = new("MOD010", "Invalid Namespace", "Move to .Contracts or .Api.", "Organization", DiagnosticSeverity.Warning, true);
    private static readonly DiagnosticDescriptor RuleRepositoryLocation = new("MOD011", "Invalid Namespace", "Move to .Infrastructure.", "Organization", DiagnosticSeverity.Warning, true);
    private static readonly DiagnosticDescriptor RuleControllerPurity = new("MOD016", "Controller Violates API Purity", "Controllers should only inject IOrchestrator.", "Architecture", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor RuleIQueryable = new("MOD017", "IQueryable Leakage Detected", "Do not return IQueryable.", "Architecture", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor RulePublicEntity = new("MOD018", "Domain Entity is Public", "Domain entities should be internal.", "Encapsulation", DiagnosticSeverity.Warning, true);
    private static readonly DiagnosticDescriptor RuleStrictStructure = new("MOD021", "Invalid Folder Structure", "Invalid namespace structure.", "Organization", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor RuleNamespaceConvention = new("MOD022", "Invalid Module Namespace", "Namespace must match Module name.", "Naming", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor RuleNoPragmas = new("MOD023", "Pragma Suppression Forbidden", "Use [ArchitectureBypass] instead.", "Architecture", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor RuleMissingHandlerImpl = new("MOD024", "Missing Handler Implementation", "The class '{0}' looks like a {1} but does not implement '{2}'.", "Scaffolding", DiagnosticSeverity.Error, true);

    /// <summary>
    /// Gets the set of supported diagnostic rules enforced by this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        RuleIllegalReference, RuleServiceLocator, RuleDirectInstantiation, RuleProfileInstantiation,
        RuleInternalMessages, RulePublicUseCases, RuleInternalHandlers, RuleNoControllersInModule,
        RuleRecursiveCall, RuleMaxDepth, RuleTooManyCommands, RuleCommandChaining, RuleEventChaining,
        RuleUseRecords, RuleNaming, RuleUseCaseLocation, RuleCommandLocation, RuleInterfaceLocation,
        RuleRepositoryLocation, RuleControllerPurity, RuleIQueryable, RulePublicEntity,
        RuleStrictStructure, RuleNamespaceConvention, RuleNoPragmas, RuleMissingHandlerImpl
    );

    /// <summary>
    /// Initializes the analyzer and registers the necessary actions for syntax, symbol, and operation analysis.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        context.RegisterSymbolAction(AnalyzeFieldAndProperty, SymbolKind.Field, SymbolKind.Property);

        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
        context.RegisterOperationAction(AnalyzeVariableDeclaration, OperationKind.VariableDeclaration);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzeMethodBody, OperationKind.MethodBody);

        context.RegisterSyntaxNodeAction(AnalyzePragma, SyntaxKind.PragmaWarningDirectiveTrivia);
    }

    // =================================================================================================
    // 1. SYMBOL ANALYSIS (Structure, Encapsulation)
    // =================================================================================================

    /// <summary>
    /// Analyzes named types (classes, structs, interfaces, records) to enforce structural, 
    /// naming, and encapsulation rules within the modules.
    /// </summary>
    /// <param name="context">The symbol analysis context.</param>
    private void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        if (symbol.IsAbstract || symbol.IsGenericType || symbol.IsImplicitlyDeclared || symbol.Locations.Length == 0) return;

        var primaryLocation = symbol.Locations[0];
        string path = primaryLocation.SourceTree?.FilePath ?? "";
        if (path.IndexOf("Modules", StringComparison.OrdinalIgnoreCase) < 0) return;

        string assemblyName = symbol.ContainingAssembly.Name;
        string fullNamespace = symbol.ContainingNamespace?.ToDisplayString() ?? "";

        bool isInApi = assemblyName.EndsWith(".Api", StringComparison.OrdinalIgnoreCase)
                    || assemblyName.EndsWith(".Contracts", StringComparison.OrdinalIgnoreCase)
                    || fullNamespace.Contains(".Api")
                    || fullNamespace.Contains(".Contracts");

        if (symbol.Name.EndsWith("Controller") || BaseTypeMatches(symbol, "ControllerBase"))
        {
            context.ReportDiagnostic(Diagnostic.Create(RuleNoControllersInModule, primaryLocation, symbol.Name));
        }

        if (assemblyName.Contains(".Module."))
        {
            var parts = assemblyName.Split('.');
            int moduleIndex = Array.IndexOf(parts, "Module");
            if (moduleIndex != -1 && moduleIndex + 1 < parts.Length)
            {
                string moduleName = parts[moduleIndex + 1];
                string expectedFragment = $".Module.{moduleName}";
                if (!fullNamespace.Contains(expectedFragment) && !IsBypassed(symbol, "MOD022"))
                {
                    context.ReportDiagnostic(Diagnostic.Create(RuleNamespaceConvention, primaryLocation, fullNamespace, string.Join(".", parts.Take(moduleIndex)), moduleName));
                }

                if (fullNamespace.StartsWith(assemblyName) && !IsBypassed(symbol, "MOD021"))
                {
                    string relativeNs = fullNamespace.Substring(assemblyName.Length);
                    if (!symbol.Name.EndsWith("Dispatcher") && !symbol.Name.StartsWith("Generated") && !IsNamespaceAllowed(relativeNs))
                        context.ReportDiagnostic(Diagnostic.Create(RuleStrictStructure, primaryLocation, symbol.Name, fullNamespace));
                }
            }
        }

        if (fullNamespace.Contains(".Domain") && !symbol.IsRecord && symbol.DeclaredAccessibility == Accessibility.Public && symbol.TypeKind == TypeKind.Class && !IsBypassed(symbol, "MOD018"))
        {
            context.ReportDiagnostic(Diagnostic.Create(RulePublicEntity, primaryLocation, symbol.Name));
        }

        foreach (var iface in symbol.AllInterfaces)
        {
            // === MESSAGES (Commands & Events) ===
            if (iface.Name == "ICommand" || iface.Name == "IEvent")
            {
                // MOD006: Use Records (Only enforces if in API layer)
                if (isInApi && !symbol.IsRecord && !IsBypassed(symbol, "MOD006"))
                    context.ReportDiagnostic(Diagnostic.Create(RuleUseRecords, primaryLocation, symbol.Name));

                // MOD001: Internal Messages (Only enforce internal if NOT in Api)
                if (!isInApi && symbol.DeclaredAccessibility == Accessibility.Public && !IsBypassed(symbol, "MOD001"))
                    context.ReportDiagnostic(Diagnostic.Create(RuleInternalMessages, primaryLocation, iface.Name, symbol.Name));

                // MOD007: Naming
                if (!symbol.Name.EndsWith(iface.Name == "ICommand" ? "Command" : "Event") && !IsBypassed(symbol, "MOD007"))
                    context.ReportDiagnostic(Diagnostic.Create(RuleNaming, primaryLocation, symbol.Name, iface.Name, iface.Name == "ICommand" ? "Command" : "Event"));
            }
            // === USE CASES ===
            else if (iface.Name == "IUseCase")
            {
                if (symbol.DeclaredAccessibility != Accessibility.Public && !IsBypassed(symbol, "MOD002"))
                    context.ReportDiagnostic(Diagnostic.Create(RulePublicUseCases, primaryLocation, symbol.Name));

                if (!symbol.Name.EndsWith("UseCase") && !IsBypassed(symbol, "MOD007"))
                    context.ReportDiagnostic(Diagnostic.Create(RuleNaming, primaryLocation, symbol.Name, iface.Name, "UseCase"));
            }
            // === HANDLERS ===
            else if (iface.Name.StartsWith("ICommandHandler") || iface.Name.StartsWith("IEventHandler") || iface.Name.StartsWith("IUseCaseHandler"))
            {
                if (symbol.DeclaredAccessibility == Accessibility.Public && !IsBypassed(symbol, "MOD003"))
                    context.ReportDiagnostic(Diagnostic.Create(RuleInternalHandlers, primaryLocation, symbol.Name));

                if (iface.Name.Contains("IUseCaseHandler") && !fullNamespace.Contains(".Application.UseCases") && !IsBypassed(symbol, "MOD008"))
                    context.ReportDiagnostic(Diagnostic.Create(RuleUseCaseLocation, primaryLocation, symbol.Name));
                if (iface.Name.Contains("ICommandHandler") && !fullNamespace.Contains(".Application.CommandHandlers") && !IsBypassed(symbol, "MOD009"))
                    context.ReportDiagnostic(Diagnostic.Create(RuleCommandLocation, primaryLocation, symbol.Name));
            }
        }

        // ==========================================================
        // MOD024 Path-Aware Handler Detection
        // ==========================================================
        if (symbol.Name.EndsWith("Handler") && !symbol.IsStatic)
        {
            bool ContainsName(string s) => fullNamespace.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0;

            if (symbol.Name.EndsWith("EventHandler") || ContainsName("EventHandlers") || ContainsName("EventHandler"))
            {
                if (!symbol.AllInterfaces.Any(i => i.Name.StartsWith("IEventHandler")))
                    context.ReportDiagnostic(Diagnostic.Create(RuleMissingHandlerImpl, primaryLocation, symbol.Name, "Event Handler", "IEventHandler"));
            }
            else if (symbol.Name.EndsWith("CommandHandler") || ContainsName("CommandHandlers") || ContainsName("CommandHandler"))
            {
                if (!symbol.AllInterfaces.Any(i => i.Name.StartsWith("ICommandHandler")))
                    context.ReportDiagnostic(Diagnostic.Create(RuleMissingHandlerImpl, primaryLocation, symbol.Name, "Command Handler", "ICommandHandler"));
            }
            else if (symbol.Name.EndsWith("UseCaseHandler") || ContainsName("UseCases") || ContainsName("UseCase"))
            {
                if (!symbol.AllInterfaces.Any(i => i.Name.StartsWith("IUseCaseHandler")))
                    context.ReportDiagnostic(Diagnostic.Create(RuleMissingHandlerImpl, primaryLocation, symbol.Name, "UseCase Handler", "IUseCaseHandler"));
            }
        }
    }

    // =================================================================================================
    // 2. MEMBER ANALYSIS (Fields, Methods, Props)
    // =================================================================================================

    /// <summary>
    /// Analyzes fields and properties to prevent the usage of anti-patterns such as 
    /// the Service Locator pattern or illegal type references.
    /// </summary>
    /// <param name="context">The symbol analysis context.</param>
    private void AnalyzeFieldAndProperty(SymbolAnalysisContext context)
    {
        if (context.Symbol.Locations.Length == 0) return;
        var primaryLocation = context.Symbol.Locations[0];

        var type = context.Symbol is IFieldSymbol f ? f.Type : ((IPropertySymbol)context.Symbol).Type;
        if (IsServiceLocator(type) && !IsBypassed(context.Symbol, "MOD025"))
            context.ReportDiagnostic(Diagnostic.Create(RuleServiceLocator, primaryLocation, type.Name));
        ValidateType(type, context, primaryLocation);
    }

    /// <summary>
    /// Analyzes method signatures to ensure compliance with architectural constraints, 
    /// such as preventing IQueryable leakage and improper parameter injection.
    /// </summary>
    /// <param name="context">The symbol analysis context.</param>
    private void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (method.Locations.Length == 0) return;
        var primaryLocation = method.Locations[0];

        if (method.ReturnType.Name.Contains("IQueryable") && !IsBypassed(method, "MOD017"))
            context.ReportDiagnostic(Diagnostic.Create(RuleIQueryable, primaryLocation, method.Name));

        ValidateType(method.ReturnType, context, primaryLocation);

        foreach (var param in method.Parameters)
        {
            if (param.Locations.Length == 0) continue;
            var paramLocation = param.Locations[0];

            if (IsServiceLocator(param.Type) && !IsBypassed(method, "MOD025"))
                context.ReportDiagnostic(Diagnostic.Create(RuleServiceLocator, paramLocation, param.Type.Name));
            ValidateType(param.Type, context, paramLocation);
        }
    }

    // =================================================================================================
    // 3. EXECUTABLE LOGIC (New, Var, Invocation)
    // =================================================================================================

    /// <summary>
    /// Analyzes object creation operations to detect manual instantiations of types 
    /// that should be resolved via dependency injection or cross-module boundaries.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    private void AnalyzeObjectCreation(OperationAnalysisContext context)
    {
        var op = (IObjectCreationOperation)context.Operation;
        ValidateType(op.Type, context, op.Syntax.GetLocation(), isInstantiation: true);

        if (op.Type != null && BaseTypeMatches(op.Type, "Profile") && op.Type.ContainingNamespace.ToString().Contains("AutoMapper"))
        {
            if (!IsBypassed(context.ContainingSymbol, "MOD027"))
                context.ReportDiagnostic(Diagnostic.Create(RuleProfileInstantiation, op.Syntax.GetLocation(), op.Type.Name));
        }
    }

    /// <summary>
    /// Analyzes variable declarations to ensure the declared types comply with 
    /// modular isolation constraints.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    private void AnalyzeVariableDeclaration(OperationAnalysisContext context)
    {
        var op = (IVariableDeclarationOperation)context.Operation;
        foreach (var decl in op.Declarators)
        {
            ValidateType(decl.Symbol.Type, context, op.Syntax.GetLocation());
        }
    }

    /// <summary>
    /// Analyzes method invocations to identify and prevent logic risks such as 
    /// recursive calls, command chaining, and direct event chaining.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    private void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var targetMethod = invocation.TargetMethod;
        if (targetMethod.Name != "Send" && targetMethod.Name != "Publish" && targetMethod.Name != "Execute") return;

        if (context.ContainingSymbol.ContainingType is not { } containingType) return;

        bool isCmdHandler = containingType.AllInterfaces.Any(i => i.Name.StartsWith("ICommandHandler"));
        bool isEvtHandler = containingType.AllInterfaces.Any(i => i.Name.StartsWith("IEventHandler"));

        if (invocation.Arguments.Length == 0) return;
        var argType = invocation.Arguments[0].Value.Type;
        if (argType == null) return;

        var handlerInterface = containingType.AllInterfaces.FirstOrDefault(i => i.Name.StartsWith("ICommandHandler") || i.Name.StartsWith("IUseCaseHandler"));
        if (handlerInterface != null && handlerInterface.TypeArguments.Length > 0 && SymbolEqualityComparer.Default.Equals(handlerInterface.TypeArguments[0], argType))
        {
            if (!IsBypassed(containingType, "MOD004")) context.ReportDiagnostic(Diagnostic.Create(RuleRecursiveCall, invocation.Syntax.GetLocation(), argType.Name));
        }

        if (isCmdHandler && targetMethod.Name == "Send" && ImplementsInterface(argType, "ICommand"))
        {
            if (!IsBypassed(containingType, "MOD019")) context.ReportDiagnostic(Diagnostic.Create(RuleCommandChaining, invocation.Syntax.GetLocation(), containingType.Name, argType.Name));
        }
        if (isEvtHandler && targetMethod.Name == "Publish" && ImplementsInterface(argType, "IEvent"))
        {
            if (!IsBypassed(containingType, "MOD020")) context.ReportDiagnostic(Diagnostic.Create(RuleEventChaining, invocation.Syntax.GetLocation(), containingType.Name, argType.Name));
        }
    }

    /// <summary>
    /// Analyzes the body of 'Handle' methods within handlers to verify complexity metrics, 
    /// limiting orchestrator call depth and command dispatch counts.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    private void AnalyzeMethodBody(OperationAnalysisContext context)
    {
        var methodBody = (IMethodBodyOperation)context.Operation;
        var methodSymbol = context.ContainingSymbol as IMethodSymbol;
        if (methodSymbol == null || methodSymbol.Name != "Handle" || methodSymbol.Locations.Length == 0) return;

        var primaryLocation = methodSymbol.Locations[0];
        var containingType = methodSymbol.ContainingType;

        bool isUseCaseHandler = containingType.AllInterfaces.Any(i => i.Name.StartsWith("IUseCaseHandler"));
        if (!isUseCaseHandler && !containingType.AllInterfaces.Any(i => i.Name.StartsWith("ICommandHandler"))) return;

        int orchestratorCalls = 0;
        int commandCalls = 0;

        foreach (var op in methodBody.Descendants())
        {
            if (op is IInvocationOperation inv)
            {
                if (inv.TargetMethod.Name == "Execute" && inv.TargetMethod.ContainingType.Name == "IOrchestrator") orchestratorCalls++;
                if (inv.TargetMethod.Name == "Send" && inv.TargetMethod.ContainingType.Name.EndsWith("Dispatcher")) commandCalls++;
            }
        }

        if (orchestratorCalls >= 3 && !IsBypassed(containingType, "MOD012")) context.ReportDiagnostic(Diagnostic.Create(RuleMaxDepth, primaryLocation));
        if (isUseCaseHandler && commandCalls > 5 && !IsBypassed(containingType, "MOD014")) context.ReportDiagnostic(Diagnostic.Create(RuleTooManyCommands, primaryLocation, containingType.Name, commandCalls));
    }

    /// <summary>
    /// Analyzes pragma directives to forbid the manual suppression of architectural 
    /// warnings via `#pragma warning disable`, enforcing the use of `[ArchitectureBypass]`.
    /// </summary>
    /// <param name="context">The syntax node analysis context.</param>
    private void AnalyzePragma(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not PragmaWarningDirectiveTriviaSyntax pragma) return;
        if (!pragma.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword)) return;
        foreach (var code in pragma.ErrorCodes) { string ruleId = code.ToString(); if (ruleId.StartsWith("MOD")) context.ReportDiagnostic(Diagnostic.Create(RuleNoPragmas, code.GetLocation(), ruleId)); }
    }

    // =================================================================================================
    // HELPERS & VALIDATION CORE
    // =================================================================================================

    /// <summary>
    /// Core validation method that checks if a type reference crosses forbidden 
    /// module boundaries. Emits diagnostics for illegal references and direct instantiations.
    /// </summary>
    /// <param name="type">The symbol type being evaluated.</param>
    /// <param name="context">The unified analysis context wrapper.</param>
    /// <param name="location">The location in source code where the violation occurred.</param>
    /// <param name="isInstantiation">Flag indicating if the type is being manually instantiated.</param>
    private void ValidateType(ITypeSymbol type, AnalysisContextWrapper context, Location location, bool isInstantiation = false)
    {
        if (type == null || type.SpecialType != SpecialType.None || type is IArrayTypeSymbol) return;

        var currentTree = context.SyntaxTree;
        if (currentTree == null || currentTree.FilePath.IndexOf("Modules", StringComparison.OrdinalIgnoreCase) < 0) return;

        var targetAssembly = type.ContainingAssembly;
        if (targetAssembly == null) return;
        var targetName = targetAssembly.Name;

        if (SymbolEqualityComparer.Default.Equals(context.Compilation.Assembly, targetAssembly)) return;
        if (IsSafeReference(targetName)) return;

        bool isTargetInModulesFolder = false;
        if (type.Locations.Length > 0 && type.Locations[0].IsInSource)
        {
            string path = type.Locations[0].SourceTree?.FilePath ?? "";
            if (path.IndexOf("Modules", StringComparison.OrdinalIgnoreCase) >= 0) isTargetInModulesFolder = true;
        }
        else if (targetName.Contains(".Module.") || targetName.Contains(".Modules."))
        {
            isTargetInModulesFolder = true;
        }

        if (isTargetInModulesFolder)
        {
            if (targetName.EndsWith(".Api")) return;
            if (IsBypassed(context.Symbol, "MOD005") || IsBypassed(context.Symbol, "MOD026")) return;

            if (isInstantiation)
                context.ReportDiagnostic(Diagnostic.Create(RuleDirectInstantiation, location, type.Name, targetName));
            else
                context.ReportDiagnostic(Diagnostic.Create(RuleIllegalReference, location, type.Name, targetName));
        }
    }

    /// <summary>
    /// Determines if an assembly reference is considered globally safe (e.g., System, Microsoft, Contracts).
    /// </summary>
    private bool IsSafeReference(string name)
    {
        if (string.IsNullOrEmpty(name)) return true;
        return name.StartsWith("System") || name.StartsWith("Microsoft") || name.StartsWith("netstandard") || name.StartsWith("mscorlib")
            || name.EndsWith(".Api") || name.EndsWith(".Contracts") || name.EndsWith(".Shared");
    }

    /// <summary>
    /// Determines if a given type symbol represents the IServiceProvider pattern.
    /// </summary>
    private bool IsServiceLocator(ITypeSymbol type) { if (type == null) return false; return type.Name == "IServiceProvider" || type.Name == "ServiceProvider"; }

    /// <summary>
    /// Checks if the namespace structure follows the mandated folder hierarchy for the module.
    /// </summary>
    private bool IsNamespaceAllowed(string relativeNs) { if (string.IsNullOrEmpty(relativeNs)) return true; if (relativeNs.StartsWith(".Contracts") || relativeNs.StartsWith(".Api") || relativeNs.StartsWith(".Domain") || relativeNs.StartsWith(".Shared") || relativeNs.StartsWith(".Infrastructure")) return true; if (relativeNs.StartsWith(".Application")) { if (relativeNs.StartsWith(".Application.UseCases") || relativeNs.StartsWith(".Application.CommandHandlers") || relativeNs.StartsWith(".Application.QueryHandlers") || relativeNs.StartsWith(".Application.EventHandlers")) return true; return false; } return false; }

    /// <summary>
    /// Verifies if a symbol or its containing type is decorated with the [ArchitectureBypass] attribute for a specific rule.
    /// </summary>
    private static bool IsBypassed(ISymbol symbol, string ruleId) { if (symbol == null) return false; foreach (var attr in symbol.GetAttributes()) { if (attr.AttributeClass?.Name == "ArchitectureBypassAttribute") { if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string bypassedId && bypassedId == ruleId) return true; } } return symbol.ContainingType != null && IsBypassed(symbol.ContainingType, ruleId); }

    /// <summary>
    /// Checks if a type inherits from a specific base class by recursively walking up the inheritance tree.
    /// </summary>
    private static bool BaseTypeMatches(ITypeSymbol type, string targetName) { while (type.BaseType != null) { if (type.BaseType.Name == targetName) return true; type = type.BaseType; } return false; }

    /// <summary>
    /// Checks if a type implements a specified interface by name.
    /// </summary>
    private static bool ImplementsInterface(ITypeSymbol type, string interfaceName) => type.AllInterfaces.Any(i => i.Name.Contains(interfaceName));

    /// <summary>
    /// A lightweight wrapper struct to unify SymbolAnalysisContext and OperationAnalysisContext 
    /// for shared validation methods.
    /// </summary>
    private struct AnalysisContextWrapper
    {
        private readonly Action<Diagnostic> _report;
        public Compilation Compilation { get; }
        public ISymbol Symbol { get; }
        public SyntaxTree SyntaxTree => Symbol?.Locations.FirstOrDefault()?.SourceTree ?? (Compilation.SyntaxTrees.FirstOrDefault());
        public AnalysisContextWrapper(SymbolAnalysisContext c) { _report = c.ReportDiagnostic; Compilation = c.Compilation; Symbol = c.Symbol; }
        public AnalysisContextWrapper(OperationAnalysisContext c) { _report = c.ReportDiagnostic; Compilation = c.Compilation; Symbol = c.ContainingSymbol; }
        public void ReportDiagnostic(Diagnostic d) => _report(d);
        public static implicit operator AnalysisContextWrapper(SymbolAnalysisContext c) => new(c);
        public static implicit operator AnalysisContextWrapper(OperationAnalysisContext c) => new(c);
    }
}
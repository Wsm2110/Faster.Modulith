using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Rename;

namespace Faster.Modulith.Analyzers.CodeFixProviders;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ModularMonolithCodeFixProvider)), Shared]
public class ModularMonolithCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create("MOD001", "MOD002", "MOD003", "MOD018", "MOD006", "MOD007", "MOD023", "MOD024", "MOD026");

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    private enum ScaffoldingMode { Basic, WithMessage, FullSuite }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Id == "MOD023")
            {
                var trivia = root?.FindTrivia(diagnostic.Location.SourceSpan.Start);
                if (trivia.HasValue && trivia.Value.IsKind(SyntaxKind.PragmaWarningDirectiveTrivia))
                    context.RegisterCodeFix(CodeAction.Create("❌ Remove Forbidden Pragma", c => RemovePragmaAsync(context.Document, trivia.Value, c), "RemovePragma"), diagnostic);
                continue;
            }

            var node = root?.FindNode(diagnostic.Location.SourceSpan);
            var typeDecl = node?.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            if (typeDecl == null) continue;

            if (diagnostic.Id == "MOD026")
                context.RegisterCodeFix(CodeAction.Create("📦 Move to .Api Project", c => MoveToApiProjectAsync(context.Document, typeDecl, c), "MoveToApi"), diagnostic);

            if (diagnostic.Id == "MOD001" || diagnostic.Id == "MOD003" || diagnostic.Id == "MOD018")
                context.RegisterCodeFix(CodeAction.Create("🔒 Make Internal", c => MakeInternalAsync(context.Document, typeDecl, c), "MakeInternal"), diagnostic);
            if (diagnostic.Id == "MOD002")
                context.RegisterCodeFix(CodeAction.Create("🔓 Make Public", c => MakePublicAsync(context.Document, typeDecl, c), "MakePublic"), diagnostic);
            if (diagnostic.Id == "MOD006" && typeDecl is ClassDeclarationSyntax)
                context.RegisterCodeFix(CodeAction.Create("📝 Convert to Record", c => ConvertToRecordAsync(context.Document, typeDecl, c), "ConvertToRecord"), diagnostic);
            if (diagnostic.Id == "MOD007")
            {
                string s = DeduceSuffix(typeDecl);
                if (!string.IsNullOrEmpty(s)) context.RegisterCodeFix(CodeAction.Create($"🏷️ Rename to '...{s}'", c => RenameSymbolAsync(context.Document, typeDecl, s, c), "FixSuffix"), diagnostic);
            }

            if (diagnostic.Id == "MOD024" && typeDecl is ClassDeclarationSyntax classDecl)
            {
                context.RegisterCodeFix(CodeAction.Create("🔨 Scaffold Handler Only", c => ScaffoldHandlerAsync(context.Document, classDecl, ScaffoldingMode.Basic, c), "ScaffoldBasic"), diagnostic);
                context.RegisterCodeFix(CodeAction.Create("🔨 Scaffold Handler + Message", c => ScaffoldHandlerAsync(context.Document, classDecl, ScaffoldingMode.WithMessage, c), "ScaffoldMsg"), diagnostic);
                context.RegisterCodeFix(CodeAction.Create("🔨 Scaffold Handler + Message + Validator", c => ScaffoldHandlerAsync(context.Document, classDecl, ScaffoldingMode.FullSuite, c), "ScaffoldFull"), diagnostic);
            }
        }
    }

    // =================================================================================================
    // LOGIC: SCAFFOLDING
    // =================================================================================================
    private async Task<Solution> ScaffoldHandlerAsync(Document document, ClassDeclarationSyntax classDecl, ScaffoldingMode mode, CancellationToken ct)
    {
        var solution = document.Project.Solution;
        var className = classDecl.Identifier.Text;

        // 1. Extract namespace
        var namespaceDecl = classDecl.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        var namespaceName = namespaceDecl?.Name.ToString() ?? "";

        string suffix = "", interfaceName = "", messageInterface = "", methodParam = "", returnType = "ValueTask", resultType = "";
        bool isTwoGenericArgs = false;
        bool isUseCase = false;

        // 2. Identification Logic
        if (document.Name.Contains("CommandHandlers") ||
            className.EndsWith("CommandHandler") ||
            namespaceName.Contains("CommandHandlers") ||
            namespaceName.Contains("CommandHandler"))
        {
            suffix = "Command";
            interfaceName = "ICommandHandler";
            messageInterface = "ICommand<Result>";
            methodParam = "command";
            returnType = "ValueTask<Result>";
            resultType = "Result";
            isTwoGenericArgs = true;
        }
        else if (document.Name.Contains("EventHandlers") ||
                 className.EndsWith("EventHandler") ||
                 namespaceName.Contains("EventHandlers") ||
                 namespaceName.Contains("EventHandler"))
        {
            suffix = "Event";
            interfaceName = "IEventHandler";
            messageInterface = "IEvent";
            methodParam = "@event";
            returnType = "ValueTask"; // ✅ Fixed: Explicitly ValueTask
            isTwoGenericArgs = false;
        }
        else if (document.Name.Contains("UseCases") ||
                 className.EndsWith("UseCaseHandler") ||
                 namespaceName.Contains("UseCases") ||
                 namespaceName.Contains("UseCaseHandler"))
        {
            suffix = "UseCase";
            interfaceName = "IUseCaseHandler";
            messageInterface = "IUseCase<Result<object>>";
            methodParam = "useCase";
            returnType = "ValueTask<Result<object>>";
            resultType = "Result<object>";
            isTwoGenericArgs = true;
            isUseCase = true;
        }
        else
        {
            return solution;
        }

        // 3. Determine Message Name
        string coreName = className;
        if (coreName.EndsWith("Handler")) coreName = coreName.Substring(0, coreName.Length - 7);
        string targetMessageName = coreName.EndsWith(suffix) ? coreName : coreName + suffix;

        var searchResult = await FindMessageTypeAsync(document, targetMessageName, ct);
        bool typeExists = searchResult.Found;

        // 4. Build Interface Syntax (IEventHandler<T>)
        var typeArgs = SyntaxFactory.TypeArgumentList().AddArguments(searchResult.TypeNode);
        if (isTwoGenericArgs) typeArgs = typeArgs.AddArguments(SyntaxFactory.ParseTypeName(resultType));

        var baseType = SyntaxFactory.SimpleBaseType(SyntaxFactory.GenericName(SyntaxFactory.Identifier(interfaceName)).WithTypeArgumentList(typeArgs));

        // 5. Build Handle Method Stub (ValueTask)
        var methodStub = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(returnType), "Handle")
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed, SyntaxFactory.Whitespace("    "));

        // If return type is Task or ValueTask, we make it async to satisfy compiler (throws exception anyway)
        if (returnType.StartsWith("Task") || returnType.StartsWith("ValueTask"))
            methodStub = methodStub.AddModifiers(SyntaxFactory.Token(SyntaxKind.AsyncKeyword));

        methodStub = methodStub.WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new[] {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier(methodParam)).WithType(searchResult.TypeNode),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("ct")).WithType(SyntaxFactory.ParseTypeName("CancellationToken"))
        }))).WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement("throw new NotImplementedException();")));

        // 6. Update Class (Modifiers & BaseList)
        var docRoot = await document.GetSyntaxRootAsync(ct) as CompilationUnitSyntax;
        var currentClass = docRoot.FindNode(classDecl.Span).FirstAncestorOrSelf<ClassDeclarationSyntax>();
        var updatedClass = currentClass;

        if (updatedClass.OpenBraceToken.IsMissing || updatedClass.SemicolonToken.IsKind(SyntaxKind.SemicolonToken))
        {
            updatedClass = updatedClass
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed))
                .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken));
        }

        // Fix BaseList Formatting
        var baseList = updatedClass.BaseList;
        if (baseList == null)
        {
            var originalTrivia = updatedClass.Identifier.TrailingTrivia;
            updatedClass = updatedClass.WithIdentifier(updatedClass.Identifier.WithTrailingTrivia(SyntaxFactory.Space));
            var colon = SyntaxFactory.Token(SyntaxKind.ColonToken).WithTrailingTrivia(SyntaxFactory.Space);
            baseList = SyntaxFactory.BaseList(colon, SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(baseType));
            baseList = baseList.WithTrailingTrivia(originalTrivia.Any() ? originalTrivia : SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed));
        }
        else
        {
            baseList = baseList.AddTypes(baseType);
        }

        updatedClass = updatedClass.WithBaseList(baseList).AddMembers(methodStub);

        // Force Internal Modifier
        if (updatedClass.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
        {
            var pubToken = updatedClass.Modifiers.First(m => m.IsKind(SyntaxKind.PublicKeyword));
            updatedClass = updatedClass.WithModifiers(updatedClass.Modifiers.Replace(pubToken, SyntaxFactory.Token(SyntaxKind.InternalKeyword)));
        }
        else if (!updatedClass.Modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword)))
        {
            updatedClass = updatedClass.AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
        }

        docRoot = docRoot.ReplaceNode(currentClass, updatedClass);
        docRoot = AddStandardUsings(docRoot, mode == ScaffoldingMode.FullSuite);

        if (typeExists && searchResult.UsingDir != null)
            if (!docRoot.Usings.Any(u => u.Name.ToString() == searchResult.UsingDir.Name.ToString()))
                docRoot = docRoot.AddUsings(searchResult.UsingDir);

        solution = solution.WithDocumentSyntaxRoot(document.Id, docRoot);

        // 7. Generate Message Record (if missing)
        if (mode >= ScaffoldingMode.WithMessage && !typeExists)
        {
            Project targetProject = null;

            // Look for Api project for Events or UseCases
            if (isUseCase || suffix == "Event")
            {
                targetProject = FindPeerProject(solution, document.Project, ".Application", ".Api");
                if (targetProject == null)
                {
                    string fallbackApiName = document.Project.Name + ".Api";
                    targetProject = solution.Projects.FirstOrDefault(p => p.Name == fallbackApiName);
                }
            }

            if (targetProject != null && targetProject.Id != document.Project.Id)
            {
                // External Api Project
                string targetNamespaceName = targetProject.DefaultNamespace ?? targetProject.Name;

                var newRecordDecl = SyntaxFactory.RecordDeclaration(SyntaxFactory.Token(SyntaxKind.RecordKeyword), targetMessageName)
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                    .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(messageInterface)))))
                    .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken)).WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken));

                var newCu = SyntaxFactory.CompilationUnit()
                    .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")))
                    .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Faster.Modulith.Contracts")))
                    .AddMembers(SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.ParseName(targetNamespaceName)).AddMembers(newRecordDecl));

                string absolutePath = null;
                string folderName = suffix == "UseCase" ? "UseCases" : (suffix + "s"); // "Events"
                if (!string.IsNullOrEmpty(targetProject.FilePath))
                {
                    string directory = Path.GetDirectoryName(targetProject.FilePath);
                    absolutePath = Path.Combine(directory, folderName, $"{targetMessageName}.cs");
                }

                var newDocId = DocumentId.CreateNewId(targetProject.Id);
                solution = solution.AddDocument(newDocId, $"{targetMessageName}.cs", newCu, folders: new[] { folderName }, filePath: absolutePath);

                // Update original file usings
                var handlerDoc = solution.GetDocument(document.Id);
                var handlerRoot = await handlerDoc.GetSyntaxRootAsync(ct) as CompilationUnitSyntax;
                if (!handlerRoot.Usings.Any(u => u.Name.ToString() == targetNamespaceName))
                {
                    var newRoot = handlerRoot.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(targetNamespaceName)));
                    solution = solution.WithDocumentSyntaxRoot(document.Id, newRoot);
                }
            }
            else
            {
                // Local Internal Contract (Commands)
                var newRecordDecl = SyntaxFactory.RecordDeclaration(SyntaxFactory.Token(SyntaxKind.RecordKeyword), targetMessageName)
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InternalKeyword)))
                    .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(messageInterface)))))
                    .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken)).WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken))
                    .WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed, SyntaxFactory.CarriageReturnLineFeed);

                var handlerDoc = solution.GetDocument(document.Id);
                var handlerRoot = await handlerDoc.GetSyntaxRootAsync(ct) as CompilationUnitSyntax;

                if (!handlerRoot.Usings.Any(u => u.Name.ToString() == "Faster.Modulith.Contracts"))
                    handlerRoot = handlerRoot.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Faster.Modulith.Contracts")));

                var newRoot = AppendMember(handlerRoot, newRecordDecl);
                solution = solution.WithDocumentSyntaxRoot(document.Id, newRoot);
            }
        }

        // 8. Generate Validator
        if (mode == ScaffoldingMode.FullSuite)
        {
            var handlerDoc = solution.GetDocument(document.Id);
            var handlerRoot = await handlerDoc.GetSyntaxRootAsync(ct) as CompilationUnitSyntax;
            var validatorName = coreName.EndsWith(suffix) ? coreName.Replace(suffix, "") + "Validator" : coreName + "Validator";

            var validatorClass = SyntaxFactory.ClassDeclaration(validatorName)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InternalKeyword)))
                .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                    SyntaxFactory.SimpleBaseType(SyntaxFactory.GenericName("AbstractValidator").WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ParseTypeName(targetMessageName)))))
                )))
                .WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed, SyntaxFactory.CarriageReturnLineFeed)
                .AddMembers(
                    SyntaxFactory.ConstructorDeclaration(validatorName)
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                    .WithBody(SyntaxFactory.Block().WithOpenBraceToken(
                        SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed, SyntaxFactory.Whitespace("            "), SyntaxFactory.Comment("// RuleFor(c => c.Id).NotEqual(0).WithMessage(\"Id cannot be 0\");"), SyntaxFactory.CarriageReturnLineFeed, SyntaxFactory.Whitespace("        "))))
                );

            var newRoot = AppendMember(handlerRoot, validatorClass);
            solution = solution.WithDocumentSyntaxRoot(document.Id, newRoot);
        }

        return solution;
    }

    // =================================================================================================
    // LOGIC: MOVE TO API (MOD026)
    // =================================================================================================
    private async Task<Solution> MoveToApiProjectAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken ct)
    {
        var solution = document.Project.Solution; var currentProject = document.Project; string typeName = typeDecl.Identifier.Text;
        var targetProject = FindPeerProject(solution, currentProject, ".Application", ".Api");
        if (targetProject == null || targetProject.Id == currentProject.Id) return solution;
        string targetNamespace = targetProject.DefaultNamespace ?? targetProject.Name;
        var publicToken = SyntaxFactory.Token(SyntaxKind.PublicKeyword); var newModifiers = SyntaxFactory.TokenList(publicToken);
        var newTypeDecl = typeDecl.WithModifiers(newModifiers).WithAttributeLists(typeDecl.AttributeLists).WithBaseList(typeDecl.BaseList).WithMembers(typeDecl.Members);
        var newCu = SyntaxFactory.CompilationUnit()
            .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")))
            .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Faster.Modulith.Contracts")))
            .AddMembers(SyntaxFactory.FileScopedNamespaceDeclaration(SyntaxFactory.ParseName(targetNamespace)).AddMembers(newTypeDecl));
        string absolutePath = null;
        if (!string.IsNullOrEmpty(targetProject.FilePath)) { string directory = Path.GetDirectoryName(targetProject.FilePath); absolutePath = Path.Combine(directory, $"{typeName}.cs"); }
        var newDocId = DocumentId.CreateNewId(targetProject.Id); solution = solution.AddDocument(newDocId, $"{typeName}.cs", newCu, folders: null, filePath: absolutePath);
        var root = await document.GetSyntaxRootAsync(ct);
        if (root.DescendantNodes().OfType<TypeDeclarationSyntax>().Count() == 1) { solution = solution.RemoveDocument(document.Id); } else { var newRoot = root.RemoveNode(typeDecl, SyntaxRemoveOptions.KeepNoTrivia); solution = solution.WithDocumentSyntaxRoot(document.Id, newRoot); }
        return solution;
    }

    // =================================================================================================
    // HELPERS
    // =================================================================================================
    private Project FindPeerProject(Solution solution, Project currentProject, string oldSuffix, string newSuffix)
    {
        string curName = currentProject.Name; string curAssembly = currentProject.AssemblyName;
        // Strategy 1: Replace .Application -> .Api
        if (curName.Contains(oldSuffix))
        {
            var target = solution.Projects.FirstOrDefault(p => p.Name == curName.Replace(oldSuffix, newSuffix));
            if (target != null) return target;
        }
        // Strategy 2: Append .Api
        var targetAppend = solution.Projects.FirstOrDefault(p => p.Name == curName + newSuffix || p.AssemblyName == curAssembly + newSuffix);
        if (targetAppend != null) return targetAppend;
        // Strategy 3: Loose Search for .Api module
        var parts = curName.Split('.');
        int moduleIndex = Array.IndexOf(parts, "Module");
        if (moduleIndex != -1 && moduleIndex + 1 < parts.Length)
        {
            string moduleName = parts[moduleIndex + 1];
            var fuzzyMatch = solution.Projects.FirstOrDefault(p => p.Name.Contains($".Module.{moduleName}") && p.Name.EndsWith(newSuffix));
            if (fuzzyMatch != null) return fuzzyMatch;
        }
        return null;
    }

    private async Task<(TypeSyntax TypeNode, UsingDirectiveSyntax? UsingDir, bool Found)> FindMessageTypeAsync(Document document, string targetName, CancellationToken ct)
    {
        var solution = document.Project.Solution; var foundSymbols = new List<ISymbol>();
        foreach (var project in solution.Projects)
        {
            if (!project.Name.Contains(".Module.") && !project.Name.Contains(".Contracts") && !project.Name.Contains(".Api")) continue;
            var symbols = await SymbolFinder.FindDeclarationsAsync(project, targetName, ignoreCase: false, cancellationToken: ct); foundSymbols.AddRange(symbols);
        }
        var targetSymbol = foundSymbols.OfType<INamedTypeSymbol>().Where(s => s.TypeKind == TypeKind.Class || s.TypeKind == TypeKind.Struct || s.IsRecord || s.TypeKind == TypeKind.Interface).OrderByDescending(s => ScoreMatch(s, document)).FirstOrDefault();
        if (targetSymbol != null)
        {
            var typeNode = SyntaxFactory.ParseTypeName(targetSymbol.Name);
            if (targetSymbol.ContainingNamespace.IsGlobalNamespace) return (typeNode, null, true);
            var usingDir = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(targetSymbol.ContainingNamespace.ToDisplayString())); return (typeNode, usingDir, true);
        }
        return (SyntaxFactory.ParseTypeName(targetName), null, false);
    }

    private CompilationUnitSyntax AddStandardUsings(CompilationUnitSyntax root, bool addFluentValidation)
    {
        if (!root.Usings.Any(u => u.Name.ToString() == "System.Threading")) root = root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading")));
        if (!root.Usings.Any(u => u.Name.ToString() == "System.Threading.Tasks")) root = root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading.Tasks")));
        if (addFluentValidation && !root.Usings.Any(u => u.Name.ToString() == "FluentValidation")) root = root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("FluentValidation")));
        string contractsNs = "Faster.Modulith.Contracts";
        if (!root.Usings.Any(u => u.Name.ToString() == contractsNs)) root = root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(contractsNs)));
        return root;
    }

    private int ScoreMatch(INamedTypeSymbol symbol, Document currentDoc) { var ns = symbol.ContainingNamespace.ToDisplayString(); var assembly = symbol.ContainingAssembly.Name; if (assembly == currentDoc.Project.AssemblyName) return 10; if (ns.Contains(".Contracts")) return 5; if (ns.Contains(".Api")) return 4; return 1; }
    private CompilationUnitSyntax AppendMember(CompilationUnitSyntax root, MemberDeclarationSyntax member) { var ns = root.Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault(); if (ns != null) return root.ReplaceNode(ns, ns.AddMembers(member)); if (root.Members.OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault() is { } fileNs) return root.ReplaceNode(fileNs, fileNs.AddMembers(member)); return root.AddMembers(member); }
    private async Task<Document> MakeInternalAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken ct) { var publicToken = typeDecl.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.PublicKeyword)); var internalToken = SyntaxFactory.Token(SyntaxKind.InternalKeyword); SyntaxTokenList newModifiers; if (publicToken.IsKind(SyntaxKind.PublicKeyword)) { internalToken = internalToken.WithTriviaFrom(publicToken); newModifiers = typeDecl.Modifiers.Replace(publicToken, internalToken); } else { internalToken = internalToken.WithTrailingTrivia(SyntaxFactory.Space); newModifiers = typeDecl.Modifiers.Insert(0, internalToken); } var root = await document.GetSyntaxRootAsync(ct); return document.WithSyntaxRoot(root!.ReplaceNode(typeDecl, typeDecl.WithModifiers(newModifiers))); }
    private async Task<Document> MakePublicAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken ct) { var internalToken = typeDecl.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.InternalKeyword)); var publicToken = SyntaxFactory.Token(SyntaxKind.PublicKeyword); SyntaxTokenList newModifiers; if (internalToken.IsKind(SyntaxKind.InternalKeyword)) { publicToken = publicToken.WithTriviaFrom(internalToken); newModifiers = typeDecl.Modifiers.Replace(internalToken, publicToken); } else { publicToken = publicToken.WithTrailingTrivia(SyntaxFactory.Space); newModifiers = typeDecl.Modifiers.Insert(0, publicToken); } var root = await document.GetSyntaxRootAsync(ct); return document.WithSyntaxRoot(root!.ReplaceNode(typeDecl, typeDecl.WithModifiers(newModifiers))); }
    private async Task<Document> RemovePragmaAsync(Document document, SyntaxTrivia trivia, CancellationToken ct) { var root = await document.GetSyntaxRootAsync(ct); return document.WithSyntaxRoot(root!.ReplaceTrivia(trivia, default(SyntaxTrivia))); }
    private async Task<Document> ConvertToRecordAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken ct) { if (typeDecl is not ClassDeclarationSyntax classDecl) return document; var recordToken = SyntaxFactory.Token(SyntaxKind.RecordKeyword).WithTriviaFrom(classDecl.Keyword); var recordDecl = SyntaxFactory.RecordDeclaration(classDecl.AttributeLists, classDecl.Modifiers, recordToken, classDecl.Identifier, classDecl.TypeParameterList, classDecl.ParameterList, classDecl.BaseList, classDecl.ConstraintClauses, classDecl.OpenBraceToken, classDecl.Members, classDecl.CloseBraceToken, default); var root = await document.GetSyntaxRootAsync(ct); return document.WithSyntaxRoot(root!.ReplaceNode(typeDecl, recordDecl)); }
    private async Task<Solution> RenameSymbolAsync(Document document, TypeDeclarationSyntax typeDecl, string suffix, CancellationToken ct) { var semanticModel = await document.GetSemanticModelAsync(ct); var symbol = semanticModel.GetDeclaredSymbol(typeDecl, ct); if (symbol == null) return document.Project.Solution; return await Renamer.RenameSymbolAsync(solution: document.Project.Solution, symbol: symbol, options: default, newName: symbol.Name + suffix, cancellationToken: ct); }
    private string DeduceSuffix(TypeDeclarationSyntax typeDecl) { if (typeDecl.BaseList == null) return ""; var baseTypes = typeDecl.BaseList.Types.ToString(); if (baseTypes.Contains("ICommand")) return "Command"; if (baseTypes.Contains("IEvent")) return "Event"; if (baseTypes.Contains("IUseCase")) return "UseCase"; if (baseTypes.Contains("ICommandHandler")) return "Handler"; if (baseTypes.Contains("IEventHandler")) return "Handler"; return ""; }
}
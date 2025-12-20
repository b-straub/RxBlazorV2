using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Diagnostics;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace RxBlazorV2CodeFix.CodeFix;

/// <summary>
/// Provides code fixes for RXBG080 (ObservableModelObserver invalid signature).
/// Offers to fix the method signature to a valid pattern.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ObservableModelObserverCodeFixProvider)), Shared]
public class ObservableModelObserverCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.ObservableModelObserverInvalidSignatureError.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in context.Diagnostics.Where(d => FixableDiagnosticIds.Contains(d.Id)))
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the method declaration
            var methodDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
                .OfType<MethodDeclarationSyntax>().FirstOrDefault();

            if (methodDeclaration is null)
            {
                continue;
            }

            // Get model type name from diagnostic (third parameter in message format)
            var modelTypeName = diagnostic.Properties.TryGetValue("ModelTypeName", out var mtn) ? mtn : "ModelType";

            // Determine if this is an async method based on current return type
            var methodSymbol = semanticModel?.GetDeclaredSymbol(methodDeclaration);
            var isCurrentlyAsync = methodSymbol is not null &&
                (methodSymbol.ReturnType.Name == "Task" || methodSymbol.ReturnType.Name == "ValueTask" ||
                 methodSymbol.IsAsync);

            if (isCurrentlyAsync)
            {
                // Offer async fixes
                var fixAsyncWithCt = CodeAction.Create(
                    title: $"Fix signature: Task {methodDeclaration.Identifier.Text}({modelTypeName} model, CancellationToken ct)",
                    createChangedDocument: c => FixAsyncMethodSignatureAsync(context.Document, root, methodDeclaration, modelTypeName, includeCancellationToken: true, c),
                    equivalenceKey: "FixAsyncWithCt");

                var fixAsyncWithoutCt = CodeAction.Create(
                    title: $"Fix signature: Task {methodDeclaration.Identifier.Text}({modelTypeName} model)",
                    createChangedDocument: c => FixAsyncMethodSignatureAsync(context.Document, root, methodDeclaration, modelTypeName, includeCancellationToken: false, c),
                    equivalenceKey: "FixAsyncWithoutCt");

                context.RegisterCodeFix(fixAsyncWithCt, diagnostic);
                context.RegisterCodeFix(fixAsyncWithoutCt, diagnostic);
            }
            else
            {
                // Offer sync fix
                var fixSync = CodeAction.Create(
                    title: $"Fix signature: void {methodDeclaration.Identifier.Text}({modelTypeName} model)",
                    createChangedDocument: c => FixSyncMethodSignatureAsync(context.Document, root, methodDeclaration, modelTypeName, c),
                    equivalenceKey: "FixSync");

                context.RegisterCodeFix(fixSync, diagnostic);
            }

            // Always offer to convert between sync/async
            if (isCurrentlyAsync)
            {
                var convertToSync = CodeAction.Create(
                    title: $"Convert to sync: void {methodDeclaration.Identifier.Text}({modelTypeName} model)",
                    createChangedDocument: c => FixSyncMethodSignatureAsync(context.Document, root, methodDeclaration, modelTypeName, c),
                    equivalenceKey: "ConvertToSync");

                context.RegisterCodeFix(convertToSync, diagnostic);
            }
            else
            {
                var convertToAsync = CodeAction.Create(
                    title: $"Convert to async: Task {methodDeclaration.Identifier.Text}({modelTypeName} model)",
                    createChangedDocument: c => FixAsyncMethodSignatureAsync(context.Document, root, methodDeclaration, modelTypeName, includeCancellationToken: false, c),
                    equivalenceKey: "ConvertToAsync");

                context.RegisterCodeFix(convertToAsync, diagnostic);
            }
        }
    }

    private static Task<Document> FixSyncMethodSignatureAsync(
        Document document,
        SyntaxNode root,
        MethodDeclarationSyntax methodDeclaration,
        string? modelTypeName,
        CancellationToken cancellationToken)
    {
        // Create parameter: ModelType model
        var modelParameter = SyntaxFactory.Parameter(
            SyntaxFactory.Identifier("model"))
            .WithType(SyntaxFactory.ParseTypeName(modelTypeName ?? "ModelType"));

        var parameterList = SyntaxFactory.ParameterList(
            SyntaxFactory.SingletonSeparatedList(modelParameter));

        // Create void return type
        var voidReturnType = SyntaxFactory.PredefinedType(
            SyntaxFactory.Token(SyntaxKind.VoidKeyword));

        // Remove async modifier if present
        var newModifiers = methodDeclaration.Modifiers
            .Where(m => !m.IsKind(SyntaxKind.AsyncKeyword))
            .ToList();

        var newMethod = methodDeclaration
            .WithReturnType(voidReturnType.WithTrailingTrivia(SyntaxFactory.Space))
            .WithModifiers(SyntaxFactory.TokenList(newModifiers))
            .WithParameterList(parameterList);

        var newRoot = root.ReplaceNode(methodDeclaration, newMethod);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static Task<Document> FixAsyncMethodSignatureAsync(
        Document document,
        SyntaxNode root,
        MethodDeclarationSyntax methodDeclaration,
        string? modelTypeName,
        bool includeCancellationToken,
        CancellationToken cancellationToken)
    {
        // Create model parameter
        var modelParameter = SyntaxFactory.Parameter(
            SyntaxFactory.Identifier("model"))
            .WithType(SyntaxFactory.ParseTypeName(modelTypeName ?? "ModelType"));

        var parameters = new List<ParameterSyntax> { modelParameter };

        if (includeCancellationToken)
        {
            var ctParameter = SyntaxFactory.Parameter(
                SyntaxFactory.Identifier("ct"))
                .WithType(SyntaxFactory.ParseTypeName("CancellationToken"));
            parameters.Add(ctParameter);
        }

        var parameterList = SyntaxFactory.ParameterList(
            SyntaxFactory.SeparatedList(parameters));

        // Create Task return type
        var taskReturnType = SyntaxFactory.ParseTypeName("Task");

        // Ensure async modifier is present
        var hasAsync = methodDeclaration.Modifiers.Any(SyntaxKind.AsyncKeyword);
        var newModifiers = hasAsync
            ? methodDeclaration.Modifiers
            : methodDeclaration.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(SyntaxFactory.Space));

        var newMethod = methodDeclaration
            .WithReturnType(taskReturnType.WithTrailingTrivia(SyntaxFactory.Space))
            .WithModifiers(newModifiers)
            .WithParameterList(parameterList);

        var newRoot = root.ReplaceNode(methodDeclaration, newMethod);

        // Add using for System.Threading if CancellationToken is added
        if (includeCancellationToken)
        {
            newRoot = SyntaxHelpers.AddUsingDirectives(newRoot, "System.Threading");
        }

        // Add using for System.Threading.Tasks for Task
        newRoot = SyntaxHelpers.AddUsingDirectives(newRoot, "System.Threading.Tasks");

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}

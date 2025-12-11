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
/// Provides code fixes for RXBG082 (Internal model observer invalid signature).
/// Offers to fix method visibility and signature to valid internal observer patterns.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InternalModelObserverCodeFixProvider)), Shared]
public class InternalModelObserverCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.InternalModelObserverInvalidSignatureWarning.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
        {
            return;
        }

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

            // Determine the current state of the method
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration);
            var isPrivate = methodDeclaration.Modifiers.Any(SyntaxKind.PrivateKeyword);
            var isCurrentlyAsync = methodSymbol is not null &&
                (methodSymbol.ReturnType.Name == "Task" || methodSymbol.ReturnType.Name == "ValueTask" ||
                 methodSymbol.IsAsync);
            var hasParameters = methodDeclaration.ParameterList.Parameters.Count > 0;

            // Offer appropriate fixes based on current state
            if (!isPrivate)
            {
                // Primary issue: not private
                // Offer to make private and fix signature
                var makeSyncPrivate = CodeAction.Create(
                    title: $"Make private: private void {methodDeclaration.Identifier.Text}()",
                    createChangedDocument: c => MakePrivateSyncAsync(context.Document, root, methodDeclaration, c),
                    equivalenceKey: "MakePrivateSync");

                context.RegisterCodeFix(makeSyncPrivate, diagnostic);

                var makeAsyncPrivate = CodeAction.Create(
                    title: $"Make private async: private Task {methodDeclaration.Identifier.Text}(CancellationToken ct)",
                    createChangedDocument: c => MakePrivateAsyncAsync(context.Document, root, methodDeclaration, includeCancellationToken: true, c),
                    equivalenceKey: "MakePrivateAsync");

                context.RegisterCodeFix(makeAsyncPrivate, diagnostic);
            }
            else if (isCurrentlyAsync && hasParameters)
            {
                // Private async method but has wrong parameters
                var fixAsyncWithCt = CodeAction.Create(
                    title: $"Fix signature: private Task {methodDeclaration.Identifier.Text}(CancellationToken ct)",
                    createChangedDocument: c => MakePrivateAsyncAsync(context.Document, root, methodDeclaration, includeCancellationToken: true, c),
                    equivalenceKey: "FixAsyncWithCt");

                var fixAsyncWithoutCt = CodeAction.Create(
                    title: $"Fix signature: private Task {methodDeclaration.Identifier.Text}()",
                    createChangedDocument: c => MakePrivateAsyncAsync(context.Document, root, methodDeclaration, includeCancellationToken: false, c),
                    equivalenceKey: "FixAsyncWithoutCt");

                context.RegisterCodeFix(fixAsyncWithCt, diagnostic);
                context.RegisterCodeFix(fixAsyncWithoutCt, diagnostic);
            }
            else if (!isCurrentlyAsync && hasParameters)
            {
                // Private sync method but has parameters
                var fixSync = CodeAction.Create(
                    title: $"Fix signature: private void {methodDeclaration.Identifier.Text}()",
                    createChangedDocument: c => MakePrivateSyncAsync(context.Document, root, methodDeclaration, c),
                    equivalenceKey: "FixSync");

                var convertToAsync = CodeAction.Create(
                    title: $"Convert to async: private Task {methodDeclaration.Identifier.Text}(CancellationToken ct)",
                    createChangedDocument: c => MakePrivateAsyncAsync(context.Document, root, methodDeclaration, includeCancellationToken: true, c),
                    equivalenceKey: "ConvertToAsync");

                context.RegisterCodeFix(fixSync, diagnostic);
                context.RegisterCodeFix(convertToAsync, diagnostic);
            }
            else
            {
                // Other signature issues (e.g., wrong return type)
                var fixSync = CodeAction.Create(
                    title: $"Fix signature: private void {methodDeclaration.Identifier.Text}()",
                    createChangedDocument: c => MakePrivateSyncAsync(context.Document, root, methodDeclaration, c),
                    equivalenceKey: "FixSync");

                var fixAsync = CodeAction.Create(
                    title: $"Fix signature: private Task {methodDeclaration.Identifier.Text}()",
                    createChangedDocument: c => MakePrivateAsyncAsync(context.Document, root, methodDeclaration, includeCancellationToken: false, c),
                    equivalenceKey: "FixAsync");

                context.RegisterCodeFix(fixSync, diagnostic);
                context.RegisterCodeFix(fixAsync, diagnostic);
            }
        }
    }

    private static Task<Document> MakePrivateSyncAsync(
        Document document,
        SyntaxNode root,
        MethodDeclarationSyntax methodDeclaration,
        CancellationToken cancellationToken)
    {
        // Create empty parameter list - preserve closing paren trivia from original
        var parameterList = SyntaxFactory.ParameterList()
            .WithCloseParenToken(
                SyntaxFactory.Token(SyntaxKind.CloseParenToken)
                    .WithTrailingTrivia(methodDeclaration.ParameterList.CloseParenToken.TrailingTrivia));

        // Create void return type
        var voidReturnType = SyntaxFactory.PredefinedType(
            SyntaxFactory.Token(SyntaxKind.VoidKeyword));

        // Build modifiers: private only, remove async and other visibility modifiers
        var newModifiers = new List<SyntaxToken>();
        var hasPrivate = false;
        var leadingTrivia = methodDeclaration.Modifiers.Count > 0
            ? methodDeclaration.Modifiers.First().LeadingTrivia
            : methodDeclaration.ReturnType.GetLeadingTrivia();

        foreach (var modifier in methodDeclaration.Modifiers)
        {
            // Skip visibility modifiers and async
            if (modifier.IsKind(SyntaxKind.PublicKeyword) ||
                modifier.IsKind(SyntaxKind.ProtectedKeyword) ||
                modifier.IsKind(SyntaxKind.InternalKeyword) ||
                modifier.IsKind(SyntaxKind.PrivateKeyword) ||
                modifier.IsKind(SyntaxKind.AsyncKeyword))
            {
                if (modifier.IsKind(SyntaxKind.PrivateKeyword))
                {
                    hasPrivate = true;
                    newModifiers.Add(modifier.WithLeadingTrivia(leadingTrivia));
                }
                continue;
            }
            newModifiers.Add(modifier);
        }

        // Add private if not already present
        if (!hasPrivate)
        {
            newModifiers.Insert(0, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)
                .WithLeadingTrivia(leadingTrivia)
                .WithTrailingTrivia(SyntaxFactory.Space));
        }

        var newMethod = methodDeclaration
            .WithReturnType(voidReturnType.WithTrailingTrivia(SyntaxFactory.Space))
            .WithModifiers(SyntaxFactory.TokenList(newModifiers))
            .WithParameterList(parameterList);

        var newRoot = root.ReplaceNode(methodDeclaration, newMethod);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static Task<Document> MakePrivateAsyncAsync(
        Document document,
        SyntaxNode root,
        MethodDeclarationSyntax methodDeclaration,
        bool includeCancellationToken,
        CancellationToken cancellationToken)
    {
        // Preserve trailing trivia from original parameter list
        var originalCloseParenTrivia = methodDeclaration.ParameterList.CloseParenToken.TrailingTrivia;

        // Create parameter list
        var parameters = new List<ParameterSyntax>();

        if (includeCancellationToken)
        {
            var ctParameter = SyntaxFactory.Parameter(
                SyntaxFactory.Identifier("ct"))
                .WithType(SyntaxFactory.ParseTypeName("CancellationToken").WithTrailingTrivia(SyntaxFactory.Space));
            parameters.Add(ctParameter);
        }

        var parameterList = parameters.Count > 0
            ? SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters))
                .WithCloseParenToken(SyntaxFactory.Token(SyntaxKind.CloseParenToken).WithTrailingTrivia(originalCloseParenTrivia))
            : SyntaxFactory.ParameterList()
                .WithCloseParenToken(SyntaxFactory.Token(SyntaxKind.CloseParenToken).WithTrailingTrivia(originalCloseParenTrivia));

        // Create Task return type
        var taskReturnType = SyntaxFactory.ParseTypeName("Task");

        // Build modifiers: private and async, remove other visibility modifiers
        var newModifiers = new List<SyntaxToken>();
        var hasPrivate = false;
        var hasAsync = false;
        var leadingTrivia = methodDeclaration.Modifiers.Count > 0
            ? methodDeclaration.Modifiers.First().LeadingTrivia
            : methodDeclaration.ReturnType.GetLeadingTrivia();

        foreach (var modifier in methodDeclaration.Modifiers)
        {
            // Skip other visibility modifiers
            if (modifier.IsKind(SyntaxKind.PublicKeyword) ||
                modifier.IsKind(SyntaxKind.ProtectedKeyword) ||
                modifier.IsKind(SyntaxKind.InternalKeyword))
            {
                continue;
            }

            if (modifier.IsKind(SyntaxKind.PrivateKeyword))
            {
                hasPrivate = true;
                newModifiers.Add(modifier.WithLeadingTrivia(leadingTrivia));
            }
            else if (modifier.IsKind(SyntaxKind.AsyncKeyword))
            {
                hasAsync = true;
                newModifiers.Add(modifier);
            }
            else
            {
                newModifiers.Add(modifier);
            }
        }

        // Add private if not present
        if (!hasPrivate)
        {
            newModifiers.Insert(0, SyntaxFactory.Token(SyntaxKind.PrivateKeyword)
                .WithLeadingTrivia(leadingTrivia)
                .WithTrailingTrivia(SyntaxFactory.Space));
        }

        // Add async if not present
        if (!hasAsync)
        {
            var insertIndex = newModifiers.Count > 0 ? 1 : 0;
            newModifiers.Insert(insertIndex, SyntaxFactory.Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(SyntaxFactory.Space));
        }

        var newMethod = methodDeclaration
            .WithReturnType(taskReturnType.WithTrailingTrivia(SyntaxFactory.Space))
            .WithModifiers(SyntaxFactory.TokenList(newModifiers))
            .WithParameterList(parameterList);

        var newRoot = root.ReplaceNode(methodDeclaration, newMethod);

        // Add using directives
        if (includeCancellationToken)
        {
            newRoot = SyntaxHelpers.AddUsingDirectives(newRoot, "System.Threading");
        }
        newRoot = SyntaxHelpers.AddUsingDirectives(newRoot, "System.Threading.Tasks");

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}

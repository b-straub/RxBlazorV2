using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Diagnostics;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RxBlazorV2CodeFix.CodeFix;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MissingObservableModelScopeCodeFixProvider))]
[Shared]
public class MissingObservableModelScopeCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.MissingObservableModelScopeWarning.Id];

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics.Where(d => FixableDiagnosticIds.Contains(d.Id)))
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var classDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
                .OfType<ClassDeclarationSyntax>().FirstOrDefault();

            if (classDeclaration is not null)
            {
                var className = classDeclaration.Identifier.Text;

                // Code fix 1: Add Scoped scope attribute (most common for page-specific models)
                var addScopedAction = CodeAction.Create(
                    title: $"Add [ObservableModelScope(ModelScope.Scoped)] to {className}",
                    createChangedDocument: c => AddScopeAttribute(context.Document, root, classDeclaration, "Scoped", c),
                    equivalenceKey: "AddScopedScope");

                context.RegisterCodeFix(addScopedAction, diagnostic);

                // Code fix 2: Add Singleton scope attribute
                var addSingletonAction = CodeAction.Create(
                    title: $"Add [ObservableModelScope(ModelScope.Singleton)] to {className}",
                    createChangedDocument: c => AddScopeAttribute(context.Document, root, classDeclaration, "Singleton", c),
                    equivalenceKey: "AddSingletonScope");

                context.RegisterCodeFix(addSingletonAction, diagnostic);

                // Code fix 3: Add Transient scope attribute
                var addTransientAction = CodeAction.Create(
                    title: $"Add [ObservableModelScope(ModelScope.Transient)] to {className}",
                    createChangedDocument: c => AddScopeAttribute(context.Document, root, classDeclaration, "Transient", c),
                    equivalenceKey: "AddTransientScope");

                context.RegisterCodeFix(addTransientAction, diagnostic);
            }
        }
    }

    private static Task<Document> AddScopeAttribute(
        Document document,
        SyntaxNode root,
        ClassDeclarationSyntax classDeclaration,
        string scopeValue,
        CancellationToken cancellationToken)
    {
        // Create the attribute with the specified scope
        var newAttribute = SyntaxFactory.Attribute(
            SyntaxFactory.IdentifierName("ObservableModelScope"))
            .WithArgumentList(
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName("ModelScope"),
                                SyntaxFactory.IdentifierName(scopeValue))))));

        // Use the helper method to add the attribute with proper trivia handling
        var newClassDeclaration = SyntaxHelpers.AddAttributePreservingTrivia(
            classDeclaration,
            newAttribute);

        // Add using statement if needed
        var newRoot = SyntaxHelpers.AddUsingDirectives(root, "RxBlazorV2.Model");
        newRoot = newRoot.ReplaceNode(
            newRoot.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .First(c => c.Identifier.Text == classDeclaration.Identifier.Text),
            newClassDeclaration);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}

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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ObservableEntityMissingPartialCodeFixProvider))]
[Shared]
public class ObservableEntityMissingPartialCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.ObservableEntityMissingPartialModifierError.Id];

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
            var token = root.FindToken(diagnosticSpan.Start);
            var node = token.Parent?.AncestorsAndSelf().FirstOrDefault(n =>
                n is ClassDeclarationSyntax || n is PropertyDeclarationSyntax);

            if (node is ClassDeclarationSyntax classDeclaration)
            {
                var action = CodeAction.Create(
                    title: "Add 'partial' modifier to class",
                    createChangedDocument: c => AddPartialModifierToClass(context.Document, root, classDeclaration, c),
                    equivalenceKey: "AddPartialModifierToClass");

                context.RegisterCodeFix(action, diagnostic);
            }
            else if (node is PropertyDeclarationSyntax propertyDeclaration)
            {
                var action = CodeAction.Create(
                    title: "Add 'partial' modifier to property",
                    createChangedDocument: c => AddPartialModifierToProperty(context.Document, root, propertyDeclaration, c),
                    equivalenceKey: "AddPartialModifierToProperty");

                context.RegisterCodeFix(action, diagnostic);
            }
        }
    }

    private static Task<Document> AddPartialModifierToClass(
        Document document,
        SyntaxNode root,
        ClassDeclarationSyntax classDeclaration,
        CancellationToken cancellationToken)
    {
        // Find the first non-trivia modifier or the class keyword
        var firstModifier = classDeclaration.Modifiers.FirstOrDefault();

        SyntaxToken partialKeyword;
        SyntaxTokenList newModifiers;

        if (firstModifier != default)
        {
            // Create partial keyword with trivia from the first modifier
            partialKeyword = SyntaxFactory.Token(SyntaxKind.PartialKeyword)
                .WithTrailingTrivia(SyntaxFactory.Space);

            // Insert partial at the end of modifiers (right before class keyword)
            // C# syntax: [attributes] [access] [sealed|abstract|static] [partial] [class]
            newModifiers = classDeclaration.Modifiers.Add(partialKeyword);
        }
        else
        {
            // No modifiers, create partial keyword with space after
            partialKeyword = SyntaxFactory.Token(SyntaxKind.PartialKeyword)
                .WithTrailingTrivia(SyntaxFactory.Space);
            newModifiers = SyntaxFactory.TokenList(partialKeyword);
        }

        var newClassDeclaration = classDeclaration.WithModifiers(newModifiers);
        var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static Task<Document> AddPartialModifierToProperty(
        Document document,
        SyntaxNode root,
        PropertyDeclarationSyntax propertyDeclaration,
        CancellationToken cancellationToken)
    {
        // Find the first non-trivia modifier or the property type
        var firstModifier = propertyDeclaration.Modifiers.FirstOrDefault();

        SyntaxToken partialKeyword;
        SyntaxTokenList newModifiers;

        if (firstModifier != default)
        {
            // Create partial keyword with space after
            partialKeyword = SyntaxFactory.Token(SyntaxKind.PartialKeyword)
                .WithTrailingTrivia(SyntaxFactory.Space);

            // Insert partial at the end of modifiers (right before property type)
            // C# syntax: [attributes] [access] [static] [partial] [type]
            newModifiers = propertyDeclaration.Modifiers.Add(partialKeyword);
        }
        else
        {
            // No modifiers, create partial keyword with space after
            partialKeyword = SyntaxFactory.Token(SyntaxKind.PartialKeyword)
                .WithTrailingTrivia(SyntaxFactory.Space);
            newModifiers = SyntaxFactory.TokenList(partialKeyword);
        }

        var newPropertyDeclaration = propertyDeclaration.WithModifiers(newModifiers);
        var newRoot = root.ReplaceNode(propertyDeclaration, newPropertyDeclaration);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}

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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NonPublicPartialConstructorCodeFixProvider))]
[Shared]
public class NonPublicPartialConstructorCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.NonPublicPartialConstructorError.Id];

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
            var constructorDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
                .OfType<ConstructorDeclarationSyntax>().FirstOrDefault();

            if (constructorDeclaration is not null)
            {
                var className = constructorDeclaration.Identifier.Text;

                // Code fix: Change constructor to public
                var changeToPublicAction = CodeAction.Create(
                    title: $"Change partial constructor to 'public'",
                    createChangedDocument: c => ChangeConstructorToPublic(context.Document, root, constructorDeclaration, c),
                    equivalenceKey: "ChangeToPublicConstructor");

                context.RegisterCodeFix(changeToPublicAction, diagnostic);
            }
        }
    }

    private static Task<Document> ChangeConstructorToPublic(
        Document document,
        SyntaxNode root,
        ConstructorDeclarationSyntax constructor,
        CancellationToken cancellationToken)
    {
        // Find the first accessibility modifier (protected, private, internal)
        var firstAccessibilityModifier = constructor.Modifiers
            .FirstOrDefault(m =>
                m.IsKind(SyntaxKind.ProtectedKeyword) ||
                m.IsKind(SyntaxKind.PrivateKeyword) ||
                m.IsKind(SyntaxKind.InternalKeyword));

        if (firstAccessibilityModifier == default)
        {
            return Task.FromResult(document);
        }

        // Create public keyword with trivia from the first accessibility modifier
        var publicKeyword = SyntaxFactory.Token(SyntaxKind.PublicKeyword)
            .WithTriviaFrom(firstAccessibilityModifier);

        // Remove all accessibility modifiers (protected, private, internal) and keep non-accessibility modifiers
        var newModifiers = SyntaxFactory.TokenList(
            constructor.Modifiers
                .Where(m =>
                    !m.IsKind(SyntaxKind.ProtectedKeyword) &&
                    !m.IsKind(SyntaxKind.PrivateKeyword) &&
                    !m.IsKind(SyntaxKind.InternalKeyword)));

        // Insert public keyword at the beginning
        newModifiers = newModifiers.Insert(0, publicKeyword);

        // Create new constructor with updated modifiers
        var newConstructor = constructor.WithModifiers(newModifiers);

        // Replace the constructor in the syntax tree
        var newRoot = root.ReplaceNode(constructor, newConstructor);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}

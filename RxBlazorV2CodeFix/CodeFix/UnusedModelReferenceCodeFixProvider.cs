using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Diagnostics;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace RxBlazorV2CodeFix.CodeFix;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnusedModelReferenceCodeFixProvider))]
public class UnusedModelReferenceCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.UnusedModelReferenceError.Id];

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Id != DiagnosticDescriptors.UnusedModelReferenceError.Id)
            {
                continue;
            }

            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan);

            // Find the attribute that contains the unused reference
            var attribute = node.FirstAncestorOrSelf<AttributeSyntax>();
            if (attribute == null)
            {
                continue;
            }

            // Code Fix: Remove the unused model reference attribute
            var removeAttributeAction = CodeAction.Create(
                title: "Remove unused model reference attribute",
                createChangedDocument: c => RemoveAttributeAsync(context.Document, root, attribute, c),
                equivalenceKey: "RemoveUnusedModelReference");

            context.RegisterCodeFix(removeAttributeAction, diagnostic);
        }
    }

    private static Task<Document> RemoveAttributeAsync(
        Document document,
        SyntaxNode root,
        AttributeSyntax attribute,
        CancellationToken cancellationToken)
    {
        var newRoot = SyntaxHelpers.RemoveAttributeFromClass(root, attribute);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}

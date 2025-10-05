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
using RxBlazorV2Generator.Diagnostics;

namespace RxBlazorV2CodeFix.CodeFix;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CircularModelReferenceCodeFixProvider)), Shared]
public class CircularModelReferenceCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.CircularModelReferenceError.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the attribute syntax node
        var attribute = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<AttributeSyntax>().First();
        if (attribute is null)
        {
            return;
        }

        // Register a code fix to remove the circular reference attribute
        var removeAction = CodeAction.Create(
            title: "Remove circular model reference",
            createChangedDocument: c => RemoveAttributeAsync(context.Document, root, attribute, c),
            equivalenceKey: "RemoveCircularReference");

        context.RegisterCodeFix(removeAction, diagnostic);
    }

    private static Task<Document> RemoveAttributeAsync(
        Document document,
        SyntaxNode root,
        AttributeSyntax attribute,
        CancellationToken cancellationToken)
    {
        // Find the attribute list containing this attribute
        var attributeList = attribute.Parent as AttributeListSyntax;
        if (attributeList is null)
        {
            return Task.FromResult(document);
        }

        SyntaxNode? newRoot;
        if (attributeList.Attributes.Count == 1)
        {
            // If this is the only attribute in the list, remove the entire attribute list
            newRoot = root.RemoveNode(attributeList, SyntaxRemoveOptions.KeepNoTrivia);
            if (newRoot is null)
            {
                return Task.FromResult(document);
            }
        }
        else
        {
            // If there are multiple attributes in the list, remove only this attribute
            var newAttributeList = attributeList.RemoveNode(attribute, SyntaxRemoveOptions.KeepNoTrivia);
            if (newAttributeList is null)
            {
                return Task.FromResult(document);
            }
            var replacedRoot = root.ReplaceNode(attributeList, newAttributeList);
            if (replacedRoot is null)
            {
                return Task.FromResult(document);
            }
            newRoot = replacedRoot;
        }

        if (newRoot is null)
        {
            return Task.FromResult(document);
        }

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}

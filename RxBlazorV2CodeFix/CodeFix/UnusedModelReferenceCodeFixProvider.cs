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
        var attributeList = attribute.Parent as AttributeListSyntax;
        if (attributeList == null)
        {
            return Task.FromResult(document);
        }

        SyntaxNode newRoot;

        if (attributeList.Attributes.Count == 1)
        {
            // Remove the entire attribute list if this is the only attribute
            if (attributeList.Parent is ClassDeclarationSyntax classDecl)
            {
                var newAttributeLists = classDecl.AttributeLists.RemoveKeepTrivia(attributeList);
                var newClassDecl = classDecl.WithAttributeLists(newAttributeLists);
                newRoot = root.ReplaceNode(classDecl, newClassDecl);
            }
            else
            {
                // For other node types, just return the original document
                newRoot = root;
            }
        }
        else
        {
            // Remove just this attribute from the list
            var newAttributes = attributeList.Attributes.Remove(attribute);
            var newAttributeList = attributeList.WithAttributes(newAttributes);
            newRoot = root.ReplaceNode(attributeList, newAttributeList);
        }

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}

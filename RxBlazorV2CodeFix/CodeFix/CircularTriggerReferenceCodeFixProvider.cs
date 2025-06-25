using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RxBlazorV2CodeFix.CodeFix;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CircularTriggerReferenceCodeFixProvider))]
public class CircularTriggerReferenceCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.CircularTriggerReferenceError.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        foreach (var diagnostic in context.Diagnostics.Where(d => FixableDiagnosticIds.Contains(d.Id)))
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var attributeNode = root.FindNode(diagnosticSpan);
            var attribute = attributeNode.FirstAncestorOrSelf<AttributeSyntax>();
            
            if (attribute is not null)
            {
                // Code fix: Remove the circular trigger attribute
                var removeTriggerAction = CodeAction.Create(
                    title: "Remove circular trigger reference",
                    createChangedDocument: c => Task.FromResult(RemoveCircularTrigger(context.Document, root, attribute)),
                    equivalenceKey: "RemoveCircularTrigger");

                context.RegisterCodeFix(removeTriggerAction, diagnostic);
            }
        }
    }

    private static Document RemoveCircularTrigger(Document document, SyntaxNode root, AttributeSyntax circularTriggerAttribute)
    {
        var attributeList = circularTriggerAttribute.Parent as AttributeListSyntax;
        if (attributeList is null) return document;

        SyntaxNode newRoot;
        if (attributeList.Attributes.Count == 1)
        {
            // Remove the entire attribute list if it's the only attribute
            var tempRoot = root.RemoveNode(attributeList, Microsoft.CodeAnalysis.SyntaxRemoveOptions.KeepNoTrivia) ?? 
                throw new InvalidOperationException("Failed to remove attribute list from syntax tree");
            newRoot = tempRoot;
        }
        else
        {
            // Remove just this attribute from the list
            var newAttributeList = attributeList.RemoveNode(circularTriggerAttribute, Microsoft.CodeAnalysis.SyntaxRemoveOptions.KeepNoTrivia) ?? 
                throw new InvalidOperationException("Failed to remove attribute from attribute list");
            var tempRoot = root.ReplaceNode(attributeList, newAttributeList) ?? 
                throw new InvalidOperationException("Failed to replace attribute list in syntax tree");
            newRoot = tempRoot;
        }

        return document.WithSyntaxRoot(newRoot);
    }
}
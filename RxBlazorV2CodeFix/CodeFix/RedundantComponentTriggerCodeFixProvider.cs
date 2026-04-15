using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Diagnostics;
using RxBlazorV2Generator.Extensions;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace RxBlazorV2CodeFix.CodeFix;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RedundantComponentTriggerCodeFixProvider))]
[Shared]
public class RedundantComponentTriggerCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.RedundantComponentTriggerError.Id];

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

            // Find the attribute that triggered the diagnostic
            var attribute = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
                .OfType<AttributeSyntax>().FirstOrDefault();

            if (attribute is null)
            {
                continue;
            }

            // Find the property
            var property = attribute.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            if (property is null)
            {
                continue;
            }

            // Code fix: Remove trigger attributes from the property
            var removeTriggerAttributesAction = CodeAction.Create(
                title: diagnostic.Descriptor.CodeFixMessage(),
                createChangedDocument: c => RemoveTriggerAttributes(
                    context.Document, root, property, c),
                equivalenceKey: diagnostic.Descriptor.Id);

            context.RegisterCodeFix(removeTriggerAttributesAction, diagnostic);
        }
    }

    private static Task<Document> RemoveTriggerAttributes(
        Document document,
        SyntaxNode root,
        PropertyDeclarationSyntax property,
        CancellationToken cancellationToken)
    {
        // Build new property with trigger attributes removed
        var newAttributeLists = new List<AttributeListSyntax>();

        foreach (var attributeList in property.AttributeLists)
        {
            var nonTriggerAttributes = attributeList.Attributes
                .Where(attr =>
                {
                    var name = attr.Name.ToString();
                    return !name.Contains("ObservableComponentTrigger") &&
                           !name.Contains("ObservableComponentTriggerAsync");
                })
                .ToList();

            // If there are non-trigger attributes in this list, keep them
            if (nonTriggerAttributes.Count > 0)
            {
                var newAttributes = SyntaxFactory.SeparatedList(nonTriggerAttributes);
                var newAttributeList = attributeList.WithAttributes(newAttributes);
                newAttributeLists.Add(newAttributeList);
            }
            // Otherwise, skip this entire attribute list (it only had trigger attributes)
        }

        // Create new property with updated attribute lists
        var newProperty = property.WithAttributeLists(
            SyntaxFactory.List(newAttributeLists));

        // Replace in root
        var newRoot = root.ReplaceNode(property, newProperty);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}

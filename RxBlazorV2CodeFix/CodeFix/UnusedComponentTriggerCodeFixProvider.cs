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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnusedComponentTriggerCodeFixProvider))]
[Shared]
public class UnusedComponentTriggerCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.UnusedObservableComponentTriggerWarning.Id];

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

            // Find the property and class
            var property = attribute.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            var classDeclaration = attribute.FirstAncestorOrSelf<ClassDeclarationSyntax>();

            if (property is null || classDeclaration is null)
            {
                continue;
            }

            var className = classDeclaration.Identifier.Text;
            var propertyName = property.Identifier.Text;

            // Code fix 1: Add [ObservableComponent] attribute to the class
            var addObservableComponentAction = CodeAction.Create(
                title: diagnostic.Descriptor.CodeFixMessage(),
                createChangedDocument: c => AddObservableComponentAttribute(
                    context.Document, root, classDeclaration, c),
                equivalenceKey: diagnostic.Descriptor.Id);

            context.RegisterCodeFix(addObservableComponentAction, diagnostic);

            // Code fix 2: Remove trigger attributes from the property
            var removeTriggerAttributesAction = CodeAction.Create(
                title: diagnostic.Descriptor.CodeFixMessage(1),
                createChangedDocument: c => RemoveTriggerAttributes(
                    context.Document, root, property, c),
                equivalenceKey: $"{diagnostic.Descriptor.Id}_RemoveTriggers");

            context.RegisterCodeFix(removeTriggerAttributesAction, diagnostic);
        }
    }

    private static Task<Document> AddObservableComponentAttribute(
        Document document,
        SyntaxNode root,
        ClassDeclarationSyntax classDeclaration,
        CancellationToken cancellationToken)
    {
        // Check if class already has [ObservableComponent] attribute
        var hasObservableComponent = classDeclaration.AttributeLists
            .SelectMany(list => list.Attributes)
            .Any(attr => attr.Name.ToString().Contains("ObservableComponent"));

        if (hasObservableComponent)
        {
            // Already has the attribute, nothing to do
            return Task.FromResult(document);
        }

        // Create the [ObservableComponent] attribute
        var newAttribute = SyntaxFactory.Attribute(
            SyntaxFactory.IdentifierName("ObservableComponent"));

        // Use helper to add attribute with proper trivia handling
        // (handles both cases: with and without existing attributes)
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

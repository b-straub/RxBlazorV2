using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Diagnostics;
using RxBlazorV2Generator.Extensions;
using RxBlazorV2Generator.Helpers;

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

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        semanticModel.ThrowIfNull();
        
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the attribute syntax node
        var attribute = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<AttributeSyntax>().First();
        if (attribute is null)
        {
            return;
        }

        // Find the class this attribute is on
        var classDeclaration = attribute.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (classDeclaration is null)
        {
            return;
        }

        // Register fix to remove this model's circular reference
        var removeSingleAction = CodeAction.Create(
            title: diagnostic.Descriptor.CodeFixMessage(),
            createChangedDocument: c => RemoveAttributeAsync(context.Document, root, attribute, c),
            equivalenceKey: diagnostic.Descriptor.Id);

        context.RegisterCodeFix(removeSingleAction, diagnostic);

        // Try to find the partner attribute in the circular reference
        var partnerAttribute = FindCircularReferencePartner(root, semanticModel, classDeclaration, attribute);
        if (partnerAttribute is not null)
        {
            // Register fix to remove both circular references
            var removeBothAction = CodeAction.Create(
                title: diagnostic.Descriptor.CodeFixMessage(1),
                createChangedDocument: c => RemoveBothAttributesAsync(context.Document, root, attribute, partnerAttribute, c),
                equivalenceKey: $"{diagnostic.Descriptor.Id}_RemoveAll");

            context.RegisterCodeFix(removeBothAction, diagnostic);
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

    private static AttributeSyntax? FindCircularReferencePartner(
        SyntaxNode root,
        SemanticModel semanticModel,
        ClassDeclarationSyntax currentClass,
        AttributeSyntax currentAttribute)
    {
        // Get the type referenced by the current attribute
        var referencedType = GetReferencedType(currentAttribute, semanticModel);
        if (referencedType is null)
        {
            return null;
        }

        // Get the current class type
        var currentClassSymbol = semanticModel.GetDeclaredSymbol(currentClass);
        if (currentClassSymbol is null)
        {
            return null;
        }

        // Find all classes in the same syntax tree
        var allClasses = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

        foreach (var otherClass in allClasses)
        {
            if (otherClass == currentClass)
            {
                continue;
            }

            var otherClassSymbol = semanticModel.GetDeclaredSymbol(otherClass);
            if (otherClassSymbol is null)
            {
                continue;
            }

            // Check if this is the referenced type
            if (!SymbolEqualityComparer.Default.Equals(otherClassSymbol.OriginalDefinition, referencedType.OriginalDefinition))
            {
                continue;
            }

            // Look for ObservableModelReference attributes on this class
            foreach (var attributeList in otherClass.AttributeLists)
            {
                foreach (var attr in attributeList.Attributes)
                {
                    var attrName = attr.Name.ToString();
                    if (!attrName.StartsWith("ObservableModelReference"))
                    {
                        continue;
                    }

                    // Check if this attribute references back to the current class
                    var attrReferencedType = GetReferencedType(attr, semanticModel);
                    if (attrReferencedType is not null &&
                        SymbolEqualityComparer.Default.Equals(attrReferencedType.OriginalDefinition, currentClassSymbol.OriginalDefinition))
                    {
                        return attr;
                    }
                }
            }
        }

        return null;
    }

    private static INamedTypeSymbol? GetReferencedType(AttributeSyntax attribute, SemanticModel semanticModel)
    {
        return SyntaxHelpers.ExtractTypeFromAttribute(attribute, semanticModel);
    }

    private static Task<Document> RemoveBothAttributesAsync(
        Document document,
        SyntaxNode root,
        AttributeSyntax attribute1,
        AttributeSyntax attribute2,
        CancellationToken cancellationToken)
    {
        var nodesToRemove = new List<SyntaxNode>();

        // Process first attribute
        var attributeList1 = attribute1.Parent as AttributeListSyntax;
        if (attributeList1 is not null)
        {
            if (attributeList1.Attributes.Count == 1)
            {
                nodesToRemove.Add(attributeList1);
            }
            else
            {
                nodesToRemove.Add(attribute1);
            }
        }

        // Process second attribute
        var attributeList2 = attribute2.Parent as AttributeListSyntax;
        if (attributeList2 is not null)
        {
            if (attributeList2.Attributes.Count == 1)
            {
                nodesToRemove.Add(attributeList2);
            }
            else
            {
                nodesToRemove.Add(attribute2);
            }
        }

        // Remove all nodes at once
        var newRoot = root.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia);
        if (newRoot is null)
        {
            return Task.FromResult(document);
        }

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}

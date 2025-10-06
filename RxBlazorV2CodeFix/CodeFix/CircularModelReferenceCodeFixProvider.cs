using System.Collections.Generic;
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
using RxBlazorV2Generator.Extensions;

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
        if (semanticModel is null)
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

        // Find the class this attribute is on
        var classDeclaration = attribute.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (classDeclaration is null)
        {
            return;
        }

        // Register fix to remove this model's circular reference
        var removeSingleAction = CodeAction.Create(
            title: diagnostic.Descriptor.CodeFixMessage(0),
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
        // Try generic syntax first: ObservableModelReference<T>
        if (attribute.Name is GenericNameSyntax genericName && genericName.TypeArgumentList?.Arguments.Count > 0)
        {
            var typeArgument = genericName.TypeArgumentList.Arguments.First();
            var typeInfo = semanticModel.GetTypeInfo(typeArgument);
            return typeInfo.Type as INamedTypeSymbol;
        }

        // Try typeof() syntax: ObservableModelReference(typeof(T))
        if (attribute.ArgumentList?.Arguments.Count > 0)
        {
            var firstArgument = attribute.ArgumentList.Arguments.First();
            if (firstArgument.Expression is TypeOfExpressionSyntax typeOfExpression)
            {
                var typeInfo = semanticModel.GetTypeInfo(typeOfExpression.Type);
                if (typeInfo.Type is INamedTypeSymbol namedType)
                {
                    // For constructed generic types, get the generic definition
                    return namedType.IsGenericType && namedType.TypeArguments.Length > 0
                        ? namedType.ConstructedFrom
                        : namedType;
                }
            }
        }

        return null;
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

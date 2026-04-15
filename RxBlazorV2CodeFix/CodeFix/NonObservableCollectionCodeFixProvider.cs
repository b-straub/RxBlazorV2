using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Diagnostics;
using RxBlazorV2Generator.Extensions;
using RxBlazorV2Generator.Helpers;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace RxBlazorV2CodeFix.CodeFix;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NonObservableCollectionCodeFixProvider))]
[Shared]
public class NonObservableCollectionCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.NonObservableCollectionPropertyError.Id];

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

        foreach (var diagnostic in context.Diagnostics.Where(d => FixableDiagnosticIds.Contains(d.Id)))
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan);

            var property = node.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            if (property is null)
            {
                continue;
            }

            var propertySymbol = semanticModel.GetDeclaredSymbol(property) as IPropertySymbol;
            if (propertySymbol is null)
            {
                continue;
            }

            var replacementType = GetObservableReplacementType(propertySymbol.Type);
            if (replacementType is null)
            {
                continue;
            }

            var action = CodeAction.Create(
                title: diagnostic.Descriptor.CodeFixMessage(),
                createChangedDocument: c => ReplaceWithObservableCollection(
                    context.Document, root, property, replacementType, c),
                equivalenceKey: diagnostic.Descriptor.Id);

            context.RegisterCodeFix(action, diagnostic);
        }
    }

    /// <summary>
    /// Determines the correct ObservableCollections replacement type for a given collection type.
    /// </summary>
    private static string? GetObservableReplacementType(ITypeSymbol typeSymbol)
    {
        var typeName = typeSymbol.Name;
        var originalDef = typeSymbol.OriginalDefinition;

        // Check for Dictionary<K,V> or IDictionary<K,V>
        if (typeName is "Dictionary" or "IDictionary" &&
            originalDef is INamedTypeSymbol { TypeArguments.Length: 2 } dictType)
        {
            var keyType = dictType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            var valueType = dictType.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            return $"ObservableDictionary<{keyType}, {valueType}>";
        }

        // Check for HashSet<T> or ISet<T>
        if (typeName is "HashSet" or "ISet")
        {
            foreach (var iface in typeSymbol.AllInterfaces)
            {
                if (iface.Name == "ICollection" && iface.IsGenericType &&
                    iface.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic")
                {
                    var elementType = iface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                    return $"ObservableHashSet<{elementType}>";
                }
            }
        }

        // Default: List<T>, IList<T>, Collection<T>, ICollection<T>, etc. → ObservableList<T>
        foreach (var iface in typeSymbol.AllInterfaces)
        {
            if (iface.Name == "ICollection" && iface.IsGenericType &&
                iface.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic")
            {
                var elementType = iface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                return $"ObservableList<{elementType}>";
            }
        }

        return null;
    }

    private static Task<Document> ReplaceWithObservableCollection(
        Document document,
        SyntaxNode root,
        PropertyDeclarationSyntax property,
        string replacementType,
        CancellationToken cancellationToken)
    {
        // Replace the type
        var newType = SyntaxFactory.ParseTypeName(replacementType)
            .WithTriviaFrom(property.Type);

        var newProperty = property.WithType(newType);

        // Convert set → private init
        if (newProperty.AccessorList is not null)
        {
            var newAccessors = newProperty.AccessorList.Accessors.Select(accessor =>
            {
                if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
                {
                    var initKeyword = SyntaxFactory.Token(SyntaxKind.InitKeyword)
                        .WithTriviaFrom(accessor.Keyword);

                    // Add private modifier, preserving existing trivia from the keyword
                    var privateKeyword = SyntaxFactory.Token(SyntaxKind.PrivateKeyword)
                        .WithLeadingTrivia(accessor.Keyword.LeadingTrivia);
                    var initKeywordWithTrivia = initKeyword
                        .WithLeadingTrivia(SyntaxFactory.Space);

                    var modifiers = SyntaxFactory.TokenList(privateKeyword);

                    return SyntaxFactory.AccessorDeclaration(
                        SyntaxKind.InitAccessorDeclaration,
                        accessor.AttributeLists,
                        modifiers,
                        initKeywordWithTrivia,
                        accessor.Body,
                        accessor.ExpressionBody,
                        accessor.SemicolonToken);
                }
                return accessor;
            });

            var newAccessorList = newProperty.AccessorList.WithAccessors(
                SyntaxFactory.List(newAccessors));

            newProperty = newProperty.WithAccessorList(newAccessorList);
        }

        // Replace in root and add using directive
        var newRoot = root.ReplaceNode(property, newProperty);
        newRoot = SyntaxHelpers.AddUsingDirectives(newRoot, "ObservableCollections");

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}

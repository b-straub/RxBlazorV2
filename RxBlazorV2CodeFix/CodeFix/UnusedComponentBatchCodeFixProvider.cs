using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Diagnostics;
using RxBlazorV2Generator.Extensions;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

namespace RxBlazorV2CodeFix.CodeFix;

/// <summary>
/// Fixes RXBG044 - an [ObservableComponentBatchAsync] batch on a model that has no
/// [ObservableComponent], so the hook it would generate never exists.
/// Offers the two coherent outcomes: give the model a component, or remove the attribute.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnusedComponentBatchCodeFixProvider))]
[Shared]
public class UnusedComponentBatchCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.UnusedComponentBatchWarning.Id];

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
            // The diagnostic is reported on the [ObservableBatch] attribute application itself.
            var attribute = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent?.AncestorsAndSelf()
                .OfType<AttributeSyntax>().FirstOrDefault();

            var classDeclaration = attribute?.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (attribute is null || classDeclaration is null)
            {
                continue;
            }

            var batchId = GetBatchId(attribute);
            if (batchId is null)
            {
                continue;
            }

            var addObservableComponentAction = CodeAction.Create(
                title: diagnostic.Descriptor.CodeFixMessage(),
                createChangedDocument: _ => Task.FromResult(AddObservableComponentAttribute(
                    context.Document, root, classDeclaration)),
                equivalenceKey: diagnostic.Descriptor.Id);

            context.RegisterCodeFix(addObservableComponentAction, diagnostic);

            var removeAttributeAction = CodeAction.Create(
                title: diagnostic.Descriptor.CodeFixMessage(1),
                createChangedDocument: _ => Task.FromResult(RemoveBatchAttributes(
                    context.Document, root, classDeclaration, batchId)),
                equivalenceKey: $"{diagnostic.Descriptor.Id}_RemoveAttribute");

            context.RegisterCodeFix(removeAttributeAction, diagnostic);
        }
    }

    /// <summary>
    /// Reads the batch id literal from an <c>[ObservableComponentBatchAsync("id", ...)]</c> application.
    /// </summary>
    private static string? GetBatchId(AttributeSyntax attribute)
    {
        var firstArgument = attribute.ArgumentList?.Arguments.FirstOrDefault();
        if (firstArgument?.Expression is LiteralExpressionSyntax literal &&
            literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return literal.Token.ValueText;
        }

        return null;
    }

    private static Document AddObservableComponentAttribute(
        Document document,
        SyntaxNode root,
        ClassDeclarationSyntax classDeclaration)
    {
        var hasObservableComponent = classDeclaration.AttributeLists
            .SelectMany(list => list.Attributes)
            .Any(attr => attr.Name.ToString().Contains("ObservableComponent"));

        if (hasObservableComponent)
        {
            return document;
        }

        var newAttribute = SyntaxFactory.Attribute(
            SyntaxFactory.IdentifierName("ObservableComponent"));

        var newClassDeclaration = SyntaxHelpers.AddAttributePreservingTrivia(
            classDeclaration,
            newAttribute);

        var newRoot = SyntaxHelpers.AddUsingDirectives(root, "RxBlazorV2.Model");
        newRoot = newRoot.ReplaceNode(
            newRoot.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .First(c => c.Identifier.Text == classDeclaration.Identifier.Text),
            newClassDeclaration);

        return document.WithSyntaxRoot(newRoot);
    }

    /// <summary>
    /// Removes every <c>[ObservableComponentBatchAsync("&lt;batchId&gt;", ...)]</c> in the class, taking
    /// the whole attribute list with it when nothing else is left on it so no empty <c>[]</c> remains.
    /// All members go together because a batch only means anything as a group.
    /// </summary>
    private static Document RemoveBatchAttributes(
        Document document,
        SyntaxNode root,
        ClassDeclarationSyntax classDeclaration,
        string batchId)
    {
        var properties = classDeclaration.DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .Where(property => property.AttributeLists
                .SelectMany(list => list.Attributes)
                .Any(attr => IsBatchAttribute(attr, batchId)))
            .ToList();

        if (properties.Count == 0)
        {
            return document;
        }

        var newRoot = root.ReplaceNodes(
            properties,
            (original, _) =>
            {
                var keptLists = new List<AttributeListSyntax>();

                foreach (var attributeList in original.AttributeLists)
                {
                    var kept = attributeList.Attributes
                        .Where(attr => !IsBatchAttribute(attr, batchId))
                        .ToList();

                    if (kept.Count == attributeList.Attributes.Count)
                    {
                        keptLists.Add(attributeList);
                        continue;
                    }

                    // Drop the list entirely when the batch attribute was its only entry.
                    if (kept.Count > 0)
                    {
                        keptLists.Add(attributeList.WithAttributes(SyntaxFactory.SeparatedList(kept)));
                    }
                }

                return original
                    .WithAttributeLists(SyntaxFactory.List(keptLists))
                    .WithTriviaFrom(original);
            });

        return document.WithSyntaxRoot(newRoot);
    }

    private static bool IsBatchAttribute(AttributeSyntax attribute, string batchId)
    {
        return attribute.Name.ToString().Contains("ObservableComponentBatchAsync") &&
               GetBatchId(attribute) == batchId;
    }
}

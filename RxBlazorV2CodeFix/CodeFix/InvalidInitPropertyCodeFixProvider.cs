using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Diagnostics;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace RxBlazorV2CodeFix.CodeFix;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InvalidInitPropertyCodeFixProvider))]
public class InvalidInitPropertyCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.InvalidInitPropertyError.Id];

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
            if (diagnostic.Id != DiagnosticDescriptors.InvalidInitPropertyError.Id)
            {
                continue;
            }

            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan);

            // Find the property declaration that contains the invalid pattern
            var property = node.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            if (property == null)
            {
                continue;
            }

            // Code Fix: Convert init to set (preserve required modifier)
            var convertToSetAction = CodeAction.Create(
                title: "Convert 'init' to 'set'",
                createChangedDocument: c => ConvertInitToSetAsync(context.Document, root, property, c),
                equivalenceKey: "ConvertInitToSet");

            context.RegisterCodeFix(convertToSetAction, diagnostic);
        }
    }

    private static Task<Document> ConvertInitToSetAsync(
        Document document,
        SyntaxNode root,
        PropertyDeclarationSyntax property,
        CancellationToken cancellationToken)
    {
        // Convert init accessor to set accessor (preserve all modifiers including required)
        var newAccessors = property.AccessorList!.Accessors.Select(accessor =>
        {
            if (accessor.IsKind(SyntaxKind.InitAccessorDeclaration))
            {
                // Create a proper SetAccessorDeclaration
                var setKeyword = SyntaxFactory.Token(SyntaxKind.SetKeyword)
                    .WithTriviaFrom(accessor.Keyword);

                var setAccessor = SyntaxFactory.AccessorDeclaration(
                    SyntaxKind.SetAccessorDeclaration,
                    accessor.AttributeLists,
                    accessor.Modifiers,
                    setKeyword,
                    accessor.Body,
                    accessor.ExpressionBody,
                    accessor.SemicolonToken);

                return setAccessor;
            }
            return accessor;
        });

        var newAccessorList = property.AccessorList.WithAccessors(
            SyntaxFactory.List(newAccessors));

        var newProperty = property.WithAccessorList(newAccessorList);

        var newRoot = root.ReplaceNode(property, newProperty);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}

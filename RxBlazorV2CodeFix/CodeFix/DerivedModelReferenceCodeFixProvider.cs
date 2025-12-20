using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Diagnostics;
using RxBlazorV2Generator.Extensions;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace RxBlazorV2CodeFix.CodeFix;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DerivedModelReferenceCodeFixProvider))]
public class DerivedModelReferenceCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.DerivedModelReferenceError.Id];

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
            if (diagnostic.Id != DiagnosticDescriptors.DerivedModelReferenceError.Id)
            {
                continue;
            }

            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan);

            // Find the constructor parameter that contains the derived model reference
            var parameter = node.FirstAncestorOrSelf<ParameterSyntax>();
            if (parameter == null)
            {
                continue;
            }

            // Code Fix: Remove the derived model reference parameter
            var removeParameterAction = CodeAction.Create(
                title: diagnostic.Descriptor.CodeFixMessage(),
                createChangedDocument: c => RemoveParameterAsync(context.Document, root, parameter, c),
                equivalenceKey: diagnostic.Descriptor.Id);

            context.RegisterCodeFix(removeParameterAction, diagnostic);
        }
    }

    private static Task<Document> RemoveParameterAsync(
        Document document,
        SyntaxNode root,
        ParameterSyntax parameter,
        CancellationToken cancellationToken)
    {
        var newRoot = SyntaxHelpers.RemoveConstructorParameter(root, parameter);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}

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

/// <summary>
/// Code fix provider for RXBG052 - Abstract class cannot be used in partial constructor.
/// Provides a fix to remove the abstract class parameter from the constructor.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AbstractClassInPartialConstructorCodeFixProvider))]
public class AbstractClassInPartialConstructorCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [
            DiagnosticDescriptors.AbstractClassInPartialConstructorError.Id
        ];

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            if (!FixableDiagnosticIds.Contains(diagnostic.Id))
            {
                continue;
            }

            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan);

            // Find the constructor parameter with the abstract class type
            var parameter = node.FirstAncestorOrSelf<ParameterSyntax>();
            if (parameter is null)
            {
                continue;
            }

            // Code Fix: Remove the abstract class parameter
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

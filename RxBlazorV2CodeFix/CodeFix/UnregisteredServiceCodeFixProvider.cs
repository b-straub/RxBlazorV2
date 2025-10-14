using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Diagnostics;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RxBlazorV2CodeFix.CodeFix;

/// <summary>
/// Provides code fixes for RXBG020 (unregistered service warning).
/// Offers to suppress the warning with [SuppressMessage] attribute including justification.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnregisteredServiceCodeFixProvider)), Shared]
public class UnregisteredServiceCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.UnregisteredServiceWarning.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel == null) return;

        foreach (var diagnostic in context.Diagnostics.Where(d => FixableDiagnosticIds.Contains(d.Id)))
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the constructor declaration
            var constructorDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
                .OfType<ConstructorDeclarationSyntax>().FirstOrDefault();

            if (constructorDeclaration != null)
            {
                // Extract parameter name and type from diagnostic properties
                var parameterName = diagnostic.Properties.TryGetValue("ParameterName", out var pName) ? pName : "service";
                var typeName = diagnostic.Properties.TryGetValue("TypeName", out var tName) ? tName : "unknown";

                // Get simple type name for display
                var simpleTypeName = typeName?.Split('.').LastOrDefault() ?? typeName ?? "service";

                // Code fix: Add [SuppressMessage] attribute with justification
                var suppressMessageAction = CodeAction.Create(
                    title: $"Suppress warning with justification for {simpleTypeName}",
                    createChangedDocument: c => Task.FromResult(AddSuppressMessageAttribute(
                        context.Document,
                        root,
                        constructorDeclaration,
                        simpleTypeName)),
                    equivalenceKey: "AddSuppressMessage");

                context.RegisterCodeFix(suppressMessageAction, diagnostic);
            }
        }
    }

    private static Document AddSuppressMessageAttribute(
        Document document,
        SyntaxNode root,
        ConstructorDeclarationSyntax constructorDeclaration,
        string typeName)
    {
        // Create the SuppressMessage attribute with justification
        var suppressMessageAttribute = SyntaxFactory.Attribute(
            SyntaxFactory.IdentifierName("SuppressMessage"),
            SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SeparatedList(new[]
                {
                    // Category argument
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal("RxBlazorGenerator"))),

                    // Diagnostic ID and title argument
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal("RXBG020:Partial constructor parameter type may not be registered in DI"))),

                    // Justification named argument
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal($"{typeName} registered externally")))
                        .WithNameEquals(
                            SyntaxFactory.NameEquals(
                                SyntaxFactory.IdentifierName("Justification")))
                })));

        // Get the indentation from the constructor (last whitespace trivia)
        var leadingTrivia = constructorDeclaration.GetLeadingTrivia();
        var indentation = leadingTrivia.LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia));

        // Create attribute list with proper indentation and trailing newline
        var attributeList = SyntaxFactory.AttributeList(
            SyntaxFactory.SingletonSeparatedList(suppressMessageAttribute))
            .WithLeadingTrivia(SyntaxFactory.TriviaList(indentation))
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        // Add attribute to constructor with ONLY indentation (strip blank lines)
        var newConstructor = constructorDeclaration
            .WithAttributeLists(constructorDeclaration.AttributeLists.Add(attributeList))
            .WithLeadingTrivia(SyntaxFactory.TriviaList(indentation));

        // Replace the constructor in the tree
        var newRoot = root.ReplaceNode(constructorDeclaration, newConstructor);

        // Add using statement for System.Diagnostics.CodeAnalysis if needed
        newRoot = SyntaxHelpers.AddUsingDirectives(newRoot, "System.Diagnostics.CodeAnalysis");

        return document.WithSyntaxRoot(newRoot);
    }
}

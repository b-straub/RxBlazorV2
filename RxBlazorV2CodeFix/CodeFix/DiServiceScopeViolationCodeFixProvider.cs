using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Diagnostics;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

namespace RxBlazorV2CodeFix.CodeFix;

/// <summary>
/// Provides code fixes for RXBG021 (DI service scope violation).
/// Offers to change the ObservableModelScope to resolve the violation.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DiServiceScopeViolationCodeFixProvider)), Shared]
public class DiServiceScopeViolationCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.DiServiceScopeViolationError.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }
        
        // Group diagnostics by class to avoid duplicate code fixes
        var diagnosticsByClass = new Dictionary<string, List<Diagnostic>>();

        foreach (var diagnostic in context.Diagnostics.Where(d => FixableDiagnosticIds.Contains(d.Id)))
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var classDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
                .OfType<ClassDeclarationSyntax>().FirstOrDefault();

            if (classDeclaration is not null)
            {
                var className = classDeclaration.Identifier.Text;
                if (!diagnosticsByClass.ContainsKey(className))
                {
                    diagnosticsByClass[className] = new List<Diagnostic>();
                }
                diagnosticsByClass[className].Add(diagnostic);
            }
        }

        // Register one code fix per class
        foreach (var kvp in diagnosticsByClass)
        {
            var className = kvp.Key;
            var classDiagnostics = kvp.Value;
            var firstDiagnostic = classDiagnostics.First();
            var diagnosticSpan = firstDiagnostic.Location.SourceSpan;
            var classDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
                .OfType<ClassDeclarationSyntax>().FirstOrDefault();

            if (classDeclaration is null)
            {
                continue;
            }

            // Get the required scope from the first diagnostic's properties
            // (all diagnostics for the same class will have the same required scope)
            var requiredScope = GetRequiredScopeFromDiagnostic(firstDiagnostic);
            if (requiredScope is null)
            {
                continue;
            }

            // Create code action with consistent equivalence key for all diagnostics in this class
            var changeScopeAction = CodeAction.Create(
                title: $"Change {className} to {requiredScope} scope",
                createChangedDocument: c => Task.FromResult(ChangeToRequiredScope(
                    context.Document,
                    root,
                    classDeclaration,
                    requiredScope)),
                equivalenceKey: $"ChangeScope_{className}_{requiredScope}");

            // Register the fix for all diagnostics
            // They all share the same equivalence key and required scope, so the batch fixer
            // will recognize them as identical and apply the fix once
            foreach (var diagnostic in classDiagnostics)
            {
                context.RegisterCodeFix(changeScopeAction, diagnostic);
            }
        }
    }

    /// <summary>
    /// Gets the required scope from diagnostic properties.
    /// The generator calculates the minimum required scope for the entire class and stores it in diagnostic properties.
    /// </summary>
    /// <param name="diagnostic">The diagnostic containing the RequiredScope property</param>
    /// <returns>The required scope, or null if not found</returns>
    private static string? GetRequiredScopeFromDiagnostic(Diagnostic diagnostic)
    {
        if (diagnostic.Properties.TryGetValue("RequiredScope", out var requiredScope))
        {
            return requiredScope;
        }
        return null;
    }

    private static Document ChangeToRequiredScope(
        Document document,
        SyntaxNode root,
        ClassDeclarationSyntax classDeclaration,
        string requiredScope)
    {
        // Find existing ObservableModelScope attribute
        var scopeAttribute = FindObservableModelScopeAttribute(classDeclaration);

        if (scopeAttribute is not null)
        {
            // Update existing attribute
            var newArgument = SyntaxFactory.AttributeArgument(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("ModelScope"),
                    SyntaxFactory.IdentifierName(requiredScope)));

            var newAttributeArgumentList = SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SingletonSeparatedList(newArgument));

            var newAttribute = scopeAttribute.WithArgumentList(newAttributeArgumentList);

            var newRoot = root.ReplaceNode(scopeAttribute, newAttribute);
            return document.WithSyntaxRoot(newRoot);
        }
        else
        {
            // Add new ObservableModelScope attribute
            var newAttribute = SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("ObservableModelScope"))
                .WithArgumentList(
                    SyntaxFactory.AttributeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.AttributeArgument(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("ModelScope"),
                                    SyntaxFactory.IdentifierName(requiredScope))))));

            // Use the SSOT helper method to add the attribute with proper trivia handling
            var newClassDeclaration = SyntaxHelpers.AddAttributePreservingTrivia(
                classDeclaration,
                newAttribute);

            // Add using statement if needed
            var newRoot = SyntaxHelpers.AddUsingDirectives(root, "RxBlazorV2.Model");
            newRoot = newRoot.ReplaceNode(
                newRoot.DescendantNodes().OfType<ClassDeclarationSyntax>()
                    .First(c => c.Identifier.Text == classDeclaration.Identifier.Text),
                newClassDeclaration);

            return document.WithSyntaxRoot(newRoot);
        }
    }

    private static AttributeSyntax? FindObservableModelScopeAttribute(ClassDeclarationSyntax classDeclaration)
    {
        return classDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(a => a.Name.ToString().Contains("ObservableModelScope"));
    }
}

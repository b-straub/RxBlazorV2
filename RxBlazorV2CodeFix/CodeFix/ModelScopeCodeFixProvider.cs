using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RxBlazorV2CodeFix.CodeFix;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ModelScopeCodeFixProvider))]
public class ModelScopeCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.SharedModelNotSingletonError.Id];

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        foreach (var diagnostic in context.Diagnostics.Where(d => FixableDiagnosticIds.Contains(d.Id)))
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var classDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
                .OfType<ClassDeclarationSyntax>().FirstOrDefault();

            if (classDeclaration != null)
            {
                var className = classDeclaration.Identifier.Text;

                // Code fix 1: Change to Singleton scope
                var changeScopeAction = CodeAction.Create(
                    title: $"Change {className} to Singleton scope",
                    createChangedDocument: c => Task.FromResult(ChangeToSingletonScope(context.Document, root, classDeclaration)),
                    equivalenceKey: "ChangeToSingleton");

                context.RegisterCodeFix(changeScopeAction, diagnostic);

                // Code fix 2: Remove scope attribute (defaults to Singleton)
                var removeScopeAction = CodeAction.Create(
                    title: $"Remove scope attribute from {className} (defaults to Singleton)",
                    createChangedDocument: c => Task.FromResult(RemoveScopeAttribute(context.Document, root, classDeclaration)),
                    equivalenceKey: "RemoveScopeAttribute");

                context.RegisterCodeFix(removeScopeAction, diagnostic);
            }
        }
    }

    private static Document ChangeToSingletonScope(Document document, SyntaxNode root, ClassDeclarationSyntax classDeclaration)
    {
        // Find existing ObservableModelScope attribute
        var scopeAttribute = FindObservableModelScopeAttribute(classDeclaration);

        if (scopeAttribute is not null)
        {
            // Update existing attribute to Singleton
            var newArgument = SyntaxFactory.AttributeArgument(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("ModelScope"),
                    SyntaxFactory.IdentifierName("Singleton")));

            var newAttributeArgumentList = SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SingletonSeparatedList(newArgument));

            var newAttribute = scopeAttribute.WithArgumentList(newAttributeArgumentList);
            
            var newRoot = root.ReplaceNode(scopeAttribute, newAttribute);
            return document.WithSyntaxRoot(newRoot);
        }
        else
        {
            // Add new ObservableModelScope attribute with Singleton
            var newAttribute = SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("ObservableModelScope"))
                .WithArgumentList(
                    SyntaxFactory.AttributeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.AttributeArgument(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("ModelScope"),
                                    SyntaxFactory.IdentifierName("Singleton"))))));

            var newAttributeList = SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(newAttribute));

            var newClassDeclaration = classDeclaration.WithAttributeLists(
                classDeclaration.AttributeLists.Add(newAttributeList));

            // Add using statement if needed
            var newRoot = AddUsingStatementIfNeeded(root, "RxBlazorV2.Model");
            newRoot = newRoot.ReplaceNode(
                newRoot.DescendantNodes().OfType<ClassDeclarationSyntax>()
                    .First(c => c.Identifier.Text == classDeclaration.Identifier.Text),
                newClassDeclaration);

            return document.WithSyntaxRoot(newRoot);
        }
    }

    private static Document RemoveScopeAttribute(Document document, SyntaxNode root, ClassDeclarationSyntax classDeclaration)
    {
        var scopeAttribute = FindObservableModelScopeAttribute(classDeclaration);
        
        if (scopeAttribute is not null)
        {
            var attributeList = scopeAttribute.Parent as AttributeListSyntax;
            if (attributeList is null) return document;

            SyntaxNode newRoot;
            if (attributeList.Attributes.Count == 1)
            {
                // Remove the entire attribute list if it's the only attribute
                var tempRoot = root.RemoveNode(attributeList, SyntaxRemoveOptions.KeepNoTrivia) ?? throw new InvalidOperationException("Failed to remove attribute list from syntax tree");
                newRoot = tempRoot;
            }
            else
            {
                // Remove just this attribute from the list
                var newAttributeList = attributeList.RemoveNode(scopeAttribute, SyntaxRemoveOptions.KeepNoTrivia) ?? throw new InvalidOperationException("Failed to remove attribute from attribute list");
                var tempRoot = root.ReplaceNode(attributeList, newAttributeList) ?? throw new InvalidOperationException("Failed to replace attribute list in syntax tree");
                newRoot = tempRoot;
            }

            return document.WithSyntaxRoot(newRoot);
        }

        return document;
    }

    private static AttributeSyntax? FindObservableModelScopeAttribute(ClassDeclarationSyntax classDeclaration)
    {
        return classDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(a => a.Name.ToString().Contains("ObservableModelScope"));
    }

    private static SyntaxNode AddUsingStatementIfNeeded(SyntaxNode root, string namespaceName)
    {
        return SyntaxHelpers.AddUsingDirectives(root, namespaceName);
    }
}
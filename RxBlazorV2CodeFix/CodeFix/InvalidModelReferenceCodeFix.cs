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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InvalidModelReferenceCodeFix))]
public class InvalidModelReferenceCodeFix : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.InvalidModelReferenceTargetError.Id];

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Id != DiagnosticDescriptors.InvalidModelReferenceTargetError.Id) continue;

            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan);

            // Find the attribute that contains the invalid reference
            var attribute = node.FirstAncestorOrSelf<AttributeSyntax>();
            if (attribute == null) continue;

            // Extract the referenced model name from the diagnostic message
            var referencedModelName = ExtractModelNameFromDiagnostic(diagnostic.GetMessage());
            if (string.IsNullOrEmpty(referencedModelName)) continue;

            // Code Fix 1: Remove the invalid attribute
            var removeAttributeAction = CodeAction.Create(
                title: $"Remove invalid model reference attribute",
                createChangedDocument: c => RemoveAttributeAsync(context.Document, root, attribute, c),
                equivalenceKey: "RemoveAttribute");

            context.RegisterCodeFix(removeAttributeAction, diagnostic);

            // Code Fix 2: Make the referenced class inherit from ObservableModel (only if we found the model name)
            if (!string.IsNullOrEmpty(referencedModelName))
            {
                var makeObservableAction = CodeAction.Create(
                    title: $"Make '{referencedModelName}' inherit from ObservableModel",
                    createChangedDocument: c => MakeClassObservableAsync(context.Document, referencedModelName!, c),
                    equivalenceKey: "MakeObservable");

                context.RegisterCodeFix(makeObservableAction, diagnostic);
            }
        }
    }

    private static string? ExtractModelNameFromDiagnostic(string message)
    {
        // Extract model name from message: "Referenced model 'ModelName' does not inherit from ObservableModel or implement IObservableModel"
        var startIndex = message.IndexOf("'") + 1;
        if (startIndex <= 0) return null;
        
        var endIndex = message.IndexOf("'", startIndex);
        if (endIndex <= startIndex) return null;
        
        return message.Substring(startIndex, endIndex - startIndex);
    }

    private static Task<Document> RemoveAttributeAsync(Document document, SyntaxNode root, AttributeSyntax attribute, CancellationToken cancellationToken)
    {
        var attributeList = attribute.Parent as AttributeListSyntax;
        if (attributeList == null) return Task.FromResult(document);

        SyntaxNode? newRoot = null;

        if (attributeList.Attributes.Count == 1)
        {
            // Remove the entire attribute list if this is the only attribute
            if (attributeList.Parent is ClassDeclarationSyntax classDecl)
            {
                var newAttributeLists = classDecl.AttributeLists.RemoveKeepTrivia(attributeList);
                var newClassDecl = classDecl.WithAttributeLists(newAttributeLists);
                newRoot = root.ReplaceNode(classDecl, newClassDecl);
            }
            else
            {
                // For other node types, just return the original document
                newRoot = root;
            }
        }
        else
        {
            // Remove just this attribute from the list
            var newAttributes = attributeList.Attributes.Remove(attribute);
            var newAttributeList = attributeList.WithAttributes(newAttributes);
            newRoot = root.ReplaceNode(attributeList, newAttributeList);
        }

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static async Task<Document> MakeClassObservableAsync(Document document, string className, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        // Find the class declaration for the referenced model
        var classDeclaration = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.ValueText == className);

        if (classDeclaration == null)
        {
            // The class might be in a different file - we'll need to search the project
            return await MakeClassObservableInProjectAsync(document, className, cancellationToken);
        }

        return UpdateClassToInheritObservableModel(document, root, classDeclaration, cancellationToken);
    }

    private static async Task<Document> MakeClassObservableInProjectAsync(Document document, string className, CancellationToken cancellationToken)
    {
        var project = document.Project;
        
        // Search all documents in the project for the class
        foreach (var doc in project.Documents)
        {
            var docRoot = await doc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (docRoot == null) continue;

            var classDeclaration = docRoot.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.ValueText == className);

            if (classDeclaration != null)
            {
                return UpdateClassToInheritObservableModel(doc, docRoot, classDeclaration, cancellationToken);
            }
        }

        return document; // Class not found
    }

    private static Document UpdateClassToInheritObservableModel(Document document, SyntaxNode root, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
    {
        // Check if the class already has a base list
        var newClassDeclaration = classDeclaration;

        if (newClassDeclaration.BaseList == null)
        {
            var oldTrivia = newClassDeclaration.Identifier.TrailingTrivia;
            var identifier = newClassDeclaration.Identifier.WithTrailingTrivia();
            
            newClassDeclaration = newClassDeclaration.WithIdentifier(identifier);
            // Add ObservableModel as base class
            var baseType = SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName("ObservableModel"));
            var baseList = SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(baseType))
                .WithTrailingTrivia(oldTrivia);
            newClassDeclaration = newClassDeclaration.WithBaseList(baseList);
        }
        else
        {
            // Insert ObservableModel as the first base type (base class must come before interfaces)
            var baseType = SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName("ObservableModel"));
            var newTypes = newClassDeclaration.BaseList.Types.Insert(0, baseType);
            var newBaseList = newClassDeclaration.BaseList.WithTypes(newTypes);
            newClassDeclaration = newClassDeclaration.WithBaseList(newBaseList);
        }

        // Make the class partial if it isn't already
        if (!newClassDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            var partialModifier = SyntaxFactory.Token(SyntaxKind.PartialKeyword);
            newClassDeclaration = newClassDeclaration.WithModifiers(
                newClassDeclaration.Modifiers.Add(partialModifier));
        }

        // Add necessary using statements
        var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);
        newRoot = AddRequiredUsingStatements(document, newRoot, cancellationToken);

        return document.WithSyntaxRoot(newRoot);
    }

    private static SyntaxNode AddRequiredUsingStatements(Document document, SyntaxNode root, CancellationToken cancellationToken)
    {
        var compilationUnit = root as CompilationUnitSyntax;
        if (compilationUnit == null) return root;

        var requiredUsings = new[]
        {
            "RxBlazorV2.Model",
            "RxBlazorV2.Interface"
        };

        var existingUsings = new HashSet<string>(
            compilationUnit.Usings
                .Select(u => u.Name?.ToString())
                .Where(name => !string.IsNullOrEmpty(name))
                .Cast<string>());

        var newUsings = new List<UsingDirectiveSyntax>();

        foreach (var usingNamespace in requiredUsings)
        {
            if (!existingUsings.Contains(usingNamespace))
            {
                var usingDirective = SyntaxFactory.UsingDirective(
                    SyntaxFactory.IdentifierName(usingNamespace));
                newUsings.Add(usingDirective);
            }
        }

        if (newUsings.Any())
        {
            var allUsings = compilationUnit.Usings.AddRange(newUsings);
            return compilationUnit.WithUsings(allUsings);
        }

        return root;
    }
}
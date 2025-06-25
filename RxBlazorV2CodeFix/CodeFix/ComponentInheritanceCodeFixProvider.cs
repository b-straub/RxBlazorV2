using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Diagnostics;
using RxBlazorV2CodeFix.CodeFix;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RxBlazorV2CodeFix.CodeFix;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ComponentInheritanceCodeFixProvider))]
public class ComponentInheritanceCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.ComponentNotObservableError.Id];

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        foreach (var diagnostic in context.Diagnostics.Where(d => FixableDiagnosticIds.Contains(d.Id)))
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var classDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
                .OfType<ClassDeclarationSyntax>().FirstOrDefault();

            if (classDeclaration is not null)
            {
                var className = classDeclaration.Identifier.Text;

                // Code fix 1: Change inheritance to ObservableComponent
                var changeInheritanceAction = CodeAction.Create(
                    title: $"Change {className} to inherit from ObservableComponent",
                    createChangedDocument: c => Task.FromResult(ChangeToObservableComponent(context.Document, root, classDeclaration)),
                    equivalenceKey: "ChangeToObservableComponent");

                context.RegisterCodeFix(changeInheritanceAction, diagnostic);

                // Code fix 2: Remove ObservableModel attributes
                var removeAttributesAction = CodeAction.Create(
                    title: $"Remove ObservableModel attributes from {className}",
                    createChangedDocument: c => Task.FromResult(RemoveObservableModelAttributes(context.Document, root, classDeclaration)),
                    equivalenceKey: "RemoveAttributes");

                context.RegisterCodeFix(removeAttributesAction, diagnostic);
            }
        }
    }

    private static Document ChangeToObservableComponent(Document document, SyntaxNode root, ClassDeclarationSyntax classDeclaration)
    {
        var newClassDeclaration = classDeclaration;

        // Find the model type from ObservableModelScope or ObservableModelReference attributes
        var modelType = FindModelTypeFromAttributes(classDeclaration);
        if (modelType == null)
        {
            // If we can't determine the model type, just use ComponentBase -> ObservableComponent without type arguments
            // This may cause compilation errors but is the best we can do
            modelType = "TModel"; // Generic placeholder
        }

        // Update base class from ComponentBase to ObservableComponent<T>
        if (newClassDeclaration.BaseList is not null)
        {
            var componentBaseType = newClassDeclaration.BaseList.Types
                .FirstOrDefault(t => t.Type.ToString().Contains("ComponentBase"));

            if (componentBaseType is not null)
            {
                // Create ObservableComponent<ModelType>
                var genericName = SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("ObservableComponent"))
                    .WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                SyntaxFactory.IdentifierName(modelType))));
                
                var observableComponentType = SyntaxFactory.SimpleBaseType(genericName)
                    .WithTriviaFrom(componentBaseType);
                
                var newTypes = newClassDeclaration.BaseList.Types.Replace(componentBaseType, observableComponentType);
                var newBaseList = newClassDeclaration.BaseList.WithTypes(newTypes);
                newClassDeclaration = newClassDeclaration.WithBaseList(newBaseList);
            }
        }
        else
        {
            // Add ObservableComponent<T> as base class if no base list exists
            var genericName = SyntaxFactory.GenericName(
                SyntaxFactory.Identifier("ObservableComponent"))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            SyntaxFactory.IdentifierName(modelType))));
            
            var baseType = SyntaxFactory.SimpleBaseType(genericName);
            var baseList = SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(baseType));
            newClassDeclaration = newClassDeclaration.WithBaseList(baseList);
        }

        // Preserve original trivia from the class declaration
        newClassDeclaration = newClassDeclaration.WithTriviaFrom(classDeclaration);

        // Add necessary using statements
        var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);
        newRoot = AddUsingStatementIfNeeded(newRoot, "RxBlazorV2.Component");

        return document.WithSyntaxRoot(newRoot);
    }

    private static Document RemoveObservableModelAttributes(Document document, SyntaxNode root, ClassDeclarationSyntax classDeclaration)
    {
        var newClassDeclaration = classDeclaration;
        var modifiedAttributeLists = new List<AttributeListSyntax>();
        var attributeListsToRemove = new List<AttributeListSyntax>();

        // Process all attribute lists in a single pass
        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            var nonObservableModelAttributes = attributeList.Attributes
                .Where(attr => !IsObservableModelAttribute(attr))
                .ToList();

            if (nonObservableModelAttributes.Count == 0)
            {
                // Remove entire attribute list if all attributes are ObservableModel-related
                attributeListsToRemove.Add(attributeList);
            }
            else if (nonObservableModelAttributes.Count < attributeList.Attributes.Count)
            {
                // Create new attribute list with only non-ObservableModel attributes
                var newAttributes = SyntaxFactory.SeparatedList(nonObservableModelAttributes);
                var newAttributeList = attributeList.WithAttributes(newAttributes);
                modifiedAttributeLists.Add(newAttributeList);
            }
            else
            {
                // Keep the attribute list as-is (no ObservableModel attributes found)
                modifiedAttributeLists.Add(attributeList);
            }
        }

        // Apply all changes in a single operation
        var finalAttributeLists = new List<AttributeListSyntax>();
        
        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            if (!attributeListsToRemove.Contains(attributeList))
            {
                var replacement = modifiedAttributeLists.FirstOrDefault(m => 
                    m.GetLocation().SourceSpan.Equals(attributeList.GetLocation().SourceSpan));
                finalAttributeLists.Add(replacement ?? attributeList);
            }
        }

        // Create new class declaration with updated attribute lists
        newClassDeclaration = newClassDeclaration.WithAttributeLists(
            SyntaxFactory.List(finalAttributeLists));

        // Preserve original class trivia
        newClassDeclaration = newClassDeclaration.WithTriviaFrom(classDeclaration);

        var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }

    private static bool IsObservableModelAttribute(AttributeSyntax attribute)
    {
        var attributeName = attribute.Name.ToString();
        return attributeName.Contains("ObservableModelScope") ||
               attributeName.Contains("ObservableModelReference");
    }

    private static string? FindModelTypeFromAttributes(ClassDeclarationSyntax classDeclaration)
    {
        // Look for ObservableModelReference<T> attribute to extract the model type
        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attributeName = attribute.Name.ToString();
                if (attributeName.Contains("ObservableModelReference"))
                {
                    // Extract type argument from ObservableModelReference<T>
                    if (attribute.Name is GenericNameSyntax genericName && 
                        genericName.TypeArgumentList.Arguments.Count > 0)
                    {
                        return genericName.TypeArgumentList.Arguments[0].ToString();
                    }
                }
            }
        }

        // If no ObservableModelReference found, look for a Model property of ObservableModel type
        foreach (var member in classDeclaration.Members.OfType<PropertyDeclarationSyntax>())
        {
            if (member.Identifier.ValueText == "Model")
            {
                return member.Type.ToString();
            }
        }

        // If no explicit model type found, return null
        return null;
    }

    private static SyntaxNode AddUsingStatementIfNeeded(SyntaxNode root, string namespaceName)
    {
        if (root is CompilationUnitSyntax compilationUnit)
        {
            var hasUsing = compilationUnit.Usings.Any(u => u.Name?.ToString() == namespaceName);

            if (!hasUsing)
            {
                // Create new using directive with proper trivia based on existing usings
                var newUsing = SyntaxFactory.UsingDirective(
                    SyntaxFactory.ParseName(namespaceName));

                // If there are existing usings, copy trivia from the last one
                if (compilationUnit.Usings.Any())
                {
                    var lastUsing = compilationUnit.Usings.Last();
                    newUsing = newUsing.WithTriviaFrom(lastUsing);
                }

                return compilationUnit.WithUsings(compilationUnit.Usings.Add(newUsing));
            }
        }

        return root;
    }
}
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
        [DiagnosticDescriptors.ComponentNotObservableWarning.Id];

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

                // Check base type to determine appropriate fix
                var baseType = classDeclaration.BaseList?.Types.FirstOrDefault()?.Type.ToString();
                if (baseType != null)
                {
                    string targetType;
                    string title;

                    if (baseType.Contains("LayoutComponentBase"))
                    {
                        // LayoutComponentBase → ObservableLayoutComponentBase
                        targetType = "ObservableLayoutComponentBase";
                        title = $"Change {className} to inherit from ObservableLayoutComponentBase";
                    }
                    else if (baseType.Contains("ComponentBase") || baseType.Contains("OwningComponentBase"))
                    {
                        // ComponentBase/OwningComponentBase → ObservableComponent
                        targetType = "ObservableComponent";
                        title = $"Change {className} to inherit from ObservableComponent";
                    }
                    else
                    {
                        return; // Unknown base type
                    }

                    // Code fix: Change inheritance
                    var changeInheritanceAction = CodeAction.Create(
                        title: title,
                        createChangedDocument: c => Task.FromResult(ChangeToObservableComponent(context.Document, root, classDeclaration, targetType)),
                        equivalenceKey: "ChangeToObservableComponent");

                    context.RegisterCodeFix(changeInheritanceAction, diagnostic);
                }
            }
        }
    }

    private static Document ChangeToObservableComponent(Document document, SyntaxNode root, ClassDeclarationSyntax classDeclaration, string targetTypeName = "ObservableComponent")
    {
        var newClassDeclaration = classDeclaration;

        // Check if there are [Inject] properties with ObservableModel types
        // If so, use non-generic version (since models are injected manually)
        // Otherwise, use generic version if we can determine the model type
        var hasInjectProperties = classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .Any(p => p.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(attr => attr.Name.ToString().Contains("Inject")));

        // Update base class from ComponentBase/OwningComponentBase/LayoutComponentBase to target Observable type
        if (newClassDeclaration.BaseList is not null)
        {
            var componentBaseType = newClassDeclaration.BaseList.Types
                .FirstOrDefault(t => t.Type.ToString().Contains("ComponentBase") ||
                                    t.Type.ToString().Contains("OwningComponentBase"));

            if (componentBaseType is not null)
            {
                TypeSyntax observableComponentTypeSyntax;

                // Check if the base class is generic (e.g., OwningComponentBase<T>)
                var isGenericBase = componentBaseType.Type is GenericNameSyntax genericBase;

                if (isGenericBase && componentBaseType.Type is GenericNameSyntax genericBaseName)
                {
                    // Extract the generic type parameter from OwningComponentBase<T>
                    var genericTypeArg = genericBaseName.TypeArgumentList.Arguments.FirstOrDefault();
                    if (genericTypeArg is not null)
                    {
                        // Create ObservableComponent<T> with the same type parameter
                        observableComponentTypeSyntax = SyntaxFactory.GenericName(
                            SyntaxFactory.Identifier(targetTypeName))
                            .WithTypeArgumentList(
                                SyntaxFactory.TypeArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(genericTypeArg)));
                    }
                    else
                    {
                        observableComponentTypeSyntax = SyntaxFactory.IdentifierName(targetTypeName);
                    }
                }
                else if (hasInjectProperties)
                {
                    // Use non-generic version when models are injected
                    observableComponentTypeSyntax = SyntaxFactory.IdentifierName(targetTypeName);
                }
                else
                {
                    // Find the model type from attributes for generic version
                    var modelType = FindModelTypeFromAttributes(classDeclaration);
                    if (modelType is not null)
                    {
                        // Create generic version with ModelType
                        observableComponentTypeSyntax = SyntaxFactory.GenericName(
                            SyntaxFactory.Identifier(targetTypeName))
                            .WithTypeArgumentList(
                                SyntaxFactory.TypeArgumentList(
                                    SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                        SyntaxFactory.IdentifierName(modelType))));
                    }
                    else
                    {
                        // Fallback to non-generic if no model type found
                        observableComponentTypeSyntax = SyntaxFactory.IdentifierName(targetTypeName);
                    }
                }

                var observableComponentType = SyntaxFactory.SimpleBaseType(observableComponentTypeSyntax)
                    .WithTriviaFrom(componentBaseType);

                var newTypes = newClassDeclaration.BaseList.Types.Replace(componentBaseType, observableComponentType);
                var newBaseList = newClassDeclaration.BaseList.WithTypes(newTypes);
                newClassDeclaration = newClassDeclaration.WithBaseList(newBaseList);
            }
        }
        else
        {
            // Add target type as base class if no base list exists
            TypeSyntax baseTypeSyntax = hasInjectProperties
                ? SyntaxFactory.IdentifierName(targetTypeName)
                : SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier(targetTypeName))
                    .WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                SyntaxFactory.IdentifierName("TModel")))); // Generic placeholder

            var baseType = SyntaxFactory.SimpleBaseType(baseTypeSyntax);
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
        return SyntaxHelpers.AddUsingDirectives(root, namespaceName);
    }
}
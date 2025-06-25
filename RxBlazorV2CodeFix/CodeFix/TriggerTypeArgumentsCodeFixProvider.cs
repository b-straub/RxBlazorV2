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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TriggerTypeArgumentsCodeFixProvider))]
public class TriggerTypeArgumentsCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.TriggerTypeArgumentsMismatchError.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        foreach (var diagnostic in context.Diagnostics.Where(d => FixableDiagnosticIds.Contains(d.Id)))
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var attributeNode = root.FindNode(diagnosticSpan);
            var attribute = attributeNode.FirstAncestorOrSelf<AttributeSyntax>();
            
            if (attribute is not null)
            {
                var propertyDeclaration = attribute.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
                if (propertyDeclaration is not null)
                {
                    // Code fix 1: Fix trigger type arguments to match command
                    var fixTypeArgumentsAction = CodeAction.Create(
                        title: "Fix trigger type arguments to match command type",
                        createChangedDocument: c => Task.FromResult(FixTriggerTypeArguments(context.Document, root, attribute, propertyDeclaration)),
                        equivalenceKey: "FixTriggerTypeArguments");

                    context.RegisterCodeFix(fixTypeArgumentsAction, diagnostic);

                    // Code fix 2: Remove trigger type arguments (use non-generic version)
                    var removeTypeArgumentsAction = CodeAction.Create(
                        title: "Remove trigger type arguments (use non-generic trigger)",
                        createChangedDocument: c => Task.FromResult(RemoveTriggerTypeArguments(context.Document, root, attribute)),
                        equivalenceKey: "RemoveTriggerTypeArguments");

                    context.RegisterCodeFix(removeTypeArgumentsAction, diagnostic);
                }
            }
        }
    }

    private static Document FixTriggerTypeArguments(Document document, SyntaxNode root, AttributeSyntax triggerAttribute, PropertyDeclarationSyntax propertyDeclaration)
    {
        // Extract type arguments from the command property type
        var commandTypeArguments = ExtractCommandTypeArguments(propertyDeclaration.Type);
        
        if (commandTypeArguments is not null)
        {
            // Create new trigger attribute with corrected type arguments
            var newAttribute = CreateTriggerAttributeWithTypeArguments(triggerAttribute, commandTypeArguments);
            var newRoot = root.ReplaceNode(triggerAttribute, newAttribute);
            return document.WithSyntaxRoot(newRoot);
        }

        return document;
    }

    private static Document RemoveTriggerTypeArguments(Document document, SyntaxNode root, AttributeSyntax triggerAttribute)
    {
        // Remove generic type arguments from trigger attribute
        var attributeName = triggerAttribute.Name;
        
        // Convert ObservableCommandTrigger<T> to ObservableCommandTrigger
        AttributeSyntax newAttribute;
        if (attributeName is GenericNameSyntax genericName)
        {
            var simpleName = SyntaxFactory.IdentifierName(genericName.Identifier);
            newAttribute = triggerAttribute.WithName(simpleName);
        }
        else
        {
            // Already non-generic, keep as is
            newAttribute = triggerAttribute;
        }

        // Remove the generic type parameter from arguments if it exists
        if (triggerAttribute.ArgumentList is not null && triggerAttribute.ArgumentList.Arguments.Count > 1)
        {
            // Keep only the first argument (property name), remove the parameter value
            var firstArgument = triggerAttribute.ArgumentList.Arguments[0];
            var newArgumentList = SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SingletonSeparatedList(firstArgument));
            newAttribute = newAttribute.WithArgumentList(newArgumentList);
        }

        var newRoot = root.ReplaceNode(triggerAttribute, newAttribute);
        return document.WithSyntaxRoot(newRoot);
    }

    private static TypeArgumentListSyntax? ExtractCommandTypeArguments(TypeSyntax commandType)
    {
        // Handle different forms of generic types (IObservableCommand<T>, IObservableCommandAsync<T>)
        return commandType switch
        {
            GenericNameSyntax genericName => genericName.TypeArgumentList,
            QualifiedNameSyntax { Right: GenericNameSyntax rightGeneric } => rightGeneric.TypeArgumentList,
            _ => null
        };
    }

    private static AttributeSyntax CreateTriggerAttributeWithTypeArguments(AttributeSyntax originalAttribute, TypeArgumentListSyntax typeArguments)
    {
        // Create ObservableCommandTrigger<T> with the correct type arguments
        var genericName = SyntaxFactory.GenericName(
            SyntaxFactory.Identifier("ObservableCommandTrigger"))
            .WithTypeArgumentList(typeArguments)
            .WithoutTrivia();

        var newAttribute = originalAttribute.WithName(genericName);

        // Update parameter value to match the new type if needed
        if (originalAttribute.ArgumentList is not null && originalAttribute.ArgumentList.Arguments.Count > 1)
        {
            var firstArg = originalAttribute.ArgumentList.Arguments[0]; // property name
            var newTypeArg = typeArguments.Arguments[0]; // the new type
            
            // Generate default value for the new type
            var defaultValue = GenerateDefaultValueForType(newTypeArg);
            var newParameterArg = SyntaxFactory.AttributeArgument(defaultValue);
            
            var newArgumentList = SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SeparatedList<AttributeArgumentSyntax>(new[] { firstArg, newParameterArg }));
            
            newAttribute = newAttribute.WithArgumentList(newArgumentList);
        }

        return newAttribute.WithoutTrivia().WithTriviaFrom(originalAttribute);
    }

    private static ExpressionSyntax GenerateDefaultValueForType(TypeSyntax typeSyntax)
    {
        // Generate default values based on type
        var typeString = typeSyntax.ToString();
        return typeString switch
        {
            "string" => SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression),
            "int" => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)),
            "bool" => SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression),
            "double" => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0.0)),
            "float" => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0.0f)),
            _ => SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression) // default to null for other types
        };
    }
}
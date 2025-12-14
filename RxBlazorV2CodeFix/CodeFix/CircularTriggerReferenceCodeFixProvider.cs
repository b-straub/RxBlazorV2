using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Diagnostics;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace RxBlazorV2CodeFix.CodeFix;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CircularTriggerReferenceCodeFixProvider))]
public class CircularTriggerReferenceCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.CircularTriggerReferenceError.Id);

    // No FixAll - each circular reference needs individual review
    public sealed override FixAllProvider? GetFixAllProvider() => null;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        foreach (var diagnostic in context.Diagnostics.Where(d => FixableDiagnosticIds.Contains(d.Id)))
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan);

            // Check if this is an internal observer (diagnostic on method identifier)
            if (diagnostic.Properties.TryGetValue("IsInternalObserver", out var isInternal) && isInternal == "true")
            {
                var methodDecl = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
                if (methodDecl is not null)
                {
                    RegisterInternalObserverCodeFixes(context, diagnostic, root, methodDecl);
                }
                continue;
            }

            // Case 1: ObservableTrigger/ObservableCommandTrigger - diagnostic is on the attribute
            var attribute = node.FirstAncestorOrSelf<AttributeSyntax>();
            if (attribute is not null)
            {
                RegisterAttributeCodeFixes(context, diagnostic, root, attribute);
            }
        }
    }

    private static void RegisterAttributeCodeFixes(
        CodeFixContext context,
        Diagnostic diagnostic,
        SyntaxNode root,
        AttributeSyntax attribute)
    {
        // Option 1: Remove the trigger attribute
        var removeTriggerAction = CodeAction.Create(
            title: "Remove circular trigger attribute",
            createChangedDocument: c => Task.FromResult(RemoveAttribute(context.Document, root, attribute)),
            equivalenceKey: "RemoveCircularTriggerAttribute");
        context.RegisterCodeFix(removeTriggerAction, diagnostic);

        // Option 2: Remove the property modification in the execute method
        if (diagnostic.Properties.TryGetValue("ExecuteMethod", out var executeMethod) &&
            diagnostic.Properties.TryGetValue("TriggerProperty", out var triggerProperty) &&
            executeMethod is { Length: > 0 } &&
            triggerProperty is { Length: > 0 })
        {
            var classDecl = attribute.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDecl is not null)
            {
                var methodDecl = classDecl.Members
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(m => m.Identifier.ValueText == executeMethod);

                if (methodDecl is not null)
                {
                    var modificationStatement = FindPropertyModificationStatement(methodDecl, triggerProperty);
                    if (modificationStatement is not null)
                    {
                        var removeModificationAction = CodeAction.Create(
                            title: $"Remove modification of '{triggerProperty}'",
                            createChangedDocument: c => Task.FromResult(RemoveStatement(context.Document, root, modificationStatement)),
                            equivalenceKey: "RemoveCircularPropertyModification");
                        context.RegisterCodeFix(removeModificationAction, diagnostic);
                    }
                }
            }
        }
    }

    private static void RegisterInternalObserverCodeFixes(
        CodeFixContext context,
        Diagnostic diagnostic,
        SyntaxNode root,
        MethodDeclarationSyntax methodDecl)
    {
        var methodName = methodDecl.Identifier.ValueText;

        // Option 1: Remove the observer method
        var removeMethodAction = CodeAction.Create(
            title: $"Remove observer method '{methodName}'",
            createChangedDocument: c => Task.FromResult(RemoveMethod(context.Document, root, methodDecl)),
            equivalenceKey: "RemoveCircularObserverMethod");
        context.RegisterCodeFix(removeMethodAction, diagnostic);

        // Option 2: Remove the property modification statement(s)
        if (diagnostic.Properties.TryGetValue("CircularProperty", out var circularProperty) &&
            circularProperty is { Length: > 0 })
        {
            // CircularProperty may contain multiple properties (comma-separated)
            // Find the first modification statement for any of them
            var properties = circularProperty.Split(',').Select(p => p.Trim()).ToList();

            foreach (var prop in properties)
            {
                // Extract just the property name from "Model.Property" format
                var propertyName = prop.Contains('.') ? prop.Split('.').Last() : prop;
                var modificationStatement = FindPropertyModificationStatement(methodDecl, propertyName);

                if (modificationStatement is not null)
                {
                    var removeModificationAction = CodeAction.Create(
                        title: $"Remove modification of '{prop}'",
                        createChangedDocument: c => Task.FromResult(RemoveStatement(context.Document, root, modificationStatement)),
                        equivalenceKey: $"RemoveCircularPropertyModification_{prop}");
                    context.RegisterCodeFix(removeModificationAction, diagnostic);
                }
            }
        }
    }

    private static StatementSyntax? FindPropertyModificationStatement(MethodDeclarationSyntax method, string propertyName)
    {
        if (method.Body is null)
        {
            return null;
        }

        // Find the first statement that modifies the property
        foreach (var statement in method.Body.Statements)
        {
            if (StatementModifiesProperty(statement, propertyName))
            {
                return statement;
            }
        }

        return null;
    }

    private static bool StatementModifiesProperty(StatementSyntax statement, string propertyName)
    {
        // Check expression statements (assignments, increments, etc.)
        if (statement is ExpressionStatementSyntax expressionStatement)
        {
            return ExpressionModifiesProperty(expressionStatement.Expression, propertyName);
        }

        return false;
    }

    private static bool ExpressionModifiesProperty(ExpressionSyntax expression, string propertyName)
    {
        // Handle assignment expressions: Counter = 0, Counter += 1, etc.
        if (expression is AssignmentExpressionSyntax assignment)
        {
            return IsPropertyAccess(assignment.Left, propertyName);
        }

        // Handle prefix expressions: ++Counter, --Counter
        if (expression is PrefixUnaryExpressionSyntax prefix)
        {
            return IsPropertyAccess(prefix.Operand, propertyName);
        }

        // Handle postfix expressions: Counter++, Counter--
        if (expression is PostfixUnaryExpressionSyntax postfix)
        {
            return IsPropertyAccess(postfix.Operand, propertyName);
        }

        return false;
    }

    private static bool IsPropertyAccess(ExpressionSyntax expression, string propertyName)
    {
        // Direct property access: Counter
        if (expression is IdentifierNameSyntax identifier)
        {
            return identifier.Identifier.ValueText == propertyName;
        }

        // Member access: this.Counter or Model.Counter
        if (expression is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Name.Identifier.ValueText == propertyName;
        }

        return false;
    }

    private static Document RemoveAttribute(Document document, SyntaxNode root, AttributeSyntax attribute)
    {
        var newRoot = SyntaxHelpers.RemoveAttributeFromClass(root, attribute);
        return document.WithSyntaxRoot(newRoot);
    }

    private static Document RemoveStatement(Document document, SyntaxNode root, StatementSyntax statement)
    {
        // Use KeepNoTrivia - the next statement has its own leading trivia
        var newRoot = root.RemoveNode(statement, SyntaxRemoveOptions.KeepNoTrivia);
        return newRoot is null ? document : document.WithSyntaxRoot(newRoot);
    }

    private static Document RemoveMethod(Document document, SyntaxNode root, MethodDeclarationSyntax method)
    {
        var newRoot = root.RemoveNode(method, SyntaxRemoveOptions.KeepNoTrivia);
        return newRoot is null ? document : document.WithSyntaxRoot(newRoot);
    }
}

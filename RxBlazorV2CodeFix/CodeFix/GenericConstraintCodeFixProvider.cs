using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Diagnostics;
using RxBlazorV2Generator.Extensions;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using RxBlazorV2Generator.Helpers;

namespace RxBlazorV2CodeFix.CodeFix;

/// <summary>
/// Code fix provider for generic constraint diagnostics (RXBG020, RXBG021, RXBG022).
/// Adjusts type parameters or removes invalid references when generic type constraints are not satisfied.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(GenericConstraintCodeFixProvider)), Shared]
public class GenericConstraintCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(
            DiagnosticDescriptors.GenericArityMismatchError.Id,
            DiagnosticDescriptors.TypeConstraintMismatchError.Id,
            DiagnosticDescriptors.InvalidOpenGenericReferenceError.Id
        );

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        semanticModel.ThrowIfNull();

        foreach (var diagnostic in context.Diagnostics)
        {
            if (!FixableDiagnosticIds.Contains(diagnostic.Id))
            {
                continue;
            }

            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan);

            var attribute = node.FirstAncestorOrSelf<AttributeSyntax>();
            if (attribute is null)
            {
                continue;
            }

            // For GenericArityMismatchError, TypeConstraintMismatchError, and InvalidOpenGenericReferenceError, offer to adjust the referencing class type parameters
            if (diagnostic.Id == DiagnosticDescriptors.GenericArityMismatchError.Id ||
                diagnostic.Id == DiagnosticDescriptors.TypeConstraintMismatchError.Id ||
                diagnostic.Id == DiagnosticDescriptors.InvalidOpenGenericReferenceError.Id)
            {
                var classDeclaration = attribute.FirstAncestorOrSelf<ClassDeclarationSyntax>();
                if (classDeclaration is not null)
                {
                    var adjustTypeParametersAction = CodeAction.Create(
                        title: diagnostic.Descriptor.CodeFixMessage(),
                        createChangedDocument: c => AdjustTypeParametersAsync(context.Document, root, attribute, classDeclaration, semanticModel, c),
                        equivalenceKey: diagnostic.Descriptor.Id);

                    context.RegisterCodeFix(adjustTypeParametersAction, diagnostic);
                }
            }

            var removeAttributeAction = CodeAction.Create(
                title: diagnostic.Descriptor.CodeFixMessage(1),
                createChangedDocument: c => RemoveAttributeAsync(context.Document, root, attribute, c),
                equivalenceKey: $"{diagnostic.Descriptor.Id}_Remove");

            context.RegisterCodeFix(removeAttributeAction, diagnostic);
        }
    }

    private static async Task<Document> AdjustTypeParametersAsync(
        Document document,
        SyntaxNode root,
        AttributeSyntax attribute,
        ClassDeclarationSyntax classDeclaration,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var referencedTypeSymbol = ExtractReferencedTypeSymbol(attribute, semanticModel);
        if (referencedTypeSymbol is null || !referencedTypeSymbol.IsGenericType)
        {
            return document;
        }

        var referencedTypeParameters = referencedTypeSymbol.TypeParameters;
        if (referencedTypeParameters.Length == 0)
        {
            return document;
        }

        // Find the source class declaration to copy constraint trivia from
        var sourceClassDeclaration = await FindSourceClassDeclarationAsync(document, referencedTypeSymbol, cancellationToken);

        var newClassDeclaration = AdjustClassTypeParameters(classDeclaration, referencedTypeParameters, sourceClassDeclaration);
        var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);

        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<ClassDeclarationSyntax?> FindSourceClassDeclarationAsync(
        Document document,
        INamedTypeSymbol typeSymbol,
        CancellationToken cancellationToken)
    {
        foreach (var location in typeSymbol.Locations)
        {
            if (location.SourceTree is null)
            {
                continue;
            }

            var sourceDocument = document.Project.GetDocument(location.SourceTree);
            if (sourceDocument is null)
            {
                continue;
            }

            var sourceRoot = await sourceDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (sourceRoot is null)
            {
                continue;
            }

            var node = sourceRoot.FindNode(location.SourceSpan);
            var classDecl = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDecl is not null)
            {
                return classDecl;
            }
        }

        return null;
    }

    private static INamedTypeSymbol? ExtractReferencedTypeSymbol(AttributeSyntax attribute, SemanticModel semanticModel)
    {
        return SyntaxHelpers.ExtractTypeFromAttribute(attribute, semanticModel);
    }

    private static ClassDeclarationSyntax AdjustClassTypeParameters(
        ClassDeclarationSyntax classDeclaration,
        ImmutableArray<ITypeParameterSymbol> targetTypeParameters,
        ClassDeclarationSyntax? sourceClassDeclaration)
    {
        var typeParameterList = SyntaxFactory.TypeParameterList(
            SyntaxFactory.SeparatedList(
                targetTypeParameters.Select(tp => SyntaxFactory.TypeParameter(tp.Name))
            )
        );

        if (classDeclaration.TypeParameterList is not null)
        {
            typeParameterList = typeParameterList.WithTriviaFrom(classDeclaration.TypeParameterList);
        }

        // Remove existing constraint clauses and their trivia
        var newClassDeclaration = classDeclaration
            .WithTypeParameterList(typeParameterList)
            .WithConstraintClauses(default);

        // Copy constraint clauses with trivia from source class if available
        if (sourceClassDeclaration is not null && sourceClassDeclaration.ConstraintClauses.Count > 0)
        {
            // Transfer trailing trivia from source base list to target base list
            // This includes the newline/space before the first 'where' clause
            if (sourceClassDeclaration.BaseList is not null && newClassDeclaration.BaseList is not null)
            {
                var sourceBaseListTrailingTrivia = sourceClassDeclaration.BaseList.GetTrailingTrivia();
                var newBaseList = newClassDeclaration.BaseList.WithTrailingTrivia(sourceBaseListTrailingTrivia);
                newClassDeclaration = newClassDeclaration.WithBaseList(newBaseList);
            }

            // Copy constraint clauses as-is to preserve exact formatting
            var normalizedClauses = NormalizeConstraintClauses(sourceClassDeclaration.ConstraintClauses);
            newClassDeclaration = newClassDeclaration.WithConstraintClauses(normalizedClauses);
        }
        else
        {
            // Fallback: generate constraints from symbols without preserved trivia
            var constraintClauses = targetTypeParameters
                .Where(tp => tp.HasReferenceTypeConstraint || tp.HasValueTypeConstraint ||
                            tp.HasConstructorConstraint || tp.ConstraintTypes.Length > 0)
                .Select(tp => CreateConstraintClause(tp, null))
                .ToList();

            if (constraintClauses.Any())
            {
                newClassDeclaration = newClassDeclaration.WithConstraintClauses(
                    SyntaxFactory.List(constraintClauses)
                );
            }
        }

        return newClassDeclaration;
    }

    private static SyntaxList<TypeParameterConstraintClauseSyntax> NormalizeConstraintClauses(
        SyntaxList<TypeParameterConstraintClauseSyntax> sourceClauses)
    {
        if (sourceClauses.Count == 0)
        {
            return sourceClauses;
        }

        // Simply return the source clauses as-is to preserve exact formatting
        return sourceClauses;
    }

    private static TypeParameterConstraintClauseSyntax CreateConstraintClause(ITypeParameterSymbol typeParameter, ClassDeclarationSyntax? sourceClass)
    {
        var constraints = new List<TypeParameterConstraintSyntax>();

        if (typeParameter.HasReferenceTypeConstraint)
        {
            constraints.Add(SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint));
        }

        if (typeParameter.HasValueTypeConstraint)
        {
            constraints.Add(SyntaxFactory.ClassOrStructConstraint(SyntaxKind.StructConstraint));
        }

        foreach (var constraintType in typeParameter.ConstraintTypes)
        {
            var typeSyntax = SyntaxFactory.ParseTypeName(constraintType.ToDisplayString());
            constraints.Add(SyntaxFactory.TypeConstraint(typeSyntax));
        }

        if (typeParameter.HasConstructorConstraint)
        {
            constraints.Add(SyntaxFactory.ConstructorConstraint());
        }

        var clause = SyntaxFactory.TypeParameterConstraintClause(
            SyntaxFactory.IdentifierName(typeParameter.Name),
            SyntaxFactory.SeparatedList(constraints)
        );

        if (sourceClass is not null && sourceClass.ConstraintClauses.Count > 0)
        {
            clause = clause.WithTriviaFrom(sourceClass.ConstraintClauses[0]);
        }

        return clause;
    }

    private static Task<Document> RemoveAttributeAsync(Document document, SyntaxNode root, AttributeSyntax attribute, CancellationToken cancellationToken)
    {
        var newRoot = SyntaxHelpers.RemoveAttributeFromClass(root, attribute);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}

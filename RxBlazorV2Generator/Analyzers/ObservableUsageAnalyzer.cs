using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RxBlazorV2Generator.Diagnostics;
using System.Collections.Immutable;

namespace RxBlazorV2Generator.Analyzers;

/// <summary>
/// Analyzer that warns when the Observable property is accessed directly in user code.
/// Direct access to Observable bypasses the framework's reactive patterns and should
/// be replaced with attributes like [ObservableTrigger], [ObservableCommand], or [ObservableComponentTrigger].
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ObservableUsageAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DiagnosticDescriptors.DirectObservableAccessWarning];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        context.RegisterSyntaxNodeAction(AnalyzeIdentifier, SyntaxKind.IdentifierName);
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;

        // Fast path: Check if accessing "Observable" property
        if (memberAccess.Name.Identifier.Text != "Observable")
        {
            return;
        }

        // Skip generated files
        if (IsGeneratedCode(context))
        {
            return;
        }

        // Get the symbol for the member access
        var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess);
        if (symbolInfo.Symbol is not IPropertySymbol propertySymbol)
        {
            return;
        }

        // Verify the property's containing type is ObservableModel (where Observable is declared)
        if (propertySymbol.ContainingType is not INamedTypeSymbol containingType)
        {
            return;
        }

        // The Observable property is declared on ObservableModel base class
        if (containingType.Name != "ObservableModel")
        {
            return;
        }

        // Verify it's the Observable property returning Observable<string[]>
        if (!IsObservablePropertyType(propertySymbol))
        {
            return;
        }

        // Get the containing member name for the error message
        var containingMemberName = GetContainingMemberName(memberAccess);

        // Report the diagnostic
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DirectObservableAccessWarning,
            memberAccess.GetLocation(),
            containingMemberName));
    }

    private static bool IsGeneratedCode(SyntaxNodeAnalysisContext context)
    {
        var filePath = context.Node.SyntaxTree.FilePath;

        // Skip .g.cs files (source-generated)
        if (filePath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Skip files in obj directory
        if (filePath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
            filePath.Contains("/obj/"))
        {
            return true;
        }

        // Skip test files (common patterns)
        if (filePath.Contains("Tests") || filePath.Contains("Test"))
        {
            return true;
        }

        return false;
    }

    private static bool IsObservablePropertyType(IPropertySymbol propertySymbol)
    {
        // Check if the property type is Observable<T> from R3
        // We're already checking that the property is named "Observable" and comes from ObservableModel
        // so we just need to verify it's a generic Observable type
        var propertyType = propertySymbol.Type;

        if (propertyType is not INamedTypeSymbol namedType)
        {
            return false;
        }

        // Check for R3.Observable<T> (generic type named Observable)
        return namedType.Name == "Observable" && namedType.IsGenericType;
    }

    private static void AnalyzeIdentifier(SyntaxNodeAnalysisContext context)
    {
        var identifier = (IdentifierNameSyntax)context.Node;

        // Fast path: Check if accessing "Observable" property
        if (identifier.Identifier.Text != "Observable")
        {
            return;
        }

        // Skip if this is part of a member access expression (handled by AnalyzeMemberAccess)
        if (identifier.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == identifier)
        {
            return;
        }

        // Skip generated files
        if (IsGeneratedCode(context))
        {
            return;
        }

        // Get the symbol for the identifier
        var symbolInfo = context.SemanticModel.GetSymbolInfo(identifier);
        if (symbolInfo.Symbol is not IPropertySymbol propertySymbol)
        {
            return;
        }

        // Verify the property's containing type is ObservableModel (where Observable is declared)
        if (propertySymbol.ContainingType is not INamedTypeSymbol containingType)
        {
            return;
        }

        // The Observable property is declared on ObservableModel base class
        if (containingType.Name != "ObservableModel")
        {
            return;
        }

        // Verify it's the Observable property returning Observable<string[]>
        if (!IsObservablePropertyType(propertySymbol))
        {
            return;
        }

        // Get the containing member name for the error message
        var containingMemberName = GetContainingMemberName(identifier);

        // Report the diagnostic
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DirectObservableAccessWarning,
            identifier.GetLocation(),
            containingMemberName));
    }

    private static string GetContainingMemberName(SyntaxNode node)
    {
        // Walk up the tree to find the containing method, property, or class
        var current = node.Parent;
        while (current is not null)
        {
            switch (current)
            {
                case MethodDeclarationSyntax method:
                    return $"method '{method.Identifier.ValueText}'";
                case PropertyDeclarationSyntax property:
                    return $"property '{property.Identifier.ValueText}'";
                case ConstructorDeclarationSyntax constructor:
                    return $"constructor of '{constructor.Identifier.ValueText}'";
                case LocalFunctionStatementSyntax localFunc:
                    return $"local function '{localFunc.Identifier.ValueText}'";
                case LambdaExpressionSyntax:
                    return "lambda expression";
                case ClassDeclarationSyntax classDecl:
                    return $"class '{classDecl.Identifier.ValueText}'";
            }

            current = current.Parent;
        }

        return "unknown location";
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RxBlazorV2Generator.Analysis;
using RxBlazorV2Generator.Diagnostics;
using RxBlazorV2Generator.Extensions;
using System.Collections.Immutable;

namespace RxBlazorV2Generator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RxBlazorDiagnosticAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor[] AllDiagnostics =
    [
        DiagnosticDescriptors.ObservableModelAnalysisError,
        DiagnosticDescriptors.CodeGenerationError,
        DiagnosticDescriptors.MethodAnalysisWarning,
        DiagnosticDescriptors.CircularModelReferenceError,
        DiagnosticDescriptors.InvalidModelReferenceTargetError,
        DiagnosticDescriptors.UnusedModelReferenceError,
        DiagnosticDescriptors.TriggerTypeArgumentsMismatchError,
        DiagnosticDescriptors.CircularTriggerReferenceError,
        DiagnosticDescriptors.CommandMethodReturnsValueError,
        DiagnosticDescriptors.CommandMethodMissingReturnValueError,
        DiagnosticDescriptors.GenericArityMismatchError,
        DiagnosticDescriptors.TypeConstraintMismatchError,
        DiagnosticDescriptors.InvalidOpenGenericReferenceError,
        DiagnosticDescriptors.InvalidInitPropertyError,
        DiagnosticDescriptors.DerivedModelReferenceError,
        DiagnosticDescriptors.MissingObservableModelScopeWarning,
        DiagnosticDescriptors.NonPublicPartialConstructorError,
        DiagnosticDescriptors.ObservableEntityMissingPartialModifierError
        // NOTE: RXBG041 (UnusedObservableComponentTriggerWarning), RXBG050 (UnregisteredServiceWarning),
        // RXBG051 (DiServiceScopeViolationError), RXBG052 (ReferencedModelDifferentAssemblyError),
        // RXBG060 (DirectObservableComponentInheritanceError), and RXBG014 (SharedModelNotSingletonError)
        // are reported by generator, not analyzer (require cross-model analysis)
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(AllDiagnostics);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeObservableModelClass, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeObservableModelClass(SyntaxNodeAnalysisContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;

        // Early filter: must have base types
        if (classDecl.BaseList?.Types.Any() != true)
            return;

        try
        {
            var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl);
            if (classSymbol is not INamedTypeSymbol namedTypeSymbol)
                return;

            // Check if this is an ObservableModel class
            if (!namedTypeSymbol.InheritsFromObservableModel())
                return;

            // Create ObservableModelRecord - single source of truth for analysis
            // Note: Passing null for serviceClasses is acceptable for analyzer -
            // it will report RXBG020 for unregistered services as expected
            var record = ObservableModelRecord.Create(
                classDecl,
                context.SemanticModel,
                context.Compilation,
                serviceClasses: null);

            if (record == null)
                return;

            // Report all diagnostics found
            foreach (var diagnostic in record.Verify())
            {
                context.ReportDiagnostic(diagnostic);
            }
        }
        catch (Exception ex)
        {
            // Report analysis error if something goes wrong
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.ObservableModelAnalysisError,
                classDecl.Identifier.GetLocation(),
                classDecl.Identifier.ValueText,
                ex.Message);

            context.ReportDiagnostic(diagnostic);
        }
    }

}

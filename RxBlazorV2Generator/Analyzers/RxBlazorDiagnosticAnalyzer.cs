using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RxBlazorV2Generator.Analysis;
using RxBlazorV2Generator.Diagnostics;
using RxBlazorV2Generator.Models;
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
        DiagnosticDescriptors.SharedModelNotSingletonError,
        DiagnosticDescriptors.TriggerTypeArgumentsMismatchError,
        DiagnosticDescriptors.CircularTriggerReferenceError,
        DiagnosticDescriptors.GenericArityMismatchError,
        DiagnosticDescriptors.TypeConstraintMismatchError,
        DiagnosticDescriptors.InvalidOpenGenericReferenceError,
        DiagnosticDescriptors.InvalidInitPropertyError,
        DiagnosticDescriptors.DerivedModelReferenceError
        // NOTE: RXBG020 (UnregisteredServiceWarning), RXBG021 (DiServiceScopeViolationError),
        // and RXBG022 (DirectObservableComponentInheritanceError) are reported by generator, not analyzer
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

        // Only analyze classes that might be ObservableModel classes
        if (!ObservableModelAnalyzer.IsObservableModelClass(classDecl))
            return;

        try
        {
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

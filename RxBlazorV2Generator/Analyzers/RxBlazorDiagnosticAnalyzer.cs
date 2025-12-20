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
    // Use unified registry from DiagnosticDescriptors - Single Source of Truth
    private static readonly HashSet<string> GeneratorReportedIds = DiagnosticDescriptors.GetGeneratorReportedIds();

    // All diagnostics must be in SupportedDiagnostics for code fixes to work,
    // but only analyzer-reported ones are actually reported by this analyzer
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.AllDefinitions.Select(d => d.Descriptor).ToArray());

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

            // Deduplicate: Only analyze on the FIRST partial declaration to avoid duplicate diagnostics
            // For multi-partial classes, the analyzer runs for each ClassDeclarationSyntax,
            // but ObservableModelRecord.Create analyzes ALL partials. Without this check,
            // diagnostics would be reported multiple times (once per partial file).
            // Note: Use location-based comparison since GetSyntax() creates new objects
            var firstRef = namedTypeSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (firstRef is not null)
            {
                var currentLocation = classDecl.GetLocation();
                var firstLocation = firstRef.GetSyntax().GetLocation();
                if (!currentLocation.Equals(firstLocation))
                {
                    return; // Skip secondary partial declarations
                }
            }

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

            // Report only analyzer-designated diagnostics (SSOT pattern from DiagnosticDescriptors)
            foreach (var diagnostic in record.Verify())
            {
                if (!GeneratorReportedIds.Contains(diagnostic.Id))
                {
                    context.ReportDiagnostic(diagnostic);
                }
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

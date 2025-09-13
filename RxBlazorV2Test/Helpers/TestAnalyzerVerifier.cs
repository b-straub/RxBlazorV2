using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using RxBlazorV2Generator;

namespace RxBlazorV2Test.Helpers;

/// <summary>
/// Test helper for compilation-end diagnostics that cannot be easily code-fixed
/// </summary>
internal static class CSharpAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public static DiagnosticResult Diagnostic()
        => new DiagnosticResult();

    public static DiagnosticResult Diagnostic(string diagnosticId)
        => new DiagnosticResult(diagnosticId, DiagnosticSeverity.Error);

    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        => new(descriptor);

    public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new AnalyzerTest<TAnalyzer> { TestCode = source };
        test.TestState.Sources.Add(("GlobalUsings.cs", SourceText.From(TestShared.GlobalUsing, Encoding.UTF8)));
        test.TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }
}

internal class AnalyzerTest<TAnalyzer> : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public AnalyzerTest()
    {
        ReferenceAssemblies = TestShared.ReferenceAssemblies();

        SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId)!;
            // external references
            project = project.AddMetadataReference(MetadataReference.CreateFromFile(typeof(RxBlazorV2.Model.ObservableModel).Assembly.Location));
            // parse options
            project = project.WithParseOptions(new CSharpParseOptions(
                languageVersion: LanguageVersion.Preview,
                preprocessorSymbols: ["DEBUG"]));
            
            // Add compilation options for proper analyzer/generator behavior
            project = project.WithCompilationOptions(project.CompilationOptions!
                .WithSpecificDiagnosticOptions(project.CompilationOptions.SpecificDiagnosticOptions
                    .Add("RXBG010", ReportDiagnostic.Error) // SharedModelNotSingletonError
                    .Add("RXBG011", ReportDiagnostic.Error) // ModelReferenceNotRegisteredError
                    .Add("RXBG012", ReportDiagnostic.Error) // TriggerTypeArgumentsMismatchError
                    .Add("RXBG013", ReportDiagnostic.Error))); // CircularTriggerReferenceError
            
            return project.Solution;
        });
    }

    protected override IEnumerable<Type> GetSourceGenerators()
    {
        return [typeof(RxBlazorGenerator)];
    }
}
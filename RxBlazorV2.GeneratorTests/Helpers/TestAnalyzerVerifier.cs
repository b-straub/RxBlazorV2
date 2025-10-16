using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using RxBlazorV2Generator;
using RxBlazorV2Generator.Diagnostics;

namespace RxBlazorV2.GeneratorTests.Helpers;

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

    public static Task VerifyAnalyzerAsync(string source)
        => VerifyAnalyzerAsync(source, []);
    
    public static Task VerifyAnalyzerAsync(string source, DiagnosticResult expected)
        => VerifyAnalyzerAsync(source, [expected]);
    
    public static Task VerifyAnalyzerAsync(string source, DiagnosticResult expected, params string[] skippedDiagnosticIds)
        => VerifyAnalyzerAsync(source, [expected], skippedDiagnosticIds);

    public static Task VerifyAnalyzerAsync(string source, DiagnosticResult[] expected, params string[] skippedDiagnosticIds)
    {
        var skippedDiagnosticIdsMerged = skippedDiagnosticIds.Append(DiagnosticDescriptors.GeneratorDiagnosticError.Id);
        
        var test = new AnalyzerTest<TAnalyzer>
        {
            TestCode = source,
            SkippedDiagnosticIds = skippedDiagnosticIdsMerged.ToArray()
        };
        
        test.TestState.Sources.Add(("GlobalUsings.cs", SourceText.From(TestShared.GlobalUsing, Encoding.UTF8)));
        test.TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;
        test.ExpectedDiagnostics.AddRange(expected);
        test.DisabledDiagnostics.Add(DiagnosticDescriptors.GeneratorDiagnosticError.Id);
        return test.RunAsync();
    }
}

internal class AnalyzerTest<TAnalyzer> : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public string[] SkippedDiagnosticIds { get; init; } = [];
    
    public AnalyzerTest()
    {
        ReferenceAssemblies = TestShared.ReferenceAssemblies();

        SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId)!;
            // external references
            project = project.AddMetadataReference(MetadataReference.CreateFromFile(typeof(Model.ObservableModel).Assembly.Location));
            // parse options
            project = project.WithParseOptions(new CSharpParseOptions(
                languageVersion: LanguageVersion.Preview,
                preprocessorSymbols: ["DEBUG"]));
            
            return project.Solution;
        });
    }

    protected override IEnumerable<Type> GetSourceGenerators()
    {
        return [typeof(RxBlazorGenerator)];
    }
    
    protected override bool IsCompilerDiagnosticIncluded(Diagnostic diagnostic, CompilerDiagnostics compilerDiagnostics)
    {
        return !SkippedDiagnosticIds.Contains(diagnostic.Id) && base.IsCompilerDiagnosticIncluded(diagnostic, compilerDiagnostics);
    }
}
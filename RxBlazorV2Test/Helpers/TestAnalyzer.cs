using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using RxBlazorV2Generator;

namespace RxBlazorV2Test.Helpers;

internal static class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public static DiagnosticResult Diagnostic()
        => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic();

    public static DiagnosticResult Diagnostic(string diagnosticId)
        => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(diagnosticId);

    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        => new(descriptor);

    public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new Test<TAnalyzer, TCodeFix> { TestCode = source };
        test.TestState.Sources.Add(("GlobalUsings.cs", SourceText.From(GlobalUsing, Encoding.UTF8)));
        test.TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    public static Task VerifyCodeFixAsync(string source, string fixedSource, int? codeActionIndex = null)
        => VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource, codeActionIndex);

    public static Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource, int? codeActionIndex = null)
        => VerifyCodeFixAsync(source, [expected], fixedSource, codeActionIndex);

    private static Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource, int? codeActionIndex = null)
    {
        var test = new Test<TAnalyzer, TCodeFix>
        {
            TestCode = source,
            FixedCode = fixedSource,
            CodeActionIndex = codeActionIndex,
            CodeFixTestBehaviors = codeActionIndex is not null ? CodeFixTestBehaviors.SkipFixAllCheck : CodeFixTestBehaviors.None
        };
        
        test.TestState.Sources.Add(("GlobalUsings.cs", SourceText.From(GlobalUsing, Encoding.UTF8)));
        test.FixedState.Sources.Add(("GlobalUsings.cs", SourceText.From(GlobalUsing, Encoding.UTF8)));
        test.TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    private static string GlobalUsing =>
        """
        global using global::System;
        global using global::System.Collections.Generic;
        global using global::System.IO;
        global using global::System.Linq;
        global using global::System.Net.Http;
        global using global::System.Threading;
        global using global::System.Threading.Tasks;
        """;
}

internal class Test<TAnalyzer, TCodeFix> : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public Test()
    {
        ReferenceAssemblies = ReferenceAssemblies.Net.Net90
            .AddPackages([
                new PackageIdentity("Microsoft.Net.Compilers.Toolset", "4.14.0"), // Use the latest version of the compiler toolset
                new PackageIdentity("Microsoft.Extensions.DependencyInjection", "9.0.6"),
                new PackageIdentity("Microsoft.AspNetCore.Components", "9.0.6"),
                new PackageIdentity("R3", "1.3.0")
            ]); 

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
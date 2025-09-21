using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using RxBlazorV2Generator;

namespace RxBlazorV2Test.Helpers;

/// <summary>
/// Test helper for compilation-end diagnostics that cannot be easily code-fixed
/// </summary>
internal static class RxBlazorGeneratorVerifier
{
    public static Task VerifySourceGeneratorAsync(string source, string generated, string modelName,
        params DiagnosticResult[] expected)
    {
        var test = new RxBlazorGeneratorTest { TestCode = source };
        test.TestState.Sources.Add(("GlobalUsings.cs", SourceText.From(TestShared.GlobalUsing, Encoding.UTF8)));

        test.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), $"Test.{modelName}.g.cs",
            SourceText.From(generated.TrimStart(), Encoding.UTF8)));
        test.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), "ObservableModelsServiceCollectionExtension.g.cs",
                SourceText.From(RxBlazorGeneratorTest.ObservableModelsServiceExtension(modelName), Encoding.UTF8))
        );
        test.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), "GenericModelsServiceCollectionExtension.g.cs",
                SourceText.From(RxBlazorGeneratorTest.GenricModelsServiceExtension(modelName), Encoding.UTF8))
        );
        test.ExpectedDiagnostics.AddRange(expected);
        test.TestState.ReferenceAssemblies = TestShared.ReferenceAssemblies();
        test.TestState.AdditionalReferences.Add(typeof(RxBlazorV2.Model.ObservableModel).Assembly);

        return test.RunAsync();
    }
}

internal class RxBlazorGeneratorTest : CSharpSourceGeneratorTest<RxBlazorGenerator, DefaultVerifier>
{
    protected override ParseOptions CreateParseOptions()
    {
        return new CSharpParseOptions(
            languageVersion: LanguageVersion.Preview);
    }


    public static string ObservableModelsServiceExtension(string modelName)
    {
        var serviceExtension = @$"
using Microsoft.Extensions.DependencyInjection;
using RxBlazorV2.Model;
using Test;

namespace Test;

public static partial class TestServices
{{
    public static IServiceCollection Initialize(IServiceCollection services)
    {{
        services.AddSingleton<{modelName}>();
        return services;
    }}
}}";
        serviceExtension = serviceExtension.TrimStart();
        serviceExtension = serviceExtension.Replace("\r\n", Environment.NewLine);
        serviceExtension = serviceExtension += Environment.NewLine;
        return serviceExtension;
    }
    
    public static string GenricModelsServiceExtension(string modelName)
    {
        var serviceExtension = @$"
using Microsoft.Extensions.DependencyInjection;
using RxBlazorV2.Model;
using Test;

namespace Test;

public static partial class TestServices
{{
}}";
        serviceExtension = serviceExtension.TrimStart();
        serviceExtension = serviceExtension.Replace("\r\n", Environment.NewLine);
        serviceExtension = serviceExtension += Environment.NewLine;
        return serviceExtension;
    }
}
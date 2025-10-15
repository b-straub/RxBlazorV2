using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using RxBlazorV2Generator;

namespace RxBlazorV2.GeneratorTests.Helpers;

/// <summary>
/// Test helper for compilation-end diagnostics that cannot be easily code-fixed
/// </summary>
internal static class RxBlazorGeneratorVerifier
{
    public static Task VerifySourceGeneratorAsync(string source, string generated, string modelName, string constrains,
        params DiagnosticResult[] expected)
    {
        var genericSplit = modelName.IndexOf('<');
        var modelFileName = genericSplit > 0 ? modelName[..genericSplit] : modelName;
        
        var test = new RxBlazorGeneratorTest { TestCode = source };
        test.TestState.Sources.Add(("GlobalUsings.cs", SourceText.From(TestShared.GlobalUsing, Encoding.UTF8)));

        test.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), $"Test.{modelFileName}.g.cs",
            SourceText.From(generated.NormalizeGeneratedCode(), Encoding.UTF8)));
        test.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), "ObservableModelsServiceCollectionExtension.g.cs",
                SourceText.From(RxBlazorGeneratorTest.ObservableModelsServiceExtension(modelName, constrains), Encoding.UTF8))
        );
        test.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), "GenericModelsServiceCollectionExtension.g.cs",
                SourceText.From(RxBlazorGeneratorTest.GenricModelsServiceExtension(modelName, constrains), Encoding.UTF8))
        );
        test.ExpectedDiagnostics.AddRange(expected);
        test.TestState.ReferenceAssemblies = TestShared.ReferenceAssemblies();
        test.TestState.AdditionalReferences.Add(typeof(RxBlazorV2.Model.ObservableModel).Assembly);

        return test.RunAsync();
    }
}

/// <summary>
/// Extension methods for test helpers
/// </summary>
internal static class TestGeneratorExtensions
{
    public static string NormalizeGeneratedCode(this string generated)
    {
        generated = generated.TrimStart();
        generated = generated.Replace("\r\n", Environment.NewLine);
        return generated;
    }
}

/// <summary>
/// Verifier for component generation tests
/// Verifies both model and component generation together since they happen in the same pass
/// </summary>
internal static class ComponentGeneratorVerifier
{
    public static Task VerifyComponentGeneratorAsync(string source, string generatedModel, string generatedComponent,
        string modelName, string componentClassName, string constrains = "",
        params DiagnosticResult[] expected)
    {
        var genericSplit = modelName.IndexOf('<');
        var modelFileName = genericSplit > 0 ? modelName[..genericSplit] : modelName;

        var test = new RxBlazorGeneratorTest { TestCode = source };
        test.TestState.Sources.Add(("GlobalUsings.cs", SourceText.From(TestShared.GlobalUsing, Encoding.UTF8)));

        // Add the expected model generation output
        test.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), $"Test.{modelFileName}.g.cs",
            SourceText.From(generatedModel.NormalizeGeneratedCode(), Encoding.UTF8)));

        // Add the expected component generation output
        // Component namespace is always {RootNamespace}.Components, so for "Test" it becomes "Test.Components"
        // Relative namespace is "Components"
        test.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), $"Components.{componentClassName}.g.cs",
            SourceText.From(generatedComponent.NormalizeGeneratedCode(), Encoding.UTF8)));

        // Add service extension files
        test.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), "ObservableModelsServiceCollectionExtension.g.cs",
                SourceText.From(RxBlazorGeneratorTest.ObservableModelsServiceExtension(modelName, constrains), Encoding.UTF8))
        );
        test.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), "GenericModelsServiceCollectionExtension.g.cs",
                SourceText.From(RxBlazorGeneratorTest.GenricModelsServiceExtension(modelName, constrains), Encoding.UTF8))
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


    public static string ObservableModelsServiceExtension(string modelName, string constrains)
    {
        var serviceExtension = $$"""

        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Model;
        using Test;

        namespace Global;

        public static partial class ObservableModels
        {
            public static IServiceCollection Initialize(IServiceCollection services)
            {
                services.AddSingleton<{{modelName}}>();
                return services;
            }
        }
        """;
        
        serviceExtension = serviceExtension.TrimStart();
        serviceExtension = serviceExtension.Replace("\r\n", Environment.NewLine);
        serviceExtension += Environment.NewLine;
        
        var serviceExtensionGeneric = $$"""

        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Model;
        using Test;

        namespace Global;

        public static partial class ObservableModels
        {
            public static IServiceCollection Initialize(IServiceCollection services)
            {
                return services;
            }
        }
        """;
        serviceExtensionGeneric = serviceExtensionGeneric.TrimStart();
        serviceExtensionGeneric = serviceExtensionGeneric.Replace("\r\n", Environment.NewLine);
        serviceExtensionGeneric += Environment.NewLine;
        return constrains.Length > 0 ? serviceExtensionGeneric : serviceExtension;
    }
    
    public static string GenricModelsServiceExtension(string modelName, string constrains)
    {
        var serviceExtension = $$"""

        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Model;
        using Test;

        namespace Global;

        public static partial class ObservableModels
        {
        }
        """;
        serviceExtension = serviceExtension.TrimStart();
        serviceExtension = serviceExtension.Replace("\r\n", Environment.NewLine);
        serviceExtension += Environment.NewLine;
        
        var serviceExtensionGeneric = $$"""

        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Model;
        using Test;

        namespace Global;

        public static partial class ObservableModels
        {
            public static IServiceCollection {{modelName}}(IServiceCollection services)
                {{constrains}}
            {
                services.AddSingleton<{{modelName}}>();
                return services;
            }

        }
        """;
        serviceExtensionGeneric = serviceExtensionGeneric.TrimStart();
        serviceExtensionGeneric = serviceExtensionGeneric.Replace("\r\n", Environment.NewLine);
        serviceExtensionGeneric += Environment.NewLine;
        
        return constrains.Length > 0 ? serviceExtensionGeneric : serviceExtension;
    }
}
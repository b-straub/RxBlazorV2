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
        test.TestState.AdditionalReferences.Add(typeof(Model.ObservableModel).Assembly);

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
        // Component namespace is same as model namespace
        test.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), $"Test.{componentClassName}.g.cs",
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
        test.TestState.AdditionalReferences.Add(typeof(Model.ObservableModel).Assembly);

        return test.RunAsync();
    }
}

/// <summary>
/// Verifier for Razor file diagnostics
/// Supports testing diagnostics that require Razor files (like DirectObservableComponentInheritanceError and SharedModelNotSingletonError)
/// </summary>
internal static class RazorFileGeneratorVerifier
{
    public static Task VerifyRazorDiagnosticsAsync(
        string source,
        Dictionary<string, string> razorFiles,
        string? generatedModel = null,
        string? generatedComponent = null,
        string? modelName = null,
        string? componentClassName = null,
        string constrains = "",
        string modelScope = "Scoped",
        Dictionary<string, string>? additionalGeneratedSources = null,
        params DiagnosticResult[] expected)
    {
        var test = new RxBlazorGeneratorTest { TestCode = source };
        test.TestState.Sources.Add(("GlobalUsings.cs", SourceText.From(TestShared.GlobalUsing, Encoding.UTF8)));

        // Add Razor files as AdditionalFiles
        foreach (var (fileName, content) in razorFiles)
        {
            test.TestState.AdditionalFiles.Add((fileName, SourceText.From(content, Encoding.UTF8)));
        }

        // Add expected generated sources if provided
        // Order matters: Model -> Component -> Razor Code-Behind -> Service Extensions
        if (generatedModel is not null && modelName is not null)
        {
            var genericSplit = modelName.IndexOf('<');
            var modelFileName = genericSplit > 0 ? modelName[..genericSplit] : modelName;

            test.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), $"Test.{modelFileName}.g.cs",
                SourceText.From(generatedModel.NormalizeGeneratedCode(), Encoding.UTF8)));
        }

        // Add component file before service extensions
        if (generatedComponent is not null && componentClassName is not null)
        {
            test.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), $"Test.{componentClassName}.g.cs",
                SourceText.From(generatedComponent.NormalizeGeneratedCode(), Encoding.UTF8)));
        }

        // Add any additional generated sources (e.g., razor code-behind files) before service extensions
        if (additionalGeneratedSources is not null)
        {
            foreach (var (fileName, content) in additionalGeneratedSources)
            {
                test.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), fileName,
                    SourceText.From(content.NormalizeGeneratedCode(), Encoding.UTF8)));
            }
        }

        // Add service extension files last
        if (generatedModel is not null && modelName is not null)
        {
            test.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), "ObservableModelsServiceCollectionExtension.g.cs",
                    SourceText.From(RxBlazorGeneratorTest.ObservableModelsServiceExtension(modelName, constrains, modelScope), Encoding.UTF8))
            );
            test.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), "GenericModelsServiceCollectionExtension.g.cs",
                    SourceText.From(RxBlazorGeneratorTest.GenricModelsServiceExtension(modelName, constrains), Encoding.UTF8))
            );
        }

        test.ExpectedDiagnostics.AddRange(expected);
        test.TestState.ReferenceAssemblies = TestShared.ReferenceAssemblies();
        test.TestState.AdditionalReferences.Add(typeof(Model.ObservableModel).Assembly);

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


    public static string ObservableModelsServiceExtension(string modelName, string constrains, string modelScope = "Scoped")
    {
        var registrationMethod = modelScope switch
        {
            "Scoped" => "AddScoped",
            "Singleton" => "AddSingleton",
            "Transient" => "AddTransient",
            _ => "AddScoped"
        };

        var serviceExtension = $$"""

        using Microsoft.Extensions.DependencyInjection;
        using RxBlazorV2.Model;
        using Test;

        namespace Global;

        public static partial class ObservableModels
        {
            public static IServiceCollection Initialize(IServiceCollection services)
            {
                services.{{registrationMethod}}<{{modelName}}>();
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
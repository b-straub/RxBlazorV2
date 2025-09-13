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
        foreach (var att in test.GetGeneratedAttributes())
        {
            test.TestState.GeneratedSources.Add(att);
        }

        test.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), $"Test.{modelName}.g.cs",
            SourceText.From(generated.TrimStart(), Encoding.UTF8)));
        test.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), "ObservableModelServiceExtensions.g.cs",
                SourceText.From(RxBlazorGeneratorTest.ServiceExtension(modelName), Encoding.UTF8))
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

    public IEnumerable<(Type, string, SourceText)> GetGeneratedAttributes()
    {
        return
        [
            (typeof(RxBlazorGenerator), "ObservableCommandAttribute.g.cs",
                SourceText.From(GetEmbeddedResourceContent("ObservableCommandAttribute.cs"), Encoding.UTF8)),
            (typeof(RxBlazorGenerator), "ObservableCommandTriggerAttribute.g.cs",
                SourceText.From(GetEmbeddedResourceContent("ObservableCommandTriggerAttribute.cs"), Encoding.UTF8)),
            (typeof(RxBlazorGenerator), "ObservableCommandTriggerAttributeT.g.cs",
                SourceText.From(GetEmbeddedResourceContent("ObservableCommandTriggerAttributeT.cs"), Encoding.UTF8)),
            (typeof(RxBlazorGenerator), "ObservableModelReferenceAttribute.g.cs",
                SourceText.From(GetEmbeddedResourceContent("ObservableModelReferenceAttribute.cs"), Encoding.UTF8)),
            (typeof(RxBlazorGenerator), "ObservableModelScopeAttribute.g.cs",
                SourceText.From(GetEmbeddedResourceContent("ObservableModelScopeAttribute.cs"), Encoding.UTF8)),
        ];
    }

    private static string GetEmbeddedResourceContent(string fileName)
    {
        var assembly = typeof(RxBlazorGenerator).Assembly;
        var resourceName = $"RxBlazorV2Generator.Templates.{fileName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static string ServiceExtension(string modelName)
    {
        var serviceExtension = @$"
using Microsoft.Extensions.DependencyInjection;
using RxBlazorV2.Model;
using Test;

namespace Microsoft.Extensions.DependencyInjection;

public static class ObservableModelServiceCollectionExtensions
{{
    public static IServiceCollection AddObservableModels(this IServiceCollection services)
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
}
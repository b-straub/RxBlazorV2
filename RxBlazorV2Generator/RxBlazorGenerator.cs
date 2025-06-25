using Microsoft.CodeAnalysis;
using RxBlazorV2Generator.Analyzers;
using RxBlazorV2Generator.Generators;
using System.Reflection;
using RxBlazorV2Generator.Models;

namespace RxBlazorV2Generator;

[Generator]
public class RxBlazorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Read MSBuild properties for configuration
        var msbuildProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) =>
            {
                provider.GlobalOptions.TryGetValue("build_property.RxBlazorObservableUpdateFrequencyMs", out var updateFrequencyValue);
                return int.TryParse(updateFrequencyValue, out var frequency) ? frequency : 100; // Default to 100ms
            });

        // Register the ObservableCommandAttribute
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "ObservableCommandAttribute.g.cs",
            Microsoft.CodeAnalysis.Text.SourceText.From(GetEmbeddedResourceContent("ObservableCommandAttribute.cs"), System.Text.Encoding.UTF8)));

        // Register the ObservableCommandTriggerAttribute
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "ObservableCommandTriggerAttribute.g.cs",
            Microsoft.CodeAnalysis.Text.SourceText.From(GetEmbeddedResourceContent("ObservableCommandTriggerAttribute.cs"), System.Text.Encoding.UTF8)));
        
        // Register the ObservableCommandTriggerAttributeT
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "ObservableCommandTriggerAttributeT.g.cs",
            Microsoft.CodeAnalysis.Text.SourceText.From(GetEmbeddedResourceContent("ObservableCommandTriggerAttributeT.cs"), System.Text.Encoding.UTF8)));
        
        // Register the ObservableModelReferenceAttribute
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "ObservableModelReferenceAttribute.g.cs",
            Microsoft.CodeAnalysis.Text.SourceText.From(GetEmbeddedResourceContent("ObservableModelReferenceAttribute.cs"), System.Text.Encoding.UTF8)));

        // Register the ObservableModelScopeAttribute
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "ObservableModelScopeAttribute.g.cs",
            Microsoft.CodeAnalysis.Text.SourceText.From(GetEmbeddedResourceContent("ObservableModelScopeAttribute.cs"), System.Text.Encoding.UTF8)));

        // Analyze service registrations to detect DI services
        var serviceClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => ServiceAnalyzer.IsServiceRegistration(s),
                transform: static (ctx, _) => ServiceAnalyzer.GetServiceClassInfo(ctx))
            .Where(static m => m is not null)
            .Collect();


        // Single pass: analyze observable models with service information
        var observableModelClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => ObservableModelAnalyzer.IsObservableModelClass(s),
                transform: static (ctx, _) => ctx)
            .Combine(serviceClasses)
            .Select(static (combined, _) => 
            {
                var (syntaxContext, services) = combined;
                
                // Merge service info
                var mergedServices = new ServiceInfoList();
                foreach (var serviceList in services.Where(s => s != null))
                {
                    foreach (var service in serviceList!.Services)
                    {
                        mergedServices.AddService(service);
                    }
                }
                
                // Analyze model with service information (model cross-references work from attributes alone)
                return ObservableModelAnalyzer.GetObservableModelClassInfo(syntaxContext, mergedServices);
            })
            .Where(static m => m is not null);

        // Generate code for observable models
        context.RegisterSourceOutput(observableModelClasses.Combine(msbuildProvider),
            static (spc, combined) => 
            {
                var (modelInfo, updateFrequencyMs) = combined;
                if (modelInfo != null)
                {
                    ObservableModelCodeGenerator.GenerateObservableModelPartials(spc, modelInfo, updateFrequencyMs);
                }
            });

        // Register syntax provider for Razor code-behind files
        var razorCodeBehindClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => RazorAnalyzer.IsRazorCodeBehindClass(s),
                transform: static (ctx, _) => RazorAnalyzer.GetRazorCodeBehindInfo(ctx))
            .Where(static m => m is not null);

        // Register additional text provider for .razor files
        var razorFiles = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".razor"));

        // Combine razor files with code-behind classes and observable models
        var combinedRazorInfo = razorCodeBehindClasses
            .Combine(razorFiles.Collect())
            .Combine(observableModelClasses.Collect())
            .Select(static (combined, _) => 
            {
                try
                {
                    return RazorAnalyzer.AnalyzeRazorWithAdditionalTexts(combined.Left.Left, combined.Left.Right, combined.Right);
                }
                catch (Exception)
                {
                    // Log error but don't throw - return null to skip this item
                    return null;
                }
            });

        // Generate constructors for Razor code-behind classes
        context.RegisterSourceOutput(combinedRazorInfo.Combine(msbuildProvider),
            static (spc, combined) => 
            {
                var (source, updateFrequencyMs) = combined;
                if (source != null)
                {
                    RazorCodeGenerator.GenerateRazorConstructors(spc, source, updateFrequencyMs);
                }
            });

        // Generate AddObservableModels extension method
        context.RegisterSourceOutput(observableModelClasses.Collect(),
            static (spc, models) => 
            {
                ObservableModelCodeGenerator.GenerateAddObservableModelsExtension(spc, models.Where(m => m != null).ToArray()!);
            });

    }

    private static string GetEmbeddedResourceContent(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"RxBlazorV2Generator.Templates.{fileName}";
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        }
        
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
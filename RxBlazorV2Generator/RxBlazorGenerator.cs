using Microsoft.CodeAnalysis;
using RxBlazorV2Generator.Analyzers;
using RxBlazorV2Generator.Generators;
using RxBlazorV2Generator.Models;

namespace RxBlazorV2Generator;

public class GeneratorConfig
{
    public int UpdateFrequencyMs { get; }
    public string RootNamespace { get; }

    public GeneratorConfig(int updateFrequencyMs, string rootNamespace)
    {
        UpdateFrequencyMs = updateFrequencyMs;
        RootNamespace = rootNamespace;
    }
}

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
                var updateFrequency = int.TryParse(updateFrequencyValue, out var frequency) ? frequency : 100; // Default to 100ms

                // Get root namespace from project properties
                provider.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
                if (string.IsNullOrEmpty(rootNamespace))
                {
                    provider.GlobalOptions.TryGetValue("build_property.AssemblyName", out rootNamespace);
                }
                rootNamespace ??= "Global"; // Fallback if neither is available

                return new GeneratorConfig(updateFrequency, rootNamespace);
            });


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
                var (modelInfo, config) = combined;
                if (modelInfo != null)
                {
                    ObservableModelCodeGenerator.GenerateObservableModelPartials(spc, modelInfo, config.UpdateFrequencyMs);
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
                var (source, config) = combined;
                if (source != null)
                {
                    RazorCodeGenerator.GenerateRazorConstructors(spc, source, config.UpdateFrequencyMs);
                }
            });

        // Generate AddObservableModels extension method
        context.RegisterSourceOutput(observableModelClasses.Collect().Combine(msbuildProvider),
            static (spc, combined) =>
            {
                var (models, config) = combined;
                var nonNullModels = models.Where(m => m != null).Select(x => x!).ToArray();
                ObservableModelCodeGenerator.GenerateAddObservableModelsExtension(spc, nonNullModels, config.RootNamespace);
                ObservableModelCodeGenerator.GenerateAddGenericObservableModelsExtension(spc, nonNullModels, config.RootNamespace);
            });

    }

}
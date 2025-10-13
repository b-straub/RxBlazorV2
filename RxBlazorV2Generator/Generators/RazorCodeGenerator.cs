using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RxBlazorV2Generator.Models;
using RxBlazorV2Generator.Diagnostics;
using System.Collections.Immutable;
using System.Text;

namespace RxBlazorV2Generator.Generators;

public static class RazorCodeGenerator
{
    public static void GenerateRazorConstructors(SourceProductionContext context, RazorCodeBehindInfo razorInfo, ImmutableArray<ObservableModelInfo?> observableModels, int updateFrequencyMs = 100)
    {
        try
        {
            // Check for diagnostic issues first
            // Note: Diagnostic is reported by analyzer, we just skip code generation here
            if (razorInfo.HasDiagnosticIssue)
            {
                return; // Don't generate code for components with diagnostic issues
            }
            var sb = new StringBuilder();
        
        sb.AppendLine("using R3;");
        sb.AppendLine("using ObservableCollections;");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine($"namespace {razorInfo.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"public partial class {razorInfo.ClassName}");
        sb.AppendLine("{");
        
        var hasModelProperty = razorInfo.ObservableModelFields.Contains("Model");
        var isObservableComponent = hasModelProperty;
        
        if (isObservableComponent)
        {
            // ObservableComponent<T> pattern - generate subscriptions and lifecycle hooks
            // No constructor needed - Blazor handles DI automatically via [Inject] and @inject

            // Generate CompositeDisposable for subscription management
            if (razorInfo.FieldToPropertiesMap.Any())
            {
                sb.AppendLine("    protected CompositeDisposable Subscriptions { get; } = new();");
                sb.AppendLine();
            }

            // Generate InitializeGeneratedCode method (ALWAYS generated to satisfy abstract contract)
            sb.AppendLine("    protected override void InitializeGeneratedCode()");
            sb.AppendLine("    {");

            // Call ContextReady on injected models
            foreach (var injectedProp in razorInfo.InjectedProperties)
            {
                sb.AppendLine($"        {injectedProp}.ContextReady();");
            }

            // Set up subscriptions if any
            if (razorInfo.FieldToPropertiesMap.Any())
            {
                sb.AppendLine("        // Subscribe to model changes for component base model and other models from properties");

                foreach (var fieldProps in razorInfo.FieldToPropertiesMap)
                {
                    var fieldName = fieldProps.Key;
                    var properties = fieldProps.Value.Distinct().ToList();

                    var propertyList = new List<string>();
                    propertyList.AddRange(properties.Select(p => $"\"{p}\""));

                    // Add observable collection properties from the model
                    if (razorInfo.FieldToTypeMap.TryGetValue(fieldName, out var fieldType))
                    {
                        var modelInfo = observableModels.FirstOrDefault(m => m != null && m.FullyQualifiedName == fieldType);
                        if (modelInfo != null)
                        {
                            var observableCollectionProps = modelInfo.PartialProperties
                                .Where(p => p.IsObservableCollection)
                                .Select(p => $"\"{p.Name}\"");
                            propertyList.AddRange(observableCollectionProps);
                        }
                    }

                    var observedPropsArray = $"[{string.Join(", ", propertyList)}]";

                    sb.AppendLine($"        Subscriptions.Add({fieldName}.Observable.Where(p => p.Intersect({observedPropsArray}).Any())");
                    sb.AppendLine($"            .Chunk(TimeSpan.FromMilliseconds({updateFrequencyMs}))");
                    sb.AppendLine("            .Subscribe(chunks =>");
                    sb.AppendLine("            {");
                    sb.AppendLine("                InvokeAsync(StateHasChanged);");
                    sb.AppendLine("            }));");
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("    ");

            // Generate InitializeGeneratedCodeAsync method (ALWAYS generated to satisfy abstract contract)
            if (razorInfo.InjectedProperties.Any())
            {
                sb.AppendLine("    protected override async Task InitializeGeneratedCodeAsync()");
                sb.AppendLine("    {");
                foreach (var injectedProp in razorInfo.InjectedProperties)
                {
                    sb.AppendLine($"        await {injectedProp}.ContextReadyAsync();");
                }
                sb.AppendLine("    }");
            }
            else
            {
                sb.AppendLine("    protected override Task InitializeGeneratedCodeAsync()");
                sb.AppendLine("    {");
                sb.AppendLine("        return Task.CompletedTask;");
                sb.AppendLine("    }");
            }
            sb.AppendLine("    ");

            // Generate Dispose method
            if (razorInfo.FieldToPropertiesMap.Any())
            {
                sb.AppendLine("    protected override void Dispose(bool disposing)");
                sb.AppendLine("    {");
                sb.AppendLine("        OnDispose();");
                sb.AppendLine("        Subscriptions.Dispose();");
                sb.AppendLine("        base.Dispose(disposing);");
                sb.AppendLine("    }");
            }
        }
        else
        {
            // Non-generic ObservableComponent pattern - models are injected via @inject or [Inject]
            // Generate CompositeDisposable for subscription management
            if (razorInfo.FieldToPropertiesMap.Any())
            {
                sb.AppendLine("    protected CompositeDisposable Subscriptions { get; } = new();");
                sb.AppendLine();
            }

            // Generate InitializeGeneratedCode method (ALWAYS generated to satisfy abstract contract)
            sb.AppendLine("    protected override void InitializeGeneratedCode()");
            sb.AppendLine("    {");

            // Call ContextReady on injected models
            foreach (var injectedProp in razorInfo.InjectedProperties)
            {
                sb.AppendLine($"        {injectedProp}.ContextReady();");
            }

            // Set up subscriptions if any
            if (razorInfo.FieldToPropertiesMap.Any())
            {
                sb.AppendLine("        // Subscribe to model changes for injected models");

                foreach (var fieldProps in razorInfo.FieldToPropertiesMap)
                {
                    var fieldName = fieldProps.Key;
                    var properties = fieldProps.Value.Distinct().ToList();

                    var propertyList = new List<string>();
                    propertyList.AddRange(properties.Select(p => $"\"{p}\""));

                    // Add observable collection properties from the model
                    if (razorInfo.FieldToTypeMap.TryGetValue(fieldName, out var fieldType))
                    {
                        var modelInfo = observableModels.FirstOrDefault(m => m != null && m.FullyQualifiedName == fieldType);
                        if (modelInfo != null)
                        {
                            var observableCollectionProps = modelInfo.PartialProperties
                                .Where(p => p.IsObservableCollection)
                                .Select(p => $"\"{p.Name}\"");
                            propertyList.AddRange(observableCollectionProps);
                        }
                    }

                    var observedPropsArray = $"[{string.Join(", ", propertyList)}]";

                    sb.AppendLine($"        Subscriptions.Add({fieldName}.Observable.Where(p => p.Intersect({observedPropsArray}).Any())");
                    sb.AppendLine($"            .Chunk(TimeSpan.FromMilliseconds({updateFrequencyMs}))");
                    sb.AppendLine("            .Subscribe(chunks =>");
                    sb.AppendLine("            {");
                    sb.AppendLine("                InvokeAsync(StateHasChanged);");
                    sb.AppendLine("            }));");
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("    ");

            // Generate InitializeGeneratedCodeAsync method (ALWAYS generated to satisfy abstract contract)
            if (razorInfo.InjectedProperties.Any())
            {
                sb.AppendLine("    protected override async Task InitializeGeneratedCodeAsync()");
                sb.AppendLine("    {");
                foreach (var injectedProp in razorInfo.InjectedProperties)
                {
                    sb.AppendLine($"        await {injectedProp}.ContextReadyAsync();");
                }
                sb.AppendLine("    }");
            }
            else
            {
                sb.AppendLine("    protected override Task InitializeGeneratedCodeAsync()");
                sb.AppendLine("    {");
                sb.AppendLine("        return Task.CompletedTask;");
                sb.AppendLine("    }");
            }
            sb.AppendLine("    ");

            // Generate Dispose method
            if (razorInfo.FieldToPropertiesMap.Any())
            {
                sb.AppendLine("    protected override void Dispose(bool disposing)");
                sb.AppendLine("    {");
                sb.AppendLine("        OnDispose();");
                sb.AppendLine("        Subscriptions.Dispose();");
                sb.AppendLine("        base.Dispose(disposing);");
                sb.AppendLine("    }");
            }
        }

        sb.AppendLine("}");

            // Remove top namespace and use dots where possible
            var namespaceParts = razorInfo.Namespace.Split('.');
            var relativeNamespace = namespaceParts.Length > 1 ? string.Join(".", namespaceParts.Skip(1)) : namespaceParts[0];
            var fileName = $"{relativeNamespace}.{razorInfo.ClassName}.g.cs";
            context.AddSource(fileName, SourceText.From(sb.ToString(), Encoding.UTF8));
        }
        catch (Exception ex)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.CodeGenerationError,
                Location.None,
                razorInfo.ClassName,
                ex.Message);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
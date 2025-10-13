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
            if (razorInfo.HasDiagnosticIssue)
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.ComponentNotObservableError,
                    Location.None,
                    razorInfo.ClassName);
                context.ReportDiagnostic(diagnostic);
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
            // ObservableComponent<T> pattern - generate subscriptions and OnInitialize
            // Exclude "Model" and injected properties from DI constructor parameters
            var diFields = razorInfo.ObservableModelFields.Where(f => f != "Model" && !razorInfo.InjectedProperties.Contains(f)).ToList();
            
            // Generate CompositeDisposable for subscription management
            if (razorInfo.FieldToPropertiesMap.Any())
            {
                sb.AppendLine("    protected CompositeDisposable Subscriptions { get; } = new();");
                sb.AppendLine();
            }
            
            // Generate constructor only if there are DI fields (not Model property)
            if (diFields.Any())
            {
                var modelParams = diFields.Select(field => 
                    $"{razorInfo.FieldToTypeMap[field]} {field.TrimStart('_')}").ToList();
                
                sb.AppendLine($"    public {razorInfo.ClassName}({string.Join(", ", modelParams)})");
                sb.AppendLine("    {");
                
                foreach (var field in diFields)
                {
                    var paramName = field.TrimStart('_');
                    sb.AppendLine($"        {field} = {paramName};");
                }
                
                sb.AppendLine("    }");
                sb.AppendLine("    ");
            }

            // Generate OnContextReady and OnContextReadyAsync for injected models
            if (razorInfo.InjectedProperties.Any())
            {
                sb.AppendLine("    protected override void OnContextReady()");
                sb.AppendLine("    {");
                foreach (var injectedProp in razorInfo.InjectedProperties)
                {
                    sb.AppendLine($"        {injectedProp}.ContextReady();");
                }
                sb.AppendLine("        base.OnContextReady();");
                sb.AppendLine("    }");
                sb.AppendLine("    ");

                sb.AppendLine("    protected override async Task OnContextReadyAsync()");
                sb.AppendLine("    {");
                foreach (var injectedProp in razorInfo.InjectedProperties)
                {
                    sb.AppendLine($"        await {injectedProp}.ContextReadyAsync();");
                }
                sb.AppendLine("        await base.OnContextReadyAsync();");
                sb.AppendLine("    }");
                sb.AppendLine("    ");
            }

            // Generate OnInitialize method for subscription setup
            if (razorInfo.FieldToPropertiesMap.Any())
            {
                sb.AppendLine("    protected override void OnInitialize()");
                sb.AppendLine("    {");
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
                
                sb.AppendLine("    }");
                sb.AppendLine("    ");
            }

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

            // Generate OnContextReady and OnContextReadyAsync for injected models
            if (razorInfo.InjectedProperties.Any())
            {
                sb.AppendLine("    protected override void OnContextReady()");
                sb.AppendLine("    {");
                foreach (var injectedProp in razorInfo.InjectedProperties)
                {
                    sb.AppendLine($"        {injectedProp}.ContextReady();");
                }
                sb.AppendLine("        base.OnContextReady();");
                sb.AppendLine("    }");
                sb.AppendLine("    ");

                sb.AppendLine("    protected override async Task OnContextReadyAsync()");
                sb.AppendLine("    {");
                foreach (var injectedProp in razorInfo.InjectedProperties)
                {
                    sb.AppendLine($"        await {injectedProp}.ContextReadyAsync();");
                }
                sb.AppendLine("        await base.OnContextReadyAsync();");
                sb.AppendLine("    }");
                sb.AppendLine("    ");
            }

            // Generate OnInitialize method for subscription setup
            if (razorInfo.FieldToPropertiesMap.Any())
            {
                sb.AppendLine("    protected override void OnInitialize()");
                sb.AppendLine("    {");
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

                sb.AppendLine("    }");
                sb.AppendLine("    ");
            }

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
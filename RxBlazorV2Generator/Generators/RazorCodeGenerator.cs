using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RxBlazorV2Generator.Models;
using RxBlazorV2Generator.Diagnostics;
using System.Text;

namespace RxBlazorV2Generator.Generators;

public static class RazorCodeGenerator
{
    public static void GenerateRazorConstructors(SourceProductionContext context, RazorCodeBehindInfo razorInfo, int updateFrequencyMs = 100)
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
        sb.AppendLine("using System;");
        sb.AppendLine("using RxBlazorV2Sample.Model;");
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
            // ObservableComponent<T> pattern - generate subscriptions and OnContextReady
            var diFields = razorInfo.ObservableModelFields.Where(f => f != "Model").ToList();
            
            // Generate CompositeDisposable for subscription management
            if (razorInfo.FieldToPropertiesMap.Any())
            {
                sb.AppendLine("    private readonly CompositeDisposable _subscriptions = new();");
                sb.AppendLine("    protected IDisposable Subscriptions => _subscriptions;");
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

            // Generate OnContextReady method for subscription setup
            if (razorInfo.FieldToPropertiesMap.Any())
            {
                sb.AppendLine("    protected override void OnContextReady()");
                sb.AppendLine("    {");
                sb.AppendLine("        // Subscribe to model changes for component base model and other models from properties");
                
                foreach (var fieldProps in razorInfo.FieldToPropertiesMap)
                {
                    var fieldName = fieldProps.Key;
                    var properties = fieldProps.Value.Distinct().ToList();
                    
                    var propertyList = new List<string>();
                    propertyList.AddRange(properties.Select(p => $"\"{p}\""));
                    propertyList.Add($"{fieldName}.ModelID");
                    
                    var observedPropsArray = $"[{string.Join(", ", propertyList)}]";
                    
                    sb.AppendLine($"        _subscriptions.Add({fieldName}.Observable.Where(p => p.Intersect({observedPropsArray}).Any())");
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
                sb.AppendLine("        Subscriptions.Dispose();");
                sb.AppendLine("        base.Dispose(disposing);");
                sb.AppendLine("    }");
            }
        }
        else
        {
            // LayoutComponentBase pattern - generate constructor DI and subscriptions
            if (razorInfo.ObservableModelFields.Any())
            {
                var modelParams = razorInfo.FieldToTypeMap
                    .Select(kvp => $"{kvp.Value} {kvp.Key.TrimStart('_')}")
                    .ToList();
                
                sb.AppendLine($"    public {razorInfo.ClassName}({string.Join(", ", modelParams)})");
                sb.AppendLine("    {");
                
                foreach (var field in razorInfo.ObservableModelFields)
                {
                    var paramName = field.TrimStart('_');
                    sb.AppendLine($"        {field} = {paramName};");
                }
                
                sb.AppendLine("        // Subscribe to model changes for component base model and other models from properties");
                
                foreach (var fieldProps in razorInfo.FieldToPropertiesMap)
                {
                    var fieldName = fieldProps.Key;
                    var properties = fieldProps.Value.Distinct().ToList();
                    
                    var propertyList = new List<string>();
                    propertyList.AddRange(properties.Select(p => $"\"{p}\""));
                    propertyList.Add($"{fieldName}.ModelID");
                    
                    var observedPropsArray = $"[{string.Join(", ", propertyList)}]";
                    
                    sb.AppendLine($"        {fieldName}.Observable.Where(p => p.Intersect({observedPropsArray}).Any())");
                    sb.AppendLine("            .Subscribe(p =>");
                    sb.AppendLine("            {");
                    sb.AppendLine("                InvokeAsync(StateHasChanged);");
                    sb.AppendLine("            });");
                }
                
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
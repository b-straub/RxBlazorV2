using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RxBlazorV2Generator.Models;
using RxBlazorV2Generator.Diagnostics;
using System.Text;

namespace RxBlazorV2Generator.Generators;

public static class ComponentCodeGenerator
{
    public static void GenerateComponent(SourceProductionContext context, ComponentInfo componentInfo, int updateFrequencyMs = 100)
    {
        try
        {
            var sb = new StringBuilder();

            // Using statements
            sb.AppendLine("using R3;");
            sb.AppendLine("using ObservableCollections;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sb.AppendLine("using RxBlazorV2.Component;");

            // Add model namespace if different from component namespace
            if (componentInfo.ModelNamespace != componentInfo.ComponentNamespace)
            {
                sb.AppendLine($"using {componentInfo.ModelNamespace};");
            }

            sb.AppendLine();
            sb.AppendLine($"namespace {componentInfo.ComponentNamespace};");
            sb.AppendLine();

            // Component class declaration with generic types and constraints
            var genericPart = string.IsNullOrEmpty(componentInfo.GenericTypes) ? string.Empty : componentInfo.GenericTypes;
            var modelTypeWithGenerics = string.IsNullOrEmpty(componentInfo.GenericTypes)
                ? componentInfo.ModelTypeName
                : $"{componentInfo.ModelTypeName}{componentInfo.GenericTypes}";

            var constraintsPart = string.IsNullOrEmpty(componentInfo.TypeConstrains) ? string.Empty : $" {componentInfo.TypeConstrains}";

            sb.AppendLine($"public partial class {componentInfo.ComponentClassName}{genericPart} : ObservableComponent<{modelTypeWithGenerics}>{constraintsPart}");
            sb.AppendLine("{");

            // Generate shortcut properties for model references (ObservableModels)
            if (componentInfo.ModelReferences.Any())
            {
                GenerateModelReferenceShortcuts(sb, componentInfo.ModelReferences);
                sb.AppendLine();
            }

            // Generate shortcut properties for DI services
            if (componentInfo.DIFields.Any())
            {
                GenerateDIFieldShortcuts(sb, componentInfo.DIFields);
                sb.AppendLine();
            }

            // Generate InitializeGeneratedCode method
            GenerateInitializeGeneratedCode(sb, componentInfo, updateFrequencyMs);
            sb.AppendLine("    ");

            // Generate InitializeGeneratedCodeAsync method
            GenerateInitializeGeneratedCodeAsync(sb, componentInfo);
            sb.AppendLine("    ");

            // Generate hook methods for properties with [ObservableComponentTrigger]
            if (componentInfo.ComponentTriggers.Any())
            {
                GenerateHookMethods(sb, componentInfo.ComponentTriggers);
            }

            sb.AppendLine("}");

            // Generate file name
            var namespaceParts = componentInfo.ComponentNamespace.Split('.');
            var relativeNamespace = namespaceParts.Length > 1
                ? string.Join(".", namespaceParts.Skip(1))
                : namespaceParts[0];
            var fileName = $"{relativeNamespace}.{componentInfo.ComponentClassName}.g.cs";

            context.AddSource(fileName, SourceText.From(sb.ToString(), Encoding.UTF8));
        }
        catch (Exception ex)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.CodeGenerationError,
                Location.None,
                componentInfo.ComponentClassName,
                ex.Message);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void GenerateInitializeGeneratedCode(StringBuilder sb, ComponentInfo componentInfo, int updateFrequencyMs)
    {
        sb.AppendLine("    protected override void InitializeGeneratedCode()");
        sb.AppendLine("    {");

        // Generate subscriptions for batched properties
        if (componentInfo.BatchSubscriptions.Any())
        {
            sb.AppendLine("        // Subscribe to model changes for component base model and other models from properties");

            foreach (var kvp in componentInfo.BatchSubscriptions)
            {
                var fieldName = kvp.Key;
                var properties = kvp.Value;
                var propertyList = properties.Select(p => $"\"{p}\"").ToList();
                var observedPropsArray = $"[{string.Join(", ", propertyList)}]";

                sb.AppendLine($"        Subscriptions.Add({fieldName}.Observable.Where(p => p.Intersect({observedPropsArray}).Any())");
                sb.AppendLine($"            .Chunk(TimeSpan.FromMilliseconds({updateFrequencyMs}))");
                sb.AppendLine("            .Subscribe(chunks =>");
                sb.AppendLine("            {");
                sb.AppendLine("                InvokeAsync(StateHasChanged);");
                sb.AppendLine("            }));");
            }
        }

        // Generate subscriptions for component triggers with chunking
        foreach (var trigger in componentInfo.ComponentTriggers)
        {
            sb.AppendLine("        ");
            if (trigger.HookType == TriggerHookType.Sync)
            {
                sb.AppendLine($"        Subscriptions.Add(Model.Observable.Where(p => p.Intersect([\"{trigger.PropertyName}\"]).Any())");
                sb.AppendLine($"            .Chunk(TimeSpan.FromMilliseconds({updateFrequencyMs}))");
                sb.AppendLine("            .Subscribe(chunks =>");
                sb.AppendLine("            {");
                sb.AppendLine($"                {trigger.HookMethodName}();");
                sb.AppendLine("            }));");
            }
            else if (trigger.HookType == TriggerHookType.Async)
            {
                sb.AppendLine($"        Subscriptions.Add(Model.Observable.Where(p => p.Intersect([\"{trigger.PropertyName}\"]).Any())");
                sb.AppendLine($"            .Chunk(TimeSpan.FromMilliseconds({updateFrequencyMs}))");
                sb.AppendLine("            .SubscribeAwait(async (chunks, ct) =>");
                sb.AppendLine("            {");
                sb.AppendLine($"                await {trigger.HookMethodName}(ct);");
                sb.AppendLine("            }));");
            }
        }

        sb.AppendLine("    }");
    }

    private static void GenerateInitializeGeneratedCodeAsync(StringBuilder sb, ComponentInfo componentInfo)
    {
        sb.AppendLine("    protected override Task InitializeGeneratedCodeAsync()");
        sb.AppendLine("    {");
        sb.AppendLine("        return Task.CompletedTask;");
        sb.AppendLine("    }");
    }

    private static void GenerateHookMethods(StringBuilder sb, List<ComponentTriggerInfo> triggers)
    {
        foreach (var trigger in triggers)
        {
            if (trigger.HookType == TriggerHookType.Sync)
            {
                // Generate sync hook method
                sb.AppendLine($"    protected virtual void {trigger.HookMethodName}()");
                sb.AppendLine("    {");
                sb.AppendLine("    }");
            }
            else if (trigger.HookType == TriggerHookType.Async)
            {
                // Generate async hook method
                sb.AppendLine($"    protected virtual Task {trigger.HookMethodName}(CancellationToken ct)");
                sb.AppendLine("    {");
                sb.AppendLine("        return Task.CompletedTask;");
                sb.AppendLine("    }");
            }

            // Add spacing between different properties
            if (trigger != triggers.Last())
            {
                sb.AppendLine("    ");
            }
        }
    }

    private static void GenerateModelReferenceShortcuts(StringBuilder sb, List<ModelReferenceInfo> modelReferences)
    {
        foreach (var modelRef in modelReferences)
        {
            sb.AppendLine($"    protected {modelRef.ReferencedModelTypeName} {modelRef.PropertyName} => Model.{modelRef.PropertyName};");
        }
    }

    private static void GenerateDIFieldShortcuts(StringBuilder sb, List<DIFieldInfo> diFields)
    {
        foreach (var diField in diFields)
        {
            sb.AppendLine($"    protected {diField.FieldType} {diField.FieldName} => Model.{diField.FieldName};");
        }
    }
}

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

            // Generate InitializeGeneratedCode method
            GenerateInitializeGeneratedCode(sb, componentInfo, updateFrequencyMs);
            sb.AppendLine();

            // Generate InitializeGeneratedCodeAsync method
            GenerateInitializeGeneratedCodeAsync(sb, componentInfo);
            sb.AppendLine();

            // Generate hook methods for properties with [ObservableComponentTrigger]
            // UNLESS they are RenderOnly (TriggerBehavior == 1)
            var triggersWithHooks = componentInfo.ComponentTriggers
                .Where(t => t.TriggerBehavior != 1) // 1 = RenderOnly (no hooks)
                .ToList();

            if (triggersWithHooks.Any())
            {
                GenerateHookMethods(sb, triggersWithHooks);
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

        // Generate subscription for model changes with filtering support
        sb.AppendLine("        // Subscribe to model changes - respects Filter() method");
        sb.AppendLine("        var filter = Filter();");
        sb.AppendLine("        if (filter.Length > 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Filter active - observe only filtered properties");
        sb.AppendLine("            Subscriptions.Add(Model.Observable");
        sb.AppendLine("                .Where(changedProps => changedProps.Intersect(filter).Any())");
        sb.AppendLine($"                .Chunk(TimeSpan.FromMilliseconds({updateFrequencyMs}))");
        sb.AppendLine("                .Subscribe(chunks =>");
        sb.AppendLine("                {");
        sb.AppendLine("                    InvokeAsync(StateHasChanged);");
        sb.AppendLine("                }));");
        sb.AppendLine("        }");
        sb.AppendLine("        // else: Empty filter - no automatic StateHasChanged, only triggers (if any) will fire");

        // Generate subscriptions for component triggers with chunking
        // UNLESS they are RenderOnly (TriggerBehavior == 1)
        foreach (var trigger in componentInfo.ComponentTriggers)
        {
            // Skip RenderOnly triggers (1 = RenderOnly, no hook subscriptions)
            if (trigger.TriggerBehavior == 1)
            {
                continue;
            }

            sb.AppendLine();

            // Determine which Observable to subscribe to and the filter property name:
            // - Local triggers: Model.Observable with filter "Model.PropertyName"
            // - Referenced triggers: Model.Observable with filter "Model.{RefProperty}.PropertyName"
            //   (Referenced model properties are transformed and merged into Model.Observable)
            var observableSource = "Model.Observable";
            string filterPropertyName;

            if (trigger.ReferencedModelPropertyName is not null)
            {
                // Referenced model trigger: Subscribe to Model.Observable
                // Filter on "Model.{RefProperty}.PropertyName" because referenced properties are transformed
                // Example: Settings.IsDay emits "Model.IsDay" → transformed to "Model.Settings.IsDay" in parent model
                filterPropertyName = $"Model.{trigger.ReferencedModelPropertyName}.{trigger.PropertyName}";
            }
            else
            {
                // Local trigger: Subscribe to Model.Observable
                filterPropertyName = $"Model.{trigger.PropertyName}";
            }

            if (trigger.HookType == TriggerHookType.Sync)
            {
                sb.AppendLine($"        Subscriptions.Add({observableSource}.Where(p => p.Intersect([\"{filterPropertyName}\"]).Any())");
                sb.AppendLine($"            .Chunk(TimeSpan.FromMilliseconds({updateFrequencyMs}))");
                sb.AppendLine("            .Subscribe(chunks =>");
                sb.AppendLine("            {");
                sb.AppendLine($"                {trigger.HookMethodName}();");
                sb.AppendLine("            }));");
            }
            else if (trigger.HookType == TriggerHookType.Async)
            {
                sb.AppendLine($"        Subscriptions.Add({observableSource}.Where(p => p.Intersect([\"{filterPropertyName}\"]).Any())");
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
                sb.AppendLine();
            }
        }
    }

}

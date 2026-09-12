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
            sb.AppendLine("#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member");

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
            GenerateInitializeGeneratedCodeAsync(sb);
            sb.AppendLine();

            // Generate hook methods for properties with [ObservableComponentTrigger]
            if (componentInfo.ComponentTriggers.Any())
            {
                GenerateHookMethods(sb, componentInfo.ComponentTriggers);
                sb.AppendLine();
            }

            // Generate one hook method per [ObservableComponentBatchAsync] batch
            if (componentInfo.ComponentBatches.Any())
            {
                GenerateComponentBatchHookMethods(sb, componentInfo.ComponentBatches);
                sb.AppendLine();
            }

            // Seal lifecycle methods to prevent derived classes (Razor pages) from
            // overriding them and breaking the reactive pipeline.
            // Use OnContextReady/OnContextReadyAsync for initialization instead.
            GenerateSealedLifecycleMethods(sb);

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

        // Generate subscriptions for component trigger hooks
        foreach (var trigger in componentInfo.ComponentTriggers)
        {
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

        // Generate subscriptions for [ObservableComponentBatchAsync] batches.
        // Debounce (trailing edge) rather than Chunk: a debounced member must reach the hook once
        // after its burst settles, not once per rolling window. Members declaring window 0 get no
        // Debounce at all, so a discrete action such as a switch click is delivered immediately.
        // Switch is applied once for the whole batch: a newer change cancels the token of an
        // in-flight hook invocation rather than queueing behind it.
        foreach (var batch in componentInfo.ComponentBatches)
        {
            sb.AppendLine();
            GenerateComponentBatchSubscription(sb, batch);
        }

        sb.AppendLine("    }");
    }

    private static void GenerateInitializeGeneratedCodeAsync(StringBuilder sb)
    {
        sb.AppendLine("    protected override Task InitializeGeneratedCodeAsync()");
        sb.AppendLine("    {");
        sb.AppendLine("        return Task.CompletedTask;");
        sb.AppendLine("    }");
    }

    /// <summary>
    /// Emits the subscription for one batch. When every member shares a debounce window the whole
    /// batch is a single filtered stream; otherwise one stream per distinct window is merged, so
    /// each member keeps its own timing while the batch still resolves to one hook.
    /// </summary>
    private static void GenerateComponentBatchSubscription(StringBuilder sb, ComponentBatchInfo batch)
    {
        var windows = batch.DistinctDebounceWindows;

        if (windows.Count == 1)
        {
            var filter = FormatBatchFilter(batch.Members.Select(member => member.QualifiedPropertyName));

            sb.AppendLine($"        Subscriptions.Add(Model.Observable.Where(p => p.Intersect([{filter}]).Any())");
            AppendDebounceOperator(sb, windows[0], "            ");
            sb.AppendLine("            .SubscribeAwait(async (props, ct) =>");
            sb.AppendLine("            {");
            sb.AppendLine($"                await {batch.HookMethodName}(ct);");
            sb.AppendLine("            }, AwaitOperation.Switch));");
            return;
        }

        // Fully qualified so a user-declared member named Observable in the partial component
        // cannot shadow the R3 static class.
        sb.AppendLine("        Subscriptions.Add(R3.Observable.Merge(");

        for (var i = 0; i < windows.Count; i++)
        {
            var window = windows[i];
            var filter = FormatBatchFilter(batch.Members
                .Where(member => member.DebounceMilliseconds == window)
                .Select(member => member.QualifiedPropertyName));

            var terminator = i == windows.Count - 1 ? ")" : ",";

            if (window > 0)
            {
                sb.AppendLine($"                Model.Observable.Where(p => p.Intersect([{filter}]).Any())");
                sb.AppendLine($"                    .Debounce(TimeSpan.FromMilliseconds({window})){terminator}");
            }
            else
            {
                sb.AppendLine($"                Model.Observable.Where(p => p.Intersect([{filter}]).Any()){terminator}");
            }
        }

        sb.AppendLine("            .SubscribeAwait(async (props, ct) =>");
        sb.AppendLine("            {");
        sb.AppendLine($"                await {batch.HookMethodName}(ct);");
        sb.AppendLine("            }, AwaitOperation.Switch));");
    }

    private static void AppendDebounceOperator(StringBuilder sb, int debounceMilliseconds, string indent)
    {
        // Window 0 means immediate: no operator at all, rather than a zero-length timer.
        if (debounceMilliseconds > 0)
        {
            sb.AppendLine($"{indent}.Debounce(TimeSpan.FromMilliseconds({debounceMilliseconds}))");
        }
    }

    private static string FormatBatchFilter(IEnumerable<string> qualifiedPropertyNames)
    {
        return string.Join(", ", qualifiedPropertyNames.Select(name => $"\"{name}\""));
    }

    /// <summary>
    /// Generates one overridable hook per [ObservableComponentBatchAsync] batch. The component
    /// overrides it to run the coalesced side effect - reloading a server-driven table, for instance.
    /// </summary>
    private static void GenerateComponentBatchHookMethods(StringBuilder sb, List<ComponentBatchInfo> batches)
    {
        for (var i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            sb.AppendLine($"    protected virtual Task {batch.HookMethodName}(CancellationToken ct)");
            sb.AppendLine("    {");
            sb.AppendLine("        return Task.CompletedTask;");
            sb.AppendLine("    }");

            if (i < batches.Count - 1)
            {
                sb.AppendLine();
            }
        }
    }

    private static void GenerateHookMethods(StringBuilder sb, List<ComponentTriggerInfo> triggers)
    {
        for (var i = 0; i < triggers.Count; i++)
        {
            var trigger = triggers[i];
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
            if (i < triggers.Count - 1)
            {
                sb.AppendLine();
            }
        }
    }

    private static void GenerateSealedLifecycleMethods(StringBuilder sb)
    {
        sb.AppendLine("    protected sealed override void OnAfterRender(bool firstRender)");
        sb.AppendLine("    {");
        sb.AppendLine("        base.OnAfterRender(firstRender);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    protected sealed override async Task OnAfterRenderAsync(bool firstRender)");
        sb.AppendLine("    {");
        sb.AppendLine("        await base.OnAfterRenderAsync(firstRender);");
        sb.AppendLine("    }");
    }

}

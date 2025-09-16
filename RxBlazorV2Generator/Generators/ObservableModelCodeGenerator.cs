using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RxBlazorV2Generator.Models;
using RxBlazorV2Generator.Analyzers;
using RxBlazorV2Generator.Diagnostics;
using System.Text;

namespace RxBlazorV2Generator.Generators;

public static class ObservableModelCodeGenerator
{
    public static void GenerateObservableModelPartials(SourceProductionContext context, ObservableModelInfo modelInfo,
        int updateFrequencyMs = 100)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("#nullable enable");

            // Add all using statements from the source file
            var requiredUsings = new HashSet<string>(modelInfo.UsingStatements);

            // Add required framework usings
            requiredUsings.Add("RxBlazorV2.Model");
            requiredUsings.Add("RxBlazorV2.Interface");
            requiredUsings.Add("Microsoft.Extensions.DependencyInjection");
            requiredUsings.Add("R3");
            requiredUsings.Add("ObservableCollections");
            requiredUsings.Add("System");

            // Add System.Linq if there are model references
            if (modelInfo.ModelReferences.Any())
            {
                requiredUsings.Add("System.Linq");
            }

            // Add using statements for referenced model namespaces
            if (modelInfo.ModelReferences.Any())
            {
                foreach (var modelRef in modelInfo.ModelReferences)
                {
                    if (!string.IsNullOrEmpty(modelRef.ReferencedModelNamespace) &&
                        modelRef.ReferencedModelNamespace != modelInfo.Namespace)
                    {
                        requiredUsings.Add(modelRef.ReferencedModelNamespace);
                    }
                }
            }

            // Sort and add all using statements
            foreach (var usingStatement in requiredUsings.OrderBy(u => u))
            {
                sb.AppendLine($"using {usingStatement};");
            }

            sb.AppendLine();
            sb.AppendLine($"namespace {modelInfo.Namespace};");
            sb.AppendLine();
            sb.AppendLine($"public partial class {modelInfo.ClassName}{modelInfo.GenericTypes}");
            sb.AppendLine("{");

            // Generate ModelID property implementation
            sb.AppendLine($"    public override string ModelID => \"{modelInfo.FullyQualifiedName}\";");
            sb.AppendLine();

            // Generate Subscriptions property implementation
            sb.AppendLine("    private readonly CompositeDisposable _subscriptions = new();");
            sb.AppendLine("    protected override IDisposable Subscriptions => _subscriptions;");
            sb.AppendLine();

            // Generate protected properties for referenced models
            foreach (var modelRef in modelInfo.ModelReferences)
            {
                sb.AppendLine(
                    $"    protected {modelRef.ReferencedModelTypeName} {modelRef.PropertyName} {{ get; private set; }}");
            }

            if (modelInfo.ModelReferences.Any())
            {
                sb.AppendLine();
            }

            // Generate partial property implementations with field keyword
            foreach (var prop in modelInfo.PartialProperties)
            {
                sb.AppendLine($"    public partial {prop.Type} {prop.Name}");
                sb.AppendLine("    {");
                sb.AppendLine("        get => field;");
                sb.AppendLine("        set");
                sb.AppendLine("        {");
                sb.AppendLine("            field = value;");
                sb.AppendLine($"            StateHasChanged(nameof({prop.Name}));");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine();
            }

            // Generate backing fields for command properties
            foreach (var cmd in modelInfo.CommandProperties)
            {
                sb.AppendLine($"    private {cmd.Type} _{cmd.Name.Substring(0, 1).ToLower()}{cmd.Name.Substring(1)};");
            }

            sb.AppendLine();

            // Generate command property implementations with backing fields
            foreach (var cmd in modelInfo.CommandProperties)
            {
                var backingField = $"_{cmd.Name.Substring(0, 1).ToLower()}{cmd.Name.Substring(1)}";
                sb.AppendLine($"    public partial {cmd.Type} {cmd.Name}");
                sb.AppendLine("    {");
                sb.AppendLine($"        get => {backingField};");
                sb.AppendLine("    }");
                sb.AppendLine();
            }

            // Generate constructor if there are model references, DI fields, or IObservableCollection properties
            var hasObservableCollections = modelInfo.PartialProperties.Any(p => p.IsObservableCollection);
            if (modelInfo.ModelReferences.Any() || modelInfo.DIFields.Any() || hasObservableCollections)
            {
                // Generate constructor parameters for referenced models and DI fields
                var constructorParams = new List<string>();

                // Add model reference parameters
                constructorParams.AddRange(modelInfo.ModelReferences.Select(mr =>
                    $"{mr.ReferencedModelTypeName} {mr.PropertyName.ToLowerInvariant()}"));

                // Add DI field parameters
                constructorParams.AddRange(modelInfo.DIFields.Select(df =>
                {
                    var paramName = df.FieldName.StartsWith("_") ? df.FieldName.Substring(1) : df.FieldName;
                    return $"{df.FieldType} {paramName.ToLowerInvariant()}";
                }));

                var allParams = string.Join(", ", constructorParams);

                sb.AppendLine($"    public {modelInfo.ClassName}({allParams}) : base()");
                sb.AppendLine("    {");

                // Assign referenced models
                foreach (var modelRef in modelInfo.ModelReferences)
                {
                    sb.AppendLine($"        {modelRef.PropertyName} = {modelRef.PropertyName.ToLowerInvariant()};");
                }

                // Assign DI fields
                foreach (var diField in modelInfo.DIFields)
                {
                    var paramName = diField.FieldName.StartsWith("_")
                        ? diField.FieldName.Substring(1)
                        : diField.FieldName;
                    sb.AppendLine($"        {diField.FieldName} = {paramName.ToLowerInvariant()};");
                }

                // Initialize IObservableCollection properties
                var observableCollectionProperties =
                    modelInfo.PartialProperties.Where(p => p.IsObservableCollection).ToList();
                if (observableCollectionProperties.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine("        // Initialize IObservableCollection properties");
                    foreach (var prop in observableCollectionProperties)
                    {
                        sb.AppendLine($"        {prop.Name} = new();");
                        sb.AppendLine($"        _subscriptions.Add({prop.Name}.ObserveChanged()");
                        sb.AppendLine($"            .Chunk(TimeSpan.FromMilliseconds({updateFrequencyMs}))");
                        sb.AppendLine($"            .Subscribe(chunks => StateHasChanged(\"{prop.Name}\")));");
                        sb.AppendLine();
                    }
                }

                // Initialize commands
                if (modelInfo.CommandProperties.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine("        // Initialize commands");
                    foreach (var cmd in modelInfo.CommandProperties)
                    {
                        var observedProps = ObservableModelAnalyzer.GetObservedProperties(modelInfo, cmd);
                        var observedPropsArray = $"[\"{string.Join("\", \"", observedProps)}\"]";

                        var factoryCall = GetFactoryCall(cmd, observedPropsArray);
                        var backingField = $"_{cmd.Name.Substring(0, 1).ToLower()}{cmd.Name.Substring(1)}";
                        sb.AppendLine($"        {backingField} = {factoryCall};");
                    }
                }

                // Generate observable subscriptions for referenced model changes
                if (modelInfo.ModelReferences.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine("        // Subscribe to referenced model changes");

                    foreach (var modelRef in modelInfo.ModelReferences)
                    {
                        var observedProps = $"[\"{string.Join("\", \"", modelRef.UsedProperties)}\"]";
                        sb.AppendLine(
                            $"        _subscriptions.Add({modelRef.PropertyName}.Observable.Where(p => p.Intersect({observedProps}).Any())");
                        sb.AppendLine($"            .Chunk(TimeSpan.FromMilliseconds({updateFrequencyMs}))");
                        sb.AppendLine(
                            "            .Subscribe(chunks => StateHasChanged(chunks.SelectMany(c => c).ToArray())));");
                    }
                }

                // Generate command trigger subscriptions
                var commandsWithTriggers = modelInfo.CommandProperties.Where(cmd => cmd.Triggers.Any()).ToList();
                if (commandsWithTriggers.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine("        // Subscribe to command triggers");

                    foreach (var cmd in commandsWithTriggers)
                    {
                        var backingField = $"_{cmd.Name.Substring(0, 1).ToLower()}{cmd.Name.Substring(1)}";

                        foreach (var trigger in cmd.Triggers)
                        {
                            var (sourceModel, triggerProperty) =
                                ParseTriggerProperty(trigger.TriggerProperty, modelInfo);
                            var triggerProps = $"[\"{triggerProperty}\"]";

                            var combinedCondition = GetCombinedTriggerCondition(cmd, trigger);

                            if (string.IsNullOrEmpty(sourceModel))
                            {
                                // Local property trigger
                                sb.AppendLine(
                                    $"        _subscriptions.Add(Observable.Where(p => p.Intersect({triggerProps}).Any())");
                                sb.AppendLine($"            .Chunk(TimeSpan.FromMilliseconds({updateFrequencyMs}))");
                                if (!string.IsNullOrEmpty(combinedCondition))
                                {
                                    sb.AppendLine($"            .Where(_ => {combinedCondition})");
                                }

                                sb.AppendLine(GetTriggerCall(cmd, backingField, trigger.Parameter));
                            }
                            else
                            {
                                // Referenced model property trigger
                                sb.AppendLine(
                                    $"        _subscriptions.Add({sourceModel}.Observable.Where(p => p.Intersect({triggerProps}).Any())");
                                sb.AppendLine($"            .Chunk(TimeSpan.FromMilliseconds({updateFrequencyMs}))");
                                if (!string.IsNullOrEmpty(combinedCondition))
                                {
                                    sb.AppendLine($"            .Where(_ => {combinedCondition})");
                                }

                                sb.AppendLine(GetTriggerCall(cmd, backingField, trigger.Parameter));
                            }
                        }
                    }
                }

                sb.AppendLine("    }");
                sb.AppendLine();
            }
            else if (modelInfo.CommandProperties.Any())
            {
                // Generate constructor for models without references but with commands only
                sb.AppendLine($"    public {modelInfo.ClassName}() : base()");
                sb.AppendLine("    {");

                // Initialize IObservableCollection properties
                var observableCollectionProperties =
                    modelInfo.PartialProperties.Where(p => p.IsObservableCollection).ToList();
                if (observableCollectionProperties.Any())
                {
                    sb.AppendLine("        // Initialize IObservableCollection properties");
                    foreach (var prop in observableCollectionProperties)
                    {
                        sb.AppendLine($"        {prop.Name} = new();");
                        sb.AppendLine($"        _subscriptions.Add({prop.Name}.ObserveChanged()");
                        sb.AppendLine($"            .Chunk(TimeSpan.FromMilliseconds({updateFrequencyMs}))");
                        sb.AppendLine($"            .Subscribe(chunks => StateHasChanged(\"{prop.Name}\")));");
                        sb.AppendLine();
                    }
                }

                // Initialize commands
                foreach (var cmd in modelInfo.CommandProperties)
                {
                    var observedProps = ObservableModelAnalyzer.GetObservedProperties(modelInfo, cmd);
                    var observedPropsArray = $"[\"{string.Join("\", \"", observedProps)}\"]";

                    var factoryCall = GetFactoryCall(cmd, observedPropsArray);
                    var backingField = $"_{cmd.Name.Substring(0, 1).ToLower()}{cmd.Name.Substring(1)}";
                    sb.AppendLine($"        {backingField} = {factoryCall};");
                }

                // Generate command trigger subscriptions for models without dependencies
                var commandsWithTriggers = modelInfo.CommandProperties.Where(cmd => cmd.Triggers.Any()).ToList();
                if (commandsWithTriggers.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine("        // Subscribe to command triggers");

                    foreach (var cmd in commandsWithTriggers)
                    {
                        var backingField = $"_{cmd.Name.Substring(0, 1).ToLower()}{cmd.Name.Substring(1)}";

                        foreach (var trigger in cmd.Triggers)
                        {
                            var (sourceModel, triggerProperty) =
                                ParseTriggerProperty(trigger.TriggerProperty, modelInfo);
                            var triggerProps = $"[\"{triggerProperty}\"]";

                            var combinedCondition = GetCombinedTriggerCondition(cmd, trigger);

                            if (string.IsNullOrEmpty(sourceModel))
                            {
                                // Local property trigger
                                sb.AppendLine(
                                    $"        _subscriptions.Add(Observable.Where(p => p.Intersect({triggerProps}).Any())");
                                sb.AppendLine($"            .Chunk(TimeSpan.FromMilliseconds({updateFrequencyMs}))");
                                if (!string.IsNullOrEmpty(combinedCondition))
                                {
                                    sb.AppendLine($"            .Where(_ => {combinedCondition})");
                                }

                                sb.AppendLine(GetTriggerCall(cmd, backingField, trigger.Parameter));
                            }
                        }
                    }
                }

                sb.AppendLine("    }");
            }
            else if (hasObservableCollections)
            {
                // Generate constructor for models with only IObservableCollection properties
                sb.AppendLine($"    public {modelInfo.ClassName}() : base()");
                sb.AppendLine("    {");

                // Initialize IObservableCollection properties
                var observableCollectionProperties =
                    modelInfo.PartialProperties.Where(p => p.IsObservableCollection).ToList();
                sb.AppendLine("        // Initialize IObservableCollection properties");
                foreach (var prop in observableCollectionProperties)
                {
                    sb.AppendLine($"        {prop.Name} = new();");
                    sb.AppendLine($"        _subscriptions.Add({prop.Name}.ObserveChanged()");
                    sb.AppendLine($"            .Chunk(TimeSpan.FromMilliseconds({updateFrequencyMs}))");
                    sb.AppendLine($"            .Subscribe(chunks => StateHasChanged(\"{prop.Name}\")));");
                    sb.AppendLine();
                }

                sb.AppendLine("    }");
            }

            sb.AppendLine("}");

            // Remove top namespace and use dots where possible
            var namespaceParts = modelInfo.Namespace.Split('.');
            var relativeNamespace =
                namespaceParts.Length > 1 ? string.Join(".", namespaceParts.Skip(1)) : namespaceParts[0];
            var fileName = $"{relativeNamespace}.{modelInfo.ClassName}.g.cs";
            context.AddSource(fileName, SourceText.From(sb.ToString(), Encoding.UTF8));
        }
        catch (Exception ex)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.CodeGenerationError,
                Location.None,
                modelInfo.ClassName,
                ex.Message);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static string GetFactoryCall(CommandPropertyInfo command, string observedPropsArray)
    {
        // Determine factory type based on command property type and method signature
        if (command.Type.Contains("IObservableCommandAsync<"))
        {
            var genericType = ExtractGenericType(command.Type);
            if (command.SupportsCancellation)
            {
                // Use cancelable factory for methods with CancellationToken
                if (command.CanExecuteMethod != null)
                {
                    return
                        $"new ObservableCommandAsyncCancelableFactory<{genericType}>(this, {observedPropsArray}, {command.ExecuteMethod}, {command.CanExecuteMethod})";
                }

                return
                    $"new ObservableCommandAsyncCancelableFactory<{genericType}>(this, {observedPropsArray}, {command.ExecuteMethod})";
            }

            // Use regular async factory for methods without CancellationToken
            if (command.CanExecuteMethod != null)
            {
                return
                    $"new ObservableCommandAsyncFactory<{genericType}>(this, {observedPropsArray}, {command.ExecuteMethod}, {command.CanExecuteMethod})";
            }

            return
                $"new ObservableCommandAsyncFactory<{genericType}>(this, {observedPropsArray}, {command.ExecuteMethod})";
        }

        if (command.Type.Contains("IObservableCommandAsync"))
        {
            if (command.SupportsCancellation)
            {
                // Use cancelable factory for methods with CancellationToken
                if (command.CanExecuteMethod != null)
                {
                    return
                        $"new ObservableCommandAsyncCancelableFactory(this, {observedPropsArray}, {command.ExecuteMethod}, {command.CanExecuteMethod})";
                }

                return
                    $"new ObservableCommandAsyncCancelableFactory(this, {observedPropsArray}, {command.ExecuteMethod})";
            }

            // Use regular async factory for methods without CancellationToken
            if (command.CanExecuteMethod != null)
            {
                return
                    $"new ObservableCommandAsyncFactory(this, {observedPropsArray}, {command.ExecuteMethod}, {command.CanExecuteMethod})";
            }

            return $"new ObservableCommandAsyncFactory(this, {observedPropsArray}, {command.ExecuteMethod})";
        }

        if (command.Type.Contains("IObservableCommand<"))
        {
            var genericType = ExtractGenericType(command.Type);
            if (command.CanExecuteMethod != null)
            {
                return
                    $"new ObservableCommandFactory<{genericType}>(this, {observedPropsArray}, {command.ExecuteMethod}, {command.CanExecuteMethod})";
            }

            return $"new ObservableCommandFactory<{genericType}>(this, {observedPropsArray}, {command.ExecuteMethod})";
        }

        // IObservableCommand
        if (command.CanExecuteMethod != null)
        {
            return
                $"new ObservableCommandFactory(this, {observedPropsArray}, {command.ExecuteMethod}, {command.CanExecuteMethod})";
        }

        return $"new ObservableCommandFactory(this, {observedPropsArray}, {command.ExecuteMethod})";
    }

    private static string GetTriggerCall(CommandPropertyInfo command, string backingField, string? parameter = null)
    {
        // Determine trigger call based on method signature
        if (parameter is not null)
        {
            return command.Type.Contains("IObservableCommandAsync")
                ? $"            .Subscribe(_ => {backingField}.ExecuteAsync({parameter})));"
                : $"            .Subscribe(_ => {backingField}.Execute({parameter})));";
        }

        return command.Type.Contains("IObservableCommandAsync")
            ? $"            .Subscribe(_ => {backingField}.ExecuteAsync()));"
            : $"            .Subscribe(_ => {backingField}.Execute()));";
    }

    private static string ExtractGenericType(string type)
    {
        var start = type.IndexOf('<') + 1;
        var end = type.LastIndexOf('>');
        return type.Substring(start, end - start);
    }


    public static void GenerateAddObservableModelsExtension(SourceProductionContext context,
        ObservableModelInfo[] models)
    {
        try
        {
            var sb = new StringBuilder();

            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sb.AppendLine("using RxBlazorV2.Model;");

            // Add using statements for all model namespaces
            var namespaces = models.Select(m => m.Namespace).Distinct().Where(ns => !string.IsNullOrEmpty(ns))
                .ToArray();

            var rootNamespace = namespaces.First().IndexOf(".", StringComparison.InvariantCulture) > 0 ? 
                namespaces.First().Split('.')[0] : namespaces.First();
            
            foreach (var ns in namespaces)
            {
                sb.AppendLine($"using {ns};");
            }

            // Add using statements for interface namespaces
            var interfaceNamespaces = models
                .SelectMany(m => m.ImplementedInterfaces)
                .Select(i => ExtractNamespace(i))
                .Distinct()
                .Where(ns => !string.IsNullOrEmpty(ns) && !namespaces.Contains(ns));
            foreach (var ns in interfaceNamespaces)
            {
                sb.AppendLine($"using {ns};");
            }

            sb.AppendLine();
            sb.AppendLine($"namespace {rootNamespace};");
            sb.AppendLine();
            sb.AppendLine("public static class ObservableModelServiceCollectionExtensions");
            sb.AppendLine("{");
            sb.AppendLine("    public static IServiceCollection AddObservableModels(this IServiceCollection services)");
            sb.AppendLine("    {");

            foreach (var model in models.Where(m => m.GenericTypes.Length == 0))
            {
                var scope = GetModelScope(model);
                var registrationMethod = GetRegistrationMethod(scope);

                // Generate dependency injection registration for the concrete type
                if (model.ModelReferences.Any() || model.DIFields.Any())
                {
                    // Model has dependencies - use factory
                    var paramResolvers = new List<string>();

                    // Add model reference resolvers
                    paramResolvers.AddRange(model.ModelReferences.Select(mr =>
                        $"sp.GetRequiredService<{mr.ReferencedModelTypeName}>()"));

                    // Add DI field resolvers
                    paramResolvers.AddRange(model.DIFields.Select(df =>
                        $"sp.GetRequiredService<{df.FieldType}>()"));

                    var allResolvers = string.Join(", ", paramResolvers);
                    sb.AppendLine(
                        $"        services.{registrationMethod}<{model.ClassName}>(sp => new {model.ClassName}({allResolvers}));");
                }
                else
                {
                    // Model has no dependencies - simple registration
                    sb.AppendLine($"        services.{registrationMethod}<{model.ClassName}>();");
                }

                // Generate interface mappings for any implemented IObservableModel interfaces
                foreach (var interfaceType in model.ImplementedInterfaces)
                {
                    sb.AppendLine(
                        $"        services.{registrationMethod}<{interfaceType}>(sp => sp.GetRequiredService<{model.ClassName}>());");
                }
            }

            sb.AppendLine("        return services;");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            context.AddSource("ObservableModelServiceCollectionExtensions.g.cs",
                SourceText.From(sb.ToString(), Encoding.UTF8));
        }
        catch (Exception ex)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.CodeGenerationError,
                Location.None,
                "AddObservableModelsExtension",
                ex.Message);
            context.ReportDiagnostic(diagnostic);
        }
    }

    public static void GenerateAddGenericObservableModelsExtension(SourceProductionContext context,
        ObservableModelInfo[] models)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sb.AppendLine("using RxBlazorV2.Model;");

            // Add using statements for all model namespaces
            var namespaces = models.Select(m => m.Namespace).Distinct().Where(ns => !string.IsNullOrEmpty(ns))
                .ToArray();
            
            var rootNamespace = namespaces.First().IndexOf(".", StringComparison.InvariantCulture) > 0 ? 
                namespaces.First().Split('.')[0] : namespaces.First();

            foreach (var ns in namespaces)
            {
                sb.AppendLine($"using {ns};");
            }

            // Add using statements for interface namespaces
            var interfaceNamespaces = models
                .SelectMany(m => m.ImplementedInterfaces)
                .Select(i => ExtractNamespace(i))
                .Distinct()
                .Where(ns => !string.IsNullOrEmpty(ns) && !namespaces.Contains(ns));
            foreach (var ns in interfaceNamespaces)
            {
                sb.AppendLine($"using {ns};");
            }

            sb.AppendLine();
            sb.AppendLine($"namespace {rootNamespace};");
            sb.AppendLine();

            sb.AppendLine($"public static class GenericModelsServiceCollectionExtensions");
            sb.AppendLine("{");

            foreach (var model in models.Where(m => m.GenericTypes.Length > 0))
            {
                sb.AppendLine(
                    $"    public static IServiceCollection Add{model.ClassName}{model.GenericTypes}(this IServiceCollection services)");

                if (model.TypeConstrains.Length > 0)
                {
                    sb.AppendLine($"        {model.TypeConstrains}");
                }

                sb.AppendLine("    {");

                var scope = GetModelScope(model);
                var registrationMethod = GetRegistrationMethod(scope);

                // Generate dependency injection registration for the concrete type
                if (model.ModelReferences.Any() || model.DIFields.Any())
                {
                    // Model has dependencies - use factory
                    var paramResolvers = new List<string>();

                    // Add model reference resolvers
                    paramResolvers.AddRange(model.ModelReferences.Select(mr =>
                        $"sp.GetRequiredService<{mr.ReferencedModelTypeName}>()"));

                    // Add DI field resolvers
                    paramResolvers.AddRange(model.DIFields.Select(df =>
                        $"sp.GetRequiredService<{df.FieldType}>()"));

                    var allResolvers = string.Join(", ", paramResolvers);
                    sb.AppendLine(
                        $"        services.{registrationMethod}<{model.ClassName}{model.GenericTypes}>(sp => new {model.ClassName}{model.GenericTypes}({allResolvers}));");
                }
                else
                {
                    // Model has no dependencies - simple registration
                    sb.AppendLine($"        services.{registrationMethod}<{model.ClassName}{model.GenericTypes}>();");
                }

                // Generate interface mappings for any implemented IObservableModel interfaces
                foreach (var interfaceType in model.ImplementedInterfaces)
                {
                    sb.AppendLine(
                        $"        services.{registrationMethod}<{interfaceType}>(sp => sp.GetRequiredService<{model.ClassName}{model.GenericTypes}>());");
                }

                sb.AppendLine("        return services;");
                sb.AppendLine("    }");
                sb.AppendLine("");
            }
            
            sb.AppendLine("}");
            context.AddSource($"GenericModelsServiceCollectionExtension.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
        catch (Exception ex)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.CodeGenerationError,
                Location.None,
                "AddObservableModelsExtension",
                ex.Message);
            context.ReportDiagnostic(diagnostic);
        } 
    }

    private static string GetModelScope(ObservableModelInfo model)
    {
        return model.ModelScope;
    }

    private static string GetRegistrationMethod(string scope)
    {
        return scope switch
        {
            "Singleton" => "AddSingleton",
            "Scoped" => "AddScoped",
            "Transient" => "AddTransient",
            _ => "AddSingleton"
        };
    }

    private static string ExtractNamespace(string fullyQualifiedTypeName)
    {
        var lastDotIndex = fullyQualifiedTypeName.LastIndexOf('.');
        return lastDotIndex > 0 ? fullyQualifiedTypeName.Substring(0, lastDotIndex) : "";
    }

    private static (string sourceModel, string property) ParseTriggerProperty(string triggerProperty,
        ObservableModelInfo modelInfo)
    {
        // Handle dot notation for referenced model properties (e.g., "ISettingsModel.AutoRefresh")
        if (triggerProperty.Contains('.'))
        {
            var parts = triggerProperty.Split('.');
            if (parts.Length == 2)
            {
                var modelName = parts[0];
                var propertyName = parts[1];

                // Check if this matches a model reference
                var modelRef = modelInfo.ModelReferences.FirstOrDefault(mr =>
                    mr.PropertyName == modelName || mr.ReferencedModelTypeName == modelName);

                if (modelRef != null)
                {
                    return (modelRef.PropertyName, propertyName);
                }
            }
        }

        // Local property (no dot notation)
        return ("", triggerProperty);
    }

    private static string GetCombinedTriggerCondition(CommandPropertyInfo command, CommandTriggerInfo trigger)
    {
        var conditions = new List<string>();

        // Add canExecuteMethod if present
        if (!string.IsNullOrEmpty(command.CanExecuteMethod))
        {
            conditions.Add($"{command.CanExecuteMethod}()");
        }

        // Add canTriggerMethod if present
        if (!string.IsNullOrEmpty(trigger.CanTriggerMethod))
        {
            conditions.Add($"{trigger.CanTriggerMethod}()");
        }

        // Combine with logical AND if multiple conditions exist
        return conditions.Count switch
        {
            0 => "",
            1 => conditions[0],
            _ => string.Join(" && ", conditions)
        };
    }
}
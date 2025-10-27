using Microsoft.CodeAnalysis;
using RxBlazorV2Generator.Analysis;
using RxBlazorV2Generator.Helpers;
using RxBlazorV2Generator.Models;
using System.Collections.Immutable;
using System.Linq;

namespace RxBlazorV2Generator.Builders;

/// <summary>
/// Builds the unified GeneratorContext containing all metadata about observable models and components
/// from both current and referenced assemblies. This serves as the single source of truth during generation.
/// </summary>
public static class GeneratorContextBuilder
{
    /// <summary>
    /// Builds the unified context from processed records and compilation.
    /// NEW ARCHITECTURE: Collects ALL observable entities first, then enriches with record data.
    /// This ensures complete inheritance hierarchies are tracked.
    /// </summary>
    public static GeneratorContext Build(
        ImmutableArray<ObservableModelRecord?> currentAssemblyRecords,
        Compilation compilation)
    {
        var components = new Dictionary<string, ComponentMetadata>();
        var models = new Dictionary<string, ModelMetadata>();

        // Step 1: Collect ALL ObservableModel types from current assembly (including base models)
        var allCurrentModels = CollectAllObservableModelsInCurrentAssembly(compilation);

        // Step 2: Build complete inheritance hierarchy for each model
        foreach (var kvp in allCurrentModels)
        {
            var fullyQualifiedName = kvp.Key;
            var typeSymbol = kvp.Value;

            var (allProperties, directProperties, baseModelType) = BuildModelInheritanceHierarchy(typeSymbol);

            var modelMetadata = new ModelMetadata(
                fullyQualifiedName: fullyQualifiedName,
                namespaceName: typeSymbol.ContainingNamespace?.ToDisplayString() ?? "",
                className: typeSymbol.Name,
                source: AssemblySource.Current,
                modelInfo: null, // Will be enriched in Step 3 if record exists
                baseModelTypeName: baseModelType?.ToDisplayString(),
                allProperties: allProperties,
                directProperties: directProperties,
                commandProperties: [], // Will be enriched from record
                modelReferences: []); // Will be enriched from record

            models[fullyQualifiedName] = modelMetadata;
        }

        // Step 3: Enrich models with record data (for models with [ObservableModelScope] etc.)
        EnrichModelsFromRecords(currentAssemblyRecords, components, models);

        // Step 4: Process referenced assemblies
        ProcessReferencedAssemblies(compilation, components, models);

        // Step 5: Calculate filterable properties for ALL components (fixes cross-assembly bug)
        CalculateFilterablePropertiesForAllComponents(components, models);

        return new GeneratorContext(components, models);
    }

    /// <summary>
    /// Enriches models with data from processed records.
    /// Models dictionary already contains ALL ObservableModels with inheritance info.
    /// This adds ModelInfo, commands, and component info for models with attributes.
    /// </summary>
    private static void EnrichModelsFromRecords(
        ImmutableArray<ObservableModelRecord?> records,
        Dictionary<string, ComponentMetadata> components,
        Dictionary<string, ModelMetadata> models)
    {
        foreach (var record in records.Where(r => r is not null && r!.ShouldGenerateCode))
        {
            if (record is null)
            {
                continue;
            }

            var modelInfo = record.ModelInfo;
            var fullyQualifiedName = modelInfo.FullyQualifiedName;

            // Model should already exist from Step 2, enrich it with record data
            if (models.TryGetValue(fullyQualifiedName, out var existingModel))
            {
                // Create enriched model metadata
                var enrichedModel = new ModelMetadata(
                    fullyQualifiedName: fullyQualifiedName,
                    namespaceName: existingModel.Namespace,
                    className: existingModel.ClassName,
                    source: AssemblySource.Current,
                    modelInfo: modelInfo, // Add the record's ModelInfo
                    baseModelTypeName: existingModel.BaseModelTypeName,
                    allProperties: existingModel.AllProperties, // Keep the complete hierarchy
                    directProperties: existingModel.DirectProperties,
                    commandProperties: [.. modelInfo.CommandProperties.Select(c => c.Name)],
                    modelReferences: modelInfo.ModelReferences);

                models[fullyQualifiedName] = enrichedModel;
            }

            // Add component metadata if this model has a component
            if (record.ComponentInfo is { } componentInfo)
            {
                // ComponentTriggers already includes referenced model triggers when includeReferencedTriggers is true
                var hasTriggers = componentInfo.ComponentTriggers.Count > 0;

                var componentMetadata = new ComponentMetadata(
                    fullyQualifiedName: $"{componentInfo.ComponentNamespace}.{componentInfo.ComponentClassName}",
                    namespaceName: componentInfo.ComponentNamespace,
                    className: componentInfo.ComponentClassName,
                    source: AssemblySource.Current,
                    filterableProperties: [], // Will be calculated in Step 5
                    hasTriggers: hasTriggers,
                    baseComponentType: null, // Will be determined in Step 5 if there's inheritance
                    baseComponentNamespace: null,
                    componentInfo: componentInfo);

                components[componentInfo.ComponentClassName] = componentMetadata;
            }
        }
    }

    /// <summary>
    /// Processes referenced assemblies to find cross-assembly ObservableComponents and their models.
    /// This consolidates the logic from ComponentDetectionExtensions.
    /// </summary>
    private static void ProcessReferencedAssemblies(
        Compilation compilation,
        Dictionary<string, ComponentMetadata> components,
        Dictionary<string, ModelMetadata> models)
    {
        var referencedComponents = compilation
            .SourceModule
            .ReferencedAssemblySymbols
            .Where(assembly => ReferencesRxBlazorV2(assembly))
            .SelectMany(assembly => GetAllObservableComponentsInAssembly(assembly));

        foreach (var componentSymbol in referencedComponents)
        {
            var namespaceName = componentSymbol.ContainingNamespace.ToDisplayString();
            var className = componentSymbol.Name;
            var fullyQualifiedName = componentSymbol.ToDisplayString();

            // Determine base component type, namespace, and model type
            string? baseComponentType = null;
            string? baseComponentNamespace = null;
            string? modelTypeName = null;

            if (componentSymbol.BaseType is { } baseType &&
                baseType.Name.StartsWith("ObservableComponent"))
            {
                baseComponentType = baseType.Name;
                baseComponentNamespace = baseType.ContainingNamespace.ToDisplayString();

                // Also track the model from the referenced assembly with full inheritance
                // ObservableComponent<TModel> has the model as its first type argument
                if (baseType.TypeArguments.Length > 0)
                {
                    var modelType = baseType.TypeArguments[0] as INamedTypeSymbol;
                    if (modelType is not null)
                    {
                        var modelFullName = modelType.ToDisplayString();
                        modelTypeName = modelFullName; // Store for component metadata

                        // Add the model to our models dictionary if not already present
                        if (!models.ContainsKey(modelFullName))
                        {
                            // Build complete inheritance hierarchy for referenced model
                            var (allProperties, directProperties, baseModelType) = BuildModelInheritanceHierarchy(modelType);

                            // Extract model references from constructor parameters
                            var modelReferences = ExtractModelReferencesFromSymbol(modelType, models);

                            // For each model reference, ensure the referenced model is also in our dictionary
                            foreach (var modelRef in modelReferences)
                            {
                                if (!models.ContainsKey(modelRef.ReferencedModelTypeName))
                                {
                                    // Recursively add the referenced model
                                    if (modelRef.TypeSymbol is INamedTypeSymbol refModelType)
                                    {
                                        var (refAllProps, refDirectProps, refBaseModel) = BuildModelInheritanceHierarchy(refModelType);
                                        var nestedRefs = ExtractModelReferencesFromSymbol(refModelType, models);

                                        models[modelRef.ReferencedModelTypeName] = new ModelMetadata(
                                            fullyQualifiedName: modelRef.ReferencedModelTypeName,
                                            namespaceName: refModelType.ContainingNamespace?.ToDisplayString() ?? "",
                                            className: refModelType.Name,
                                            source: AssemblySource.Referenced,
                                            modelInfo: null,
                                            baseModelTypeName: refBaseModel?.ToDisplayString(),
                                            allProperties: refAllProps,
                                            directProperties: refDirectProps,
                                            commandProperties: [],
                                            modelReferences: nestedRefs);
                                    }
                                }
                            }

                            models[modelFullName] = new ModelMetadata(
                                fullyQualifiedName: modelFullName,
                                namespaceName: modelType.ContainingNamespace?.ToDisplayString() ?? "",
                                className: modelType.Name,
                                source: AssemblySource.Referenced,
                                modelInfo: null,
                                baseModelTypeName: baseModelType?.ToDisplayString(),
                                allProperties: allProperties,
                                directProperties: directProperties,
                                commandProperties: [],
                                modelReferences: modelReferences);
                        }
                    }
                }
            }

            var componentMetadata = new ComponentMetadata(
                fullyQualifiedName: fullyQualifiedName,
                namespaceName: namespaceName,
                className: className,
                source: AssemblySource.Referenced,
                filterableProperties: [], // Will be calculated in Step 3
                hasTriggers: false, // Cross-assembly components don't generate triggers in current assembly
                baseComponentType: baseComponentType,
                baseComponentNamespace: baseComponentNamespace,
                componentInfo: null,
                modelTypeName: modelTypeName);

            components[className] = componentMetadata;
        }
    }

    /// <summary>
    /// Calculates filterable properties for ALL components (both current and referenced assemblies).
    /// This ensures EVERY component has the "Model." prefix, fixing the cross-assembly bug.
    /// Consolidates logic from FilterablePropertiesBuilder.
    /// </summary>
    private static void CalculateFilterablePropertiesForAllComponents(
        Dictionary<string, ComponentMetadata> components,
        Dictionary<string, ModelMetadata> models)
    {
        // Build a lookup of models by fully qualified name for quick reference resolution
        var modelsByFullName = models.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value);

        foreach (var kvp in components.ToList())
        {
            var componentName = kvp.Key;
            var component = kvp.Value;

            // For current assembly components, we have the ModelInfo
            if (component.Source == AssemblySource.Current && component.ComponentInfo is not null)
            {
                // Include generic types in the lookup key for generic models
                var modelFullName = $"{component.ComponentInfo.ModelNamespace}.{component.ComponentInfo.ModelTypeName}{component.ComponentInfo.GenericTypes}";

                if (modelsByFullName.TryGetValue(modelFullName, out var model) && model.ModelInfo is not null)
                {
                    var filterableProperties = BuildFilterablePropertiesForModel(model, modelsByFullName);

                    // Update the component with calculated properties
                    components[componentName] = new ComponentMetadata(
                        component.FullyQualifiedName,
                        component.Namespace,
                        component.ClassName,
                        component.Source,
                        filterableProperties,
                        component.HasTriggers,
                        component.BaseComponentType,
                        component.BaseComponentNamespace,
                        component.ComponentInfo,
                        component.ModelTypeName);
                }
            }
            // For referenced assembly components, use the stored ModelTypeName
            else if (component.Source == AssemblySource.Referenced && component.ModelTypeName is not null)
            {
                // Look up the model directly using the stored type name
                if (modelsByFullName.TryGetValue(component.ModelTypeName, out var model))
                {
                    var filterableProperties = BuildFilterablePropertiesForModel(model, modelsByFullName);

                    // Update the component with calculated properties
                    components[componentName] = new ComponentMetadata(
                        component.FullyQualifiedName,
                        component.Namespace,
                        component.ClassName,
                        component.Source,
                        filterableProperties,
                        component.HasTriggers,
                        component.BaseComponentType,
                        component.BaseComponentNamespace,
                        component.ComponentInfo,
                        component.ModelTypeName);
                }
            }
        }
    }

    /// <summary>
    /// Builds set of all filterable property names for a specific model.
    /// Includes the model's own properties (direct + inherited) and recursively includes referenced model properties.
    /// ALL properties have the "Model." prefix.
    /// </summary>
    private static HashSet<string> BuildFilterablePropertiesForModel(
        ModelMetadata model,
        Dictionary<string, ModelMetadata> modelsByFullName)
    {
        var properties = new HashSet<string>();

        // Add ALL properties (direct + inherited): "Model.PropertyName"
        // This fixes the regression where inherited properties (e.g., LogEntries from SampleBaseModel) were not tracked
        foreach (var propName in model.AllProperties)
        {
            properties.Add($"Model.{propName}");
        }

        // Add all command properties: "Model.CommandName"
        foreach (var cmdName in model.CommandProperties)
        {
            properties.Add($"Model.{cmdName}");
        }

        // Add referenced model properties
        foreach (var modelRef in model.ModelReferences)
        {
            // Try to find the referenced model
            var referencedModelFullName = modelRef.ReferencedModelTypeName;

            if (modelsByFullName.TryGetValue(referencedModelFullName, out var referencedModel))
            {
                // Add ALL properties (direct + inherited) from the referenced model: "Model.Settings.PropertyName"
                foreach (var propName in referencedModel.AllProperties)
                {
                    properties.Add($"Model.{modelRef.PropertyName}.{propName}");
                }

                // Add all command properties from the referenced model: "Model.Settings.CommandName"
                foreach (var cmdName in referencedModel.CommandProperties)
                {
                    properties.Add($"Model.{modelRef.PropertyName}.{cmdName}");
                }
            }
        }

        return properties;
    }

    /// <summary>
    /// Builds filterable properties for a referenced assembly component by walking its base type hierarchy.
    /// This ensures cross-assembly components get proper "Model." prefixed properties.
    /// </summary>
    private static HashSet<string> BuildFilterablePropertiesForReferencedComponent(
        INamedTypeSymbol componentSymbol,
        Dictionary<string, ComponentMetadata> components,
        Dictionary<string, ModelMetadata> models)
    {
        var properties = new HashSet<string>();

        // Walk the inheritance hierarchy to find the base ObservableComponent<TModel>
        var currentType = componentSymbol.BaseType;
        while (currentType is not null)
        {
            // Check if this is an ObservableComponent<TModel>
            if (currentType.Name.StartsWith("ObservableComponent") &&
                currentType.TypeArguments.Length > 0)
            {
                var modelType = currentType.TypeArguments[0];
                var modelFullName = modelType.ToDisplayString();

                // Try to find the model in our models dictionary
                if (models.TryGetValue(modelFullName, out var model))
                {
                    // Both current and referenced assembly models now have AllProperties populated
                    // This includes inherited properties from base ObservableModel classes
                    var modelProperties = BuildFilterablePropertiesForModel(model, models);
                    foreach (var prop in modelProperties)
                    {
                        properties.Add(prop);
                    }
                }

                break; // Found the model, no need to go further up the hierarchy
            }

            currentType = currentType.BaseType;
        }

        return properties;
    }

    /// <summary>
    /// Checks if an assembly references RxBlazorV2.
    /// </summary>
    private static bool ReferencesRxBlazorV2(IAssemblySymbol assembly)
    {
        return assembly.Modules
            .SelectMany(module => module.ReferencedAssemblies)
            .Any(reference => reference.Name == "RxBlazorV2");
    }

    /// <summary>
    /// Finds all ObservableComponent types in an assembly.
    /// </summary>
    private static IEnumerable<INamedTypeSymbol> GetAllObservableComponentsInAssembly(IAssemblySymbol assembly)
    {
        return GetObservableComponentsRecursive(assembly.GlobalNamespace);
    }

    /// <summary>
    /// Recursively traverses namespace hierarchy to find all types that inherit from ObservableComponent.
    /// </summary>
    private static IEnumerable<INamedTypeSymbol> GetObservableComponentsRecursive(INamespaceOrTypeSymbol symbol)
    {
        if (symbol.IsNamespace)
        {
            foreach (var member in symbol.GetMembers())
            {
                if (member is INamespaceOrTypeSymbol namespaceOrType)
                {
                    foreach (var component in GetObservableComponentsRecursive(namespaceOrType))
                    {
                        yield return component;
                    }
                }
            }
        }
        else if (symbol is INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.BaseType is not null &&
                namedTypeSymbol.BaseType.Name.StartsWith("ObservableComponent"))
            {
                yield return namedTypeSymbol;
            }
        }
    }

    /// <summary>
    /// Collects ALL ObservableModel types in the current assembly (regardless of attributes).
    /// This ensures we capture base models like SampleBaseModel that might not have [ObservableModelScope].
    /// For generic types, uses BOTH the definition form (with type parameters) AND the base name.
    /// </summary>
    private static Dictionary<string, INamedTypeSymbol> CollectAllObservableModelsInCurrentAssembly(Compilation compilation)
    {
        var models = new Dictionary<string, INamedTypeSymbol>();

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();

            foreach (var typeDecl in root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>())
            {
                var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl) as INamedTypeSymbol;
                if (typeSymbol is null)
                {
                    continue;
                }

                // Check if this type inherits from ObservableModel
                if (InheritsFromObservableModel(typeSymbol))
                {
                    var fullyQualifiedName = typeSymbol.ToDisplayString();
                    models[fullyQualifiedName] = typeSymbol;
                }
            }
        }

        return models;
    }

    /// <summary>
    /// Checks if a type inherits from ObservableModel (directly or indirectly).
    /// </summary>
    private static bool InheritsFromObservableModel(INamedTypeSymbol typeSymbol)
    {
        var currentType = typeSymbol.BaseType;
        while (currentType is not null)
        {
            if (currentType.Name == "ObservableModel")
            {
                return true;
            }
            currentType = currentType.BaseType;
        }
        return false;
    }

    /// <summary>
    /// Extracts all public properties from a type symbol.
    /// Returns property names without any prefix.
    /// </summary>
    private static HashSet<string> ExtractPropertiesFromTypeSymbol(INamedTypeSymbol typeSymbol)
    {
        var properties = new HashSet<string>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is IPropertySymbol propertySymbol &&
                propertySymbol.DeclaredAccessibility == Accessibility.Public &&
                !propertySymbol.IsStatic)
            {
                properties.Add(propertySymbol.Name);
            }
        }

        return properties;
    }

    /// <summary>
    /// Builds complete property hierarchy for a model by walking its base type chain.
    /// Returns (allProperties, directProperties, baseModelType).
    /// ALL properties are accumulated from the complete inheritance chain.
    /// </summary>
    private static (HashSet<string> AllProperties, HashSet<string> DirectProperties, INamedTypeSymbol? BaseModelType)
        BuildModelInheritanceHierarchy(INamedTypeSymbol typeSymbol)
    {
        var allProperties = new HashSet<string>();
        var directProperties = ExtractPropertiesFromTypeSymbol(typeSymbol);

        // Add direct properties to all properties
        foreach (var prop in directProperties)
        {
            allProperties.Add(prop);
        }

        // Find the base ObservableModel (if any)
        INamedTypeSymbol? baseModelType = null;
        var currentBase = typeSymbol.BaseType;

        while (currentBase is not null)
        {
            // If we hit ObservableModel itself, stop (it's the root base class)
            if (currentBase.Name == "ObservableModel")
            {
                break;
            }

            // Check if this base type is also an ObservableModel descendant
            if (InheritsFromObservableModel(currentBase))
            {
                // This is the immediate base model
                if (baseModelType is null)
                {
                    baseModelType = currentBase;
                }

                // Extract properties from this base model
                var baseProperties = ExtractPropertiesFromTypeSymbol(currentBase);
                foreach (var prop in baseProperties)
                {
                    allProperties.Add(prop);
                }
            }

            currentBase = currentBase.BaseType;
        }

        return (allProperties, directProperties, baseModelType);
    }

    /// <summary>
    /// Extracts model references from a type symbol by analyzing its constructor parameters.
    /// Only works for types from referenced assemblies (no syntax tree available).
    /// Returns list of ModelReferenceInfo for parameters that inherit from ObservableModel.
    /// </summary>
    private static List<ModelReferenceInfo> ExtractModelReferencesFromSymbol(
        INamedTypeSymbol typeSymbol,
        Dictionary<string, ModelMetadata> models)
    {
        var modelReferences = new List<ModelReferenceInfo>();

        // Find constructors (usually there's only one for ObservableModels)
        var constructors = typeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Constructor && !m.IsStatic)
            .ToList();

        if (constructors.Count == 0)
        {
            return modelReferences;
        }

        // Use the first public constructor
        var constructor = constructors.FirstOrDefault(c => c.DeclaredAccessibility == Accessibility.Public)
                         ?? constructors.FirstOrDefault();

        if (constructor is null)
        {
            return modelReferences;
        }

        // Analyze each parameter
        foreach (var parameter in constructor.Parameters)
        {
            if (parameter.Type is not INamedTypeSymbol paramType)
            {
                continue;
            }

            // Check if this parameter is an ObservableModel
            if (InheritsFromObservableModel(paramType))
            {
                var parameterTypeName = paramType.ToDisplayString();
                var propertyName = ToPascalCase(parameter.Name);

                // Check if this is a derived ObservableModel
                var baseObservableModelType = GetObservableModelBaseType(paramType);
                var isDerivedModel = baseObservableModelType is not null;
                var baseTypeName = baseObservableModelType?.ToDisplayString();

                modelReferences.Add(new ModelReferenceInfo(
                    parameterTypeName,
                    paramType.ContainingNamespace.ToDisplayString(),
                    propertyName,
                    [], // Cannot analyze usage without syntax tree
                    null,
                    isDerivedModel,
                    baseTypeName,
                    paramType));
            }
        }

        return modelReferences;
    }

    /// <summary>
    /// Gets the base ObservableModel type for a given type symbol.
    /// Returns null if the type directly inherits from ObservableModel.
    /// </summary>
    private static INamedTypeSymbol? GetObservableModelBaseType(INamedTypeSymbol typeSymbol)
    {
        var currentBase = typeSymbol.BaseType;

        while (currentBase is not null)
        {
            // If we hit ObservableModel itself, the type directly inherits from it
            if (currentBase.Name == "ObservableModel")
            {
                return null;
            }

            // Check if this base type is also an ObservableModel descendant
            if (InheritsFromObservableModel(currentBase))
            {
                return currentBase;
            }

            currentBase = currentBase.BaseType;
        }

        return null;
    }

    /// <summary>
    /// Converts a camelCase or snake_case string to PascalCase.
    /// </summary>
    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Handle snake_case
        if (input.Contains('_'))
        {
            var parts = input.Split('_');
            return string.Join("", parts.Select(p =>
                string.IsNullOrEmpty(p) ? "" : char.ToUpper(p[0]) + p.Substring(1)));
        }

        // Handle camelCase
        return char.ToUpper(input[0]) + input.Substring(1);
    }
}

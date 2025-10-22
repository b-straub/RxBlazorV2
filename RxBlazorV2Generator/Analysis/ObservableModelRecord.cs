using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Diagnostics;
using RxBlazorV2Generator.Extensions;
using RxBlazorV2Generator.Models;

namespace RxBlazorV2Generator.Analysis;

/// <summary>
/// Represents a complete analysis record for an ObservableModel class.
/// This is the single source of truth for ObservableModel analysis and diagnostics.
/// Follows the DexieNET pattern for clean separation between analysis and diagnostic reporting.
/// </summary>
public class ObservableModelRecord
{
    public ObservableModelInfo ModelInfo { get; }
    private readonly List<Diagnostic> _diagnostics;

    /// <summary>
    /// Indicates whether code should be generated for this model.
    /// False when the model has fatal errors (e.g., missing partial modifier).
    /// </summary>
    public bool ShouldGenerateCode { get; private set; }

    private ObservableModelRecord(ObservableModelInfo modelInfo, List<Diagnostic> diagnostics, bool shouldGenerateCode = true)
    {
        ModelInfo = modelInfo;
        _diagnostics = diagnostics;
        ShouldGenerateCode = shouldGenerateCode;
    }

    /// <summary>
    /// Creates an ObservableModelRecord by analyzing the class declaration.
    /// Returns null if the class is not an ObservableModel or analysis fails.
    /// This is the single place where ObservableModel analysis happens - used by both analyzer and generator.
    /// </summary>
    public static ObservableModelRecord? Create(
        ClassDeclarationSyntax classDecl,
        SemanticModel semanticModel,
        Compilation compilation,
        ServiceInfoList? serviceClasses)
    {
        try
        {
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
            if (classSymbol is not INamedTypeSymbol namedTypeSymbol)
                return null;

            // Check if class inherits from ObservableModel
            if (!namedTypeSymbol.InheritsFromObservableModel())
                return null;

            var diagnostics = new List<Diagnostic>();
            var shouldGenerateCode = true;

            // Check if class is missing 'partial' modifier (RXBG072)
            var isPartial = classDecl.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword));
            if (!isPartial)
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.ObservableEntityMissingPartialModifierError,
                    classDecl.Identifier.GetLocation(),
                    "Class",
                    namedTypeSymbol.Name,
                    "inherits from ObservableModel",
                    "class");
                diagnostics.Add(diagnostic);

                // NOTE: For non-partial classes, skip all further analysis and create minimal record
                // This prevents cascading diagnostics for properties/commands when the class itself is broken
                var minimalModelInfo = new ObservableModelInfo(
                    namedTypeSymbol.ContainingNamespace.ToDisplayString(),
                    namedTypeSymbol.Name,
                    namedTypeSymbol.ToDisplayString(),
                    [],  // No partial properties
                    [],  // No command properties
                    [],  // No methods
                    [],  // No model references
                    "Singleton",  // Default scope
                    [],  // No DI fields
                    [],  // No interfaces
                    string.Empty,  // No generic types
                    string.Empty,  // No type constraints
                    [],  // No using statements
                    null,  // No base model
                    "public",  // Default accessibility
                    "public"   // Default class accessibility
                );

                return new ObservableModelRecord(minimalModelInfo, diagnostics, shouldGenerateCode: false);
            }

            // Extract all components using extension methods
            var methods = classDecl.CollectMethods();
            var (partialProperties, partialPropertyDiagnostics) = classDecl.ExtractPartialPropertiesWithDiagnostics(semanticModel);
            var (commandProperties, commandPropertiesDiagnostics) = classDecl.ExtractCommandPropertiesWithDiagnostics(methods, semanticModel);

            // Extract model references and DI fields from partial constructor parameters
            var (modelReferences, diFields, unregisteredServices, diFieldsWithScope) = classDecl.ExtractPartialConstructorDependencies(semanticModel, serviceClasses);

            // Extract additional DI fields from private field declarations
            var additionalDIFields = classDecl.ExtractDIFields(semanticModel, serviceClasses);
            diFields.AddRange(additionalDIFields);

            var (modelScope, hasScopeAttribute) = classDecl.ExtractModelScopeWithAttributeCheck(semanticModel);
            var implementedInterfaces = namedTypeSymbol.ExtractObservableModelInterfaces();
            var genericTypes = namedTypeSymbol.ExtractObservableModelGenericTypes();
            var typeConstrains = classDecl.ExtractTypeConstrains();
            var usingStatements = classDecl.ExtractUsingStatements();
            var baseModelType = namedTypeSymbol.GetObservableModelBaseType();
            var baseModelTypeName = baseModelType?.ToDisplayString();
            var constructorAccessibility = classDecl.GetConstructorAccessibility();
            var classAccessibility = classDecl.GetClassAccessibility();

            // Add diagnostics from partial properties and command properties analysis
            diagnostics.AddRange(partialPropertyDiagnostics);
            diagnostics.AddRange(commandPropertiesDiagnostics);

            // Check for missing ObservableModelScope attribute (RXBG070)
            if (!hasScopeAttribute)
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.MissingObservableModelScopeWarning,
                    classDecl.Identifier.GetLocation(),
                    namedTypeSymbol.Name);
                diagnostics.Add(diagnostic);
            }

            // Check for non-public partial constructor with parameters (RXBG071)
            var partialConstructor = classDecl.GetPartialConstructor();
            if (partialConstructor is not null && partialConstructor.ParameterList.Parameters.Count > 0)
            {
                if (constructorAccessibility != "public")
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.NonPublicPartialConstructorError,
                        partialConstructor.Identifier.GetLocation(),
                        namedTypeSymbol.Name,
                        constructorAccessibility);
                    diagnostics.Add(diagnostic);
                }
            }

            // Create model info
            var modelInfo = new ObservableModelInfo(
                namedTypeSymbol.ContainingNamespace.ToDisplayString(),
                namedTypeSymbol.Name,
                namedTypeSymbol.ToDisplayString(),
                partialProperties,
                commandProperties,
                methods,
                modelReferences,
                modelScope,
                diFields,
                implementedInterfaces,
                genericTypes,
                typeConstrains,
                usingStatements,
                baseModelTypeName,
                constructorAccessibility,
                classAccessibility);

            // Build symbol map for model references
            var modelSymbolMap = new Dictionary<string, ITypeSymbol>();
            foreach (var modelRef in modelReferences)
            {
                if (modelRef.TypeSymbol is not null)
                {
                    modelSymbolMap[modelRef.PropertyName] = modelRef.TypeSymbol;
                }
            }

            // Enhance model references with command method analysis
            var enhancedModelReferences = modelInfo.EnhanceModelReferencesWithCommandAnalysis(
                semanticModel,
                modelSymbolMap);

            // Check for unused model references and derived model issues (RXBG008, RXBG017)
            var modelReferenceDiagnostics = enhancedModelReferences.CreateUnusedModelReferenceDiagnostics(
                namedTypeSymbol,
                classDecl,
                compilation);
            diagnostics.AddRange(modelReferenceDiagnostics);

            // NOTE: Shared model scoping (RXBG014) is now checked in generator post-step
            // by counting component usage in razor files

            // Store unregistered services and DI scope violations for later diagnostic generation
            // These are stored as properties so the generator can report them (RXBG020, RXBG021)
            var record = new ObservableModelRecord(
                new ObservableModelInfo(
                    namedTypeSymbol.ContainingNamespace.ToDisplayString(),
                    namedTypeSymbol.Name,
                    namedTypeSymbol.ToDisplayString(),
                    partialProperties,
                    commandProperties,
                    methods,
                    enhancedModelReferences,
                    modelScope,
                    diFields,
                    implementedInterfaces,
                    genericTypes,
                    typeConstrains,
                    usingStatements,
                    baseModelTypeName,
                    constructorAccessibility,
                    classAccessibility),
                diagnostics,
                shouldGenerateCode);

            // Store unregistered services and DI scope info for generator diagnostic reporting
            record.UnregisteredServices = unregisteredServices;
            record.DiFieldsWithScope = diFieldsWithScope;

            // Extract component attribute data for later ComponentInfo generation
            ExtractComponentAttributeData(namedTypeSymbol, record);

            // Check for unused ObservableComponentTrigger attributes (RXBG041)
            CheckForUnusedComponentTriggers(namedTypeSymbol, classDecl, record, diagnostics);

            // ComponentInfo will be extracted later in the pipeline when all records are available
            // This allows looking up referenced model triggers from their ObservableModelRecords

            return record;
        }
        catch (Exception)
        {
            // Silently skip models that fail analysis
            return null;
        }
    }

    // Properties for generator-specific diagnostics (RXBG020, RXBG021)
    public List<(string paramName, string paramType, ITypeSymbol? typeSymbol, Location? location)> UnregisteredServices { get; private set; } = [];
    public List<(DIFieldInfo diField, string? serviceScope, Location? location)> DiFieldsWithScope { get; private set; } = [];

    // Component attribute data (extracted during Create for use in later pipeline stages)
    public bool HasObservableComponentAttribute { get; private set; }
    public bool IncludeReferencedTriggers { get; private set; } = true; // default
    public string? CustomComponentName { get; private set; }
    public string GenericTypes { get; private set; } = string.Empty;
    public string TypeConstraints { get; private set; } = string.Empty;
    // Property names with component triggers (propertyName -> (hasSync, syncHookName, hasAsync, asyncHookName))
    public Dictionary<string, (bool hasSync, string? syncHookName, bool hasAsync, string? asyncHookName)> ComponentTriggerProperties { get; private set; } = [];

    // Component information for component generation
    // Set later in pipeline when all records are available for referenced model lookup
    public ComponentInfo? ComponentInfo { get; set; }

    /// <summary>
    /// Returns all diagnostics for this model.
    /// This is the single source of truth for diagnostic logic.
    /// Note: RXBG020, RXBG021, and RXBG014 are reported by the generator in a separate pass.
    /// </summary>
    public List<Diagnostic> Verify()
    {
        return new List<Diagnostic>(_diagnostics);
    }

    /// <summary>
    /// Checks for ObservableComponentTrigger attributes that won't generate any code.
    /// This happens when a model has trigger attributes but:
    /// 1. Does NOT have [ObservableComponent] attribute (no component to generate hooks in)
    /// 2. Is NOT referenced by another model with [ObservableComponent(includeReferencedTriggers: true)]
    /// Note: Currently only checks condition 1 (no ObservableComponent). A full check for condition 2
    /// would require compilation-wide analysis to see if ANY model references this one.
    /// Reports RXBG041 warning for properties with unused trigger attributes.
    /// </summary>
    private static void CheckForUnusedComponentTriggers(
        INamedTypeSymbol namedTypeSymbol,
        ClassDeclarationSyntax classDecl,
        ObservableModelRecord record,
        List<Diagnostic> diagnostics)
    {
        // If model has [ObservableComponent], triggers are used - no warning needed
        if (record.HasObservableComponentAttribute)
        {
            return;
        }

        // If model has no trigger properties, nothing to check
        if (record.ComponentTriggerProperties.Count == 0)
        {
            return;
        }

        // Model has trigger attributes but no [ObservableComponent]
        // Report warning for each property with trigger attributes
        // Note: We can't easily check if model is referenced by another model with includeReferencedTriggers: true
        // at this point, so we report conservatively. The diagnostic message mentions both conditions.
        foreach (var member in namedTypeSymbol.GetMembers())
        {
            if (member is IPropertySymbol propertySymbol)
            {
                foreach (var attr in propertySymbol.GetAttributes())
                {
                    if (attr.AttributeClass?.Name == "ObservableComponentTriggerAttribute" ||
                        attr.AttributeClass?.Name == "ObservableComponentTriggerAsyncAttribute")
                    {
                        // Get the location of the attribute for precise error reporting
                        var location = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                            ?? propertySymbol.Locations.FirstOrDefault()
                            ?? classDecl.Identifier.GetLocation();

                        var diagnostic = Diagnostic.Create(
                            DiagnosticDescriptors.UnusedObservableComponentTriggerWarning,
                            location,
                            propertySymbol.Name,
                            namedTypeSymbol.Name);
                        diagnostics.Add(diagnostic);

                        // Only report once per property (even if it has both sync and async triggers)
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Extracts and stores component attribute data from [ObservableComponent] attribute.
    /// This is called during Create() to store data for later ComponentInfo generation.
    /// </summary>
    private static void ExtractComponentAttributeData(INamedTypeSymbol namedTypeSymbol, ObservableModelRecord record)
    {
        // Check for [ObservableComponent] attribute
        foreach (var attribute in namedTypeSymbol.GetAttributes())
        {
            if (attribute.AttributeClass?.Name == "ObservableComponentAttribute")
            {
                record.HasObservableComponentAttribute = true;

                // Extract includeReferencedTriggers (default true)
                if (attribute.ConstructorArguments.Length > 0 &&
                    attribute.ConstructorArguments[0].Value is bool includeRefTriggers)
                {
                    record.IncludeReferencedTriggers = includeRefTriggers;
                }

                // Extract custom component name
                if (attribute.ConstructorArguments.Length > 1 &&
                    attribute.ConstructorArguments[1].Value is string componentName &&
                    !string.IsNullOrWhiteSpace(componentName))
                {
                    record.CustomComponentName = componentName;
                }

                // Extract generic types and constraints
                record.GenericTypes = namedTypeSymbol.ExtractObservableModelGenericTypes();
                record.TypeConstraints = namedTypeSymbol.ContainingType?.DeclaringSyntaxReferences
                    .Select(r => r.GetSyntax())
                    .OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault()
                    ?.ExtractTypeConstrains()
                    ?? namedTypeSymbol.DeclaringSyntaxReferences
                    .Select(r => r.GetSyntax())
                    .OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault()
                    ?.ExtractTypeConstrains()
                    ?? string.Empty;

                break;
            }
        }

        // Extract component trigger properties with custom hook names
        var componentTriggers = new Dictionary<string, (bool hasSync, string? syncHookName, bool hasAsync, string? asyncHookName)>();
        foreach (var member in namedTypeSymbol.GetMembers())
        {
            if (member is IPropertySymbol propertySymbol)
            {
                var hasSyncTrigger = false;
                string? syncHookName = null;
                var hasAsyncTrigger = false;
                string? asyncHookName = null;

                foreach (var attr in propertySymbol.GetAttributes())
                {
                    if (attr.AttributeClass?.Name == "ObservableComponentTriggerAttribute")
                    {
                        hasSyncTrigger = true;
                        // Extract custom hook name from first constructor argument
                        if (attr.ConstructorArguments.Length > 0 &&
                            attr.ConstructorArguments[0].Value is string customName &&
                            !string.IsNullOrWhiteSpace(customName))
                        {
                            syncHookName = customName;
                        }
                    }
                    else if (attr.AttributeClass?.Name == "ObservableComponentTriggerAsyncAttribute")
                    {
                        hasAsyncTrigger = true;
                        // Extract custom hook name from first constructor argument
                        if (attr.ConstructorArguments.Length > 0 &&
                            attr.ConstructorArguments[0].Value is string customNameAsync &&
                            !string.IsNullOrWhiteSpace(customNameAsync))
                        {
                            asyncHookName = customNameAsync;
                        }
                    }
                }

                if (hasSyncTrigger || hasAsyncTrigger)
                {
                    componentTriggers[propertySymbol.Name] = (hasSyncTrigger, syncHookName, hasAsyncTrigger, asyncHookName);
                }
            }
        }
        record.ComponentTriggerProperties = componentTriggers;
    }

    /// <summary>
    /// Extracts component information from stored component attribute data.
    /// This method should be called in the generator pipeline when all ObservableModelRecords are available,
    /// allowing lookup of referenced model triggers from their records.
    /// For cross-assembly references, uses Compilation.GetTypeByMetadataName to read trigger attributes.
    /// </summary>
    public static ComponentInfo? ExtractComponentInfo(
        ObservableModelRecord currentRecord,
        Dictionary<string, ObservableModelRecord> allRecords,
        Compilation compilation)
    {
        try
        {
            // Check if model has [ObservableComponent] attribute
            if (!currentRecord.HasObservableComponentAttribute)
            {
                return null;
            }

            var modelInfo = currentRecord.ModelInfo;
            var componentClassName = currentRecord.CustomComponentName ?? $"{modelInfo.ClassName}Component";
            var componentNamespace = modelInfo.Namespace;

            // Extract component triggers from current model's properties
            var componentTriggers = new List<ComponentTriggerInfo>();

            foreach (var kvp in currentRecord.ComponentTriggerProperties)
            {
                var propertyName = kvp.Key;
                var (hasSync, syncHookName, hasAsync, asyncHookName) = kvp.Value;

                // Use custom hook names if provided, otherwise ComponentTriggerInfo will use defaults
                if (hasSync)
                {
                    componentTriggers.Add(new ComponentTriggerInfo(
                        propertyName,
                        TriggerHookType.Sync,
                        syncHookName)); // Custom or null for default: On{PropertyName}Changed
                }

                if (hasAsync)
                {
                    componentTriggers.Add(new ComponentTriggerInfo(
                        propertyName,
                        TriggerHookType.Async,
                        asyncHookName)); // Custom or null for default: On{PropertyName}ChangedAsync
                }
            }

            // Process referenced model triggers if includeReferencedTriggers is enabled
            var allComponentTriggers = new List<ComponentTriggerInfo>(componentTriggers);

            if (currentRecord.IncludeReferencedTriggers && modelInfo.ModelReferences.Count > 0)
            {
                foreach (var modelRef in modelInfo.ModelReferences)
                {
                    // Look up the referenced model's record
                    // ReferencedModelTypeName already contains the fully qualified name
                    var refFullTypeName = modelRef.ReferencedModelTypeName;

                    Dictionary<string, (bool hasSync, string? syncHookName, bool hasAsync, string? asyncHookName)> triggerProperties;

                    if (allRecords.TryGetValue(refFullTypeName, out var referencedRecord))
                    {
                        // Same assembly - use the record's component trigger properties
                        triggerProperties = referencedRecord.ComponentTriggerProperties;
                    }
                    else if (modelRef.TypeSymbol is not null)
                    {
                        // Cross-assembly reference - use the cached TypeSymbol to read trigger attributes
                        var referencedTypeSymbol = modelRef.TypeSymbol;

                        // Extract trigger properties from the type symbol
                        triggerProperties = new Dictionary<string, (bool hasSync, string? syncHookName, bool hasAsync, string? asyncHookName)>();
                        foreach (var member in referencedTypeSymbol.GetMembers())
                        {
                            if (member is IPropertySymbol propertySymbol)
                            {
                                bool hasSyncTrigger = false;
                                string? syncHookName = null;
                                bool hasAsyncTrigger = false;
                                string? asyncHookName = null;

                                foreach (var attr in propertySymbol.GetAttributes())
                                {
                                    if (attr.AttributeClass?.Name == "ObservableComponentTriggerAttribute")
                                    {
                                        hasSyncTrigger = true;
                                        // Extract custom hook name from first constructor argument
                                        if (attr.ConstructorArguments.Length > 0 &&
                                            attr.ConstructorArguments[0].Value is string customName &&
                                            !string.IsNullOrWhiteSpace(customName))
                                        {
                                            syncHookName = customName;
                                        }
                                    }
                                    else if (attr.AttributeClass?.Name == "ObservableComponentTriggerAsyncAttribute")
                                    {
                                        hasAsyncTrigger = true;
                                        // Extract custom hook name from first constructor argument
                                        if (attr.ConstructorArguments.Length > 0 &&
                                            attr.ConstructorArguments[0].Value is string customNameAsync &&
                                            !string.IsNullOrWhiteSpace(customNameAsync))
                                        {
                                            asyncHookName = customNameAsync;
                                        }
                                    }
                                }

                                if (hasSyncTrigger || hasAsyncTrigger)
                                {
                                    triggerProperties[propertySymbol.Name] = (hasSyncTrigger, syncHookName, hasAsyncTrigger, asyncHookName);
                                }
                            }
                        }
                    }
                    else
                    {
                        // TypeSymbol not available - skip this reference
                        continue;
                    }

                    // Get triggers from referenced model (same or cross-assembly)
                    foreach (var kvp in triggerProperties)
                    {
                        var propertyName = kvp.Key;
                        var (hasSync, syncHookName, hasAsync, asyncHookName) = kvp.Value;

                        // Build hook method name: On{ReferencedProperty}{PropertyName}Changed[Async]
                        // Example: OnSettingsIsDayChanged
                        // Note: Custom hook names from referenced model are ignored for parent component hooks
                        var defaultBaseName = $"On{modelRef.PropertyName}{propertyName}";

                        if (hasSync)
                        {
                            var hookName = $"{defaultBaseName}Changed";
                            allComponentTriggers.Add(new ComponentTriggerInfo(
                                propertyName,
                                TriggerHookType.Sync,
                                hookName,
                                modelRef.PropertyName));
                        }

                        if (hasAsync)
                        {
                            var hookName = $"{defaultBaseName}ChangedAsync";
                            allComponentTriggers.Add(new ComponentTriggerInfo(
                                propertyName,
                                TriggerHookType.Async,
                                hookName,
                                modelRef.PropertyName));
                        }
                    }
                }
            }

            return new ComponentInfo(
                componentClassName,
                componentNamespace,
                modelInfo.ClassName,
                modelInfo.Namespace,
                allComponentTriggers,
                currentRecord.GenericTypes,
                currentRecord.TypeConstraints,
                modelInfo.ModelReferences,
                modelInfo.DIFields,
                currentRecord.IncludeReferencedTriggers);
        }
        catch (Exception)
        {
            return null;
        }
    }
}

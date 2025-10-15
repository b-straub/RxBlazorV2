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

    private ObservableModelRecord(ObservableModelInfo modelInfo, List<Diagnostic> diagnostics)
    {
        ModelInfo = modelInfo;
        _diagnostics = diagnostics;
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

            // Extract all components using extension methods
            var methods = classDecl.CollectMethods();
            var (partialProperties, partialPropertyDiagnostics) = classDecl.ExtractPartialPropertiesWithDiagnostics(semanticModel);
            var (commandProperties, commandPropertiesDiagnostics) = classDecl.ExtractCommandPropertiesWithDiagnostics(methods, semanticModel);

            // Extract model references and DI fields from partial constructor parameters
            var (modelReferences, diFields, unregisteredServices, diFieldsWithScope) = classDecl.ExtractPartialConstructorDependencies(semanticModel, serviceClasses);

            // Extract additional DI fields from private field declarations
            var additionalDIFields = classDecl.ExtractDIFields(semanticModel, serviceClasses);
            diFields.AddRange(additionalDIFields);

            var modelScope = classDecl.ExtractModelScopeFromClass(semanticModel);
            var implementedInterfaces = namedTypeSymbol.ExtractObservableModelInterfaces();
            var genericTypes = namedTypeSymbol.ExtractObservableModelGenericTypes();
            var typeConstrains = classDecl.ExtractTypeConstrains();
            var usingStatements = classDecl.ExtractUsingStatements();
            var baseModelType = namedTypeSymbol.GetObservableModelBaseType();
            var baseModelTypeName = baseModelType?.ToDisplayString();
            var constructorAccessibility = classDecl.GetConstructorAccessibility();

            // Add diagnostics from partial properties and command properties analysis
            diagnostics.AddRange(partialPropertyDiagnostics);
            diagnostics.AddRange(commandPropertiesDiagnostics);

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
                constructorAccessibility);

            // Build symbol map for model references
            var modelSymbolMap = new Dictionary<string, ITypeSymbol>();
            foreach (var modelRef in modelReferences)
            {
                var fullTypeName = string.IsNullOrEmpty(modelRef.ReferencedModelNamespace)
                    ? modelRef.ReferencedModelTypeName
                    : $"{modelRef.ReferencedModelNamespace}.{modelRef.ReferencedModelTypeName}";

                var typeSymbol = compilation.GetTypeByMetadataName(fullTypeName);
                if (typeSymbol != null)
                {
                    modelSymbolMap[modelRef.PropertyName] = typeSymbol;
                }
            }

            // Enhance model references with command method analysis
            var enhancedModelReferences = modelInfo.EnhanceModelReferencesWithCommandAnalysis(
                semanticModel,
                modelSymbolMap);

            // Check for unused model references and derived model issues (RXBG008, RXBG017)
            var modelReferenceDiagnostics = enhancedModelReferences.CreateUnusedModelReferenceDiagnostics(
                namedTypeSymbol,
                classDecl);
            diagnostics.AddRange(modelReferenceDiagnostics);

            // Check for shared model scoping issues (RXBG007)
            var sharedModelDiagnostics = AnalyzeSharedModelScoping(namedTypeSymbol, classDecl, compilation);
            diagnostics.AddRange(sharedModelDiagnostics);

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
                    constructorAccessibility),
                diagnostics);

            // Store unregistered services and DI scope info for generator diagnostic reporting
            record.UnregisteredServices = unregisteredServices;
            record.DiFieldsWithScope = diFieldsWithScope;

            // Extract component information if [ObservableComponent] attribute is present
            record.ComponentInfo = ExtractComponentInfo(namedTypeSymbol, partialProperties);

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

    // Component information for component generation
    public ComponentInfo? ComponentInfo { get; private set; }

    /// <summary>
    /// Returns all diagnostics for this model.
    /// This is the single source of truth for diagnostic logic.
    /// Note: RXBG020 and RXBG021 are reported by the generator in a separate pass.
    /// </summary>
    public List<Diagnostic> Verify()
    {
        return new List<Diagnostic>(_diagnostics);
    }

    /// <summary>
    /// Analyzes shared model scoping using compilation information.
    /// Checks if a non-singleton model is shared across multiple components (RXBG007).
    /// </summary>
    private static List<Diagnostic> AnalyzeSharedModelScoping(
        INamedTypeSymbol namedTypeSymbol,
        ClassDeclarationSyntax classDecl,
        Compilation compilation)
    {
        var diagnostics = new List<Diagnostic>();

        try
        {
            // Get the model scope from attributes
            var modelScope = "Singleton"; // Default scope
            Location? attributeLocation = null;

            foreach (var attribute in namedTypeSymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.Name == "ObservableModelScopeAttribute")
                {
                    if (attribute.ConstructorArguments.Length > 0 &&
                        attribute.ConstructorArguments[0].Value is int scopeValue)
                    {
                        modelScope = scopeValue switch
                        {
                            0 => "Singleton",
                            1 => "Scoped",
                            2 => "Transient",
                            _ => "Singleton"
                        };
                    }
                    attributeLocation = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();
                    break;
                }
            }

            // Only check non-singleton models
            if (modelScope != "Singleton")
            {
                // Count how many components use this model
                var modelUsageCount = CountModelUsageInCompilation(namedTypeSymbol, compilation);

                if (modelUsageCount > 1)
                {
                    var location = attributeLocation ?? classDecl.GetLocation();
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.SharedModelNotSingletonError,
                        location,
                        namedTypeSymbol.ToDisplayString(),
                        modelScope);
                    diagnostics.Add(diagnostic);
                }
            }
        }
        catch (Exception)
        {
            // Silently skip on error
        }

        return diagnostics;
    }

    /// <summary>
    /// Counts how many components in the compilation use this model type.
    /// </summary>
    private static int CountModelUsageInCompilation(INamedTypeSymbol modelType, Compilation compilation)
    {
        var usageCount = 0;

        var allTypes = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .Where(type => GetObservableComponentBaseModelType(type) != null);

        foreach (var componentType in allTypes)
        {
            var baseModelType = GetObservableComponentBaseModelType(componentType);
            if (baseModelType != null && SymbolEqualityComparer.Default.Equals(baseModelType, modelType))
            {
                usageCount++;
            }
        }

        return usageCount;
    }

    /// <summary>
    /// Gets the model type from ObservableComponent&lt;T&gt; base class.
    /// </summary>
    private static INamedTypeSymbol? GetObservableComponentBaseModelType(INamedTypeSymbol classSymbol)
    {
        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "ObservableComponent" && baseType.TypeArguments.Length == 1)
            {
                return baseType.TypeArguments[0] as INamedTypeSymbol;
            }
            baseType = baseType.BaseType;
        }
        return null;
    }

    /// <summary>
    /// Extracts component information from [ObservableComponent] attribute if present.
    /// </summary>
    private static ComponentInfo? ExtractComponentInfo(
        INamedTypeSymbol namedTypeSymbol,
        List<PartialPropertyInfo> partialProperties)
    {
        try
        {
            // Check for [ObservableComponent] attribute
            AttributeData? componentAttribute = null;
            foreach (var attribute in namedTypeSymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.Name == "ObservableComponentAttribute")
                {
                    componentAttribute = attribute;
                    break;
                }
            }

            if (componentAttribute is null)
            {
                return null;
            }

            // Get component name from attribute parameter or default to {ModelName}Component
            string? customComponentName = null;
            if (componentAttribute.ConstructorArguments.Length > 0 &&
                componentAttribute.ConstructorArguments[0].Value is string componentName &&
                !string.IsNullOrWhiteSpace(componentName))
            {
                customComponentName = componentName;
            }

            var componentClassName = customComponentName ?? $"{namedTypeSymbol.Name}Component";

            // Determine component namespace (Components subfolder of model namespace)
            var modelNamespace = namedTypeSymbol.ContainingNamespace.ToDisplayString();
            var componentNamespace = $"{modelNamespace.Split('.')[0]}.Components";

            // Extract component triggers from properties
            var componentTriggers = new List<ComponentTriggerInfo>();
            foreach (var property in partialProperties)
            {
                // Check if property has trigger attributes
                var propertySymbol = namedTypeSymbol.GetMembers(property.Name)
                    .OfType<IPropertySymbol>()
                    .FirstOrDefault();

                if (propertySymbol is not null)
                {
                    bool hasSyncTrigger = false;
                    bool hasAsyncTrigger = false;
                    string? syncHookName = null;
                    string? asyncHookName = null;

                    foreach (var attribute in propertySymbol.GetAttributes())
                    {
                        if (attribute.AttributeClass?.Name == "ObservableComponentTriggerAttribute")
                        {
                            hasSyncTrigger = true;
                            if (attribute.ConstructorArguments.Length > 0 &&
                                attribute.ConstructorArguments[0].Value is string hookName &&
                                !string.IsNullOrWhiteSpace(hookName))
                            {
                                syncHookName = hookName;
                            }
                        }
                        else if (attribute.AttributeClass?.Name == "ObservableComponentTriggerAsyncAttribute")
                        {
                            hasAsyncTrigger = true;
                            if (attribute.ConstructorArguments.Length > 0 &&
                                attribute.ConstructorArguments[0].Value is string hookName &&
                                !string.IsNullOrWhiteSpace(hookName))
                            {
                                asyncHookName = hookName;
                            }
                        }
                    }

                    // Create trigger based on which attributes are present
                    if (hasSyncTrigger && hasAsyncTrigger)
                    {
                        // Both attributes present - create separate triggers
                        componentTriggers.Add(new ComponentTriggerInfo(property.Name, TriggerHookType.Sync, syncHookName));
                        componentTriggers.Add(new ComponentTriggerInfo(property.Name, TriggerHookType.Async, asyncHookName));
                    }
                    else if (hasSyncTrigger)
                    {
                        componentTriggers.Add(new ComponentTriggerInfo(property.Name, TriggerHookType.Sync, syncHookName));
                    }
                    else if (hasAsyncTrigger)
                    {
                        componentTriggers.Add(new ComponentTriggerInfo(property.Name, TriggerHookType.Async, asyncHookName));
                    }
                }
            }

            // Build batch subscriptions map
            var batchSubscriptions = new Dictionary<string, List<string>>();
            var modelProperties = new List<string>();

            foreach (var property in partialProperties)
            {
                if (property.BatchIds is not null && property.BatchIds.Length > 0)
                {
                    // Property has batches - add to Model subscriptions
                    modelProperties.Add(property.Name);
                }
                else
                {
                    // Property has no batch - also subscribe to changes
                    modelProperties.Add(property.Name);
                }
            }

            if (modelProperties.Any())
            {
                batchSubscriptions["Model"] = modelProperties;
            }

            return new ComponentInfo(
                componentClassName,
                componentNamespace,
                namedTypeSymbol.Name,
                modelNamespace,
                componentTriggers,
                batchSubscriptions);
        }
        catch (Exception)
        {
            return null;
        }
    }
}

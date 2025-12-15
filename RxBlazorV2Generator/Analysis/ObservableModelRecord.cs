using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Diagnostics;
using RxBlazorV2Generator.Extensions;
using RxBlazorV2Generator.Models;
using System.Collections.Immutable;

namespace RxBlazorV2Generator.Analysis;

/// <summary>
/// Represents a complete analysis record for an ObservableModel class.
/// This is the single source of truth for ObservableModel analysis and diagnostics.
/// Follows the DexieNET pattern for clean separation between analysis and diagnostic reporting.
/// Implements IEquatable for incremental generator caching.
/// </summary>
public class ObservableModelRecord : IEquatable<ObservableModelRecord>
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
    /// Creates an ObservableModelRecord by analyzing multiple partial class declarations.
    /// Returns null if the class is not an ObservableModel or analysis fails.
    /// This overload merges members from all partial class declarations.
    /// </summary>
    public static ObservableModelRecord? Create(
        List<ClassDeclarationSyntax> classDeclarations,
        SemanticModel semanticModel,
        Compilation compilation,
        ServiceInfoList? serviceClasses)
    {
        if (classDeclarations.Count == 0)
        {
            return null;
        }

        // If only one declaration, use the simpler single-declaration path
        if (classDeclarations.Count == 1)
        {
            return Create(classDeclarations[0], semanticModel, compilation, serviceClasses);
        }

        // Multiple partial declarations - merge them
        return CreateFromPartialDeclarations(classDeclarations, semanticModel, compilation, serviceClasses);
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

            // Extract model references, DI fields, and model observers from partial constructor parameters
            var (modelReferences, diFields, unregisteredServices, diFieldsWithScope, modelObservers, modelObserverDiagnostics) = classDecl.ExtractPartialConstructorDependencies(semanticModel, serviceClasses, namedTypeSymbol);

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

            // Add diagnostics from partial properties, command properties, and model observer analysis
            diagnostics.AddRange(partialPropertyDiagnostics);
            diagnostics.AddRange(commandPropertiesDiagnostics);
            diagnostics.AddRange(modelObserverDiagnostics);

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
                compilation,
                modelSymbolMap);

            // Check for unused model references and derived model issues (RXBG008, RXBG017)
            var modelReferenceDiagnostics = enhancedModelReferences.CreateUnusedModelReferenceDiagnostics(
                namedTypeSymbol,
                classDecl,
                compilation);
            diagnostics.AddRange(modelReferenceDiagnostics);

            // Build set of methods that should NOT be auto-detected as internal observers
            // These are methods already used as command execute/canExecute or trigger methods
            var excludedMethods = BuildExcludedMethodsSet(commandProperties, partialProperties);

            // Analyze private methods for internal model observers (auto-detection)
            // These are methods that access referenced model properties and will generate subscriptions
            var (internalModelObservers, invalidInternalObservers) = classDecl.AnalyzeInternalModelObserversWithDiagnostics(
                semanticModel,
                enhancedModelReferences,
                modelSymbolMap,
                excludedMethods);

            // Add RXBG031 (CircularTriggerReferenceError) for internal observers to diagnostics
            // so the analyzer can report them (with code fix support in IDE)
            foreach (var invalidObserver in invalidInternalObservers.Where(o => o.IsCircularReference))
            {
                if (invalidObserver.Location is not null)
                {
                    var circularProps = string.Join(", ", invalidObserver.CircularProperties
                        .Select(p => $"{invalidObserver.ModelReferenceName}.{p}"));

                    var properties = ImmutableDictionary.CreateBuilder<string, string?>();
                    properties.Add("CircularProperty", circularProps);
                    properties.Add("MethodName", invalidObserver.MethodName);
                    properties.Add("IsInternalObserver", "true");

                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.CircularTriggerReferenceError,
                        invalidObserver.Location,
                        properties.ToImmutable(),
                        invalidObserver.MethodName,
                        circularProps,
                        invalidObserver.MethodName);
                    diagnostics.Add(diagnostic);
                }
            }

            // NOTE: RXBG082 (InternalModelObserverInvalidSignatureWarning) is reported by generator
            // after cross-model analysis completes. Invalid observers are stored for later.

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
                    classAccessibility,
                    modelObservers,
                    internalModelObservers),
                diagnostics,
                shouldGenerateCode);

            // Store unregistered services, DI scope info, and invalid observers for generator diagnostic reporting
            record.UnregisteredServices = unregisteredServices;
            record.DiFieldsWithScope = diFieldsWithScope;
            record.InvalidInternalObservers = invalidInternalObservers;

            // Extract component attribute data for later ComponentInfo generation
            ExtractComponentAttributeData(namedTypeSymbol, record);

            // NOTE: RXBG041 (UnusedObservableComponentTriggerWarning) is checked in the generator
            // after all ComponentInfo is extracted, where we have the complete model reference graph
            // to determine if a model is referenced by another model with includeReferencedTriggers: true

            // ComponentInfo will be extracted later in the pipeline when all records are available
            // This allows looking up referenced model triggers from their ObservableModelRecords

            return record;
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected, just return null
            return null;
        }
    }

    /// <summary>
    /// Creates an ObservableModelRecord by merging members from multiple partial class declarations.
    /// </summary>
    private static ObservableModelRecord? CreateFromPartialDeclarations(
        List<ClassDeclarationSyntax> classDeclarations,
        SemanticModel semanticModel,
        Compilation compilation,
        ServiceInfoList? serviceClasses)
    {
        try
        {
            // Find the "primary" declaration (the one with `: ObservableModel` or attributes)
            var primaryDecl = classDeclarations.FirstOrDefault(d => d.BaseList?.Types.Any() == true)
                ?? classDeclarations[0];

            // Get the correct semantic model for the primary declaration's syntax tree
            var primarySemanticModel = compilation.GetSemanticModel(primaryDecl.SyntaxTree);

            var classSymbol = primarySemanticModel.GetDeclaredSymbol(primaryDecl);
            if (classSymbol is not INamedTypeSymbol namedTypeSymbol)
            {
                return null;
            }

            // Check if class inherits from ObservableModel
            if (!namedTypeSymbol.InheritsFromObservableModel())
            {
                return null;
            }

            var diagnostics = new List<Diagnostic>();
            var shouldGenerateCode = true;

            // Check if any declaration is missing 'partial' modifier (RXBG072)
            foreach (var decl in classDeclarations)
            {
                var isPartial = decl.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword));
                if (!isPartial)
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.ObservableEntityMissingPartialModifierError,
                        decl.Identifier.GetLocation(),
                        "Class",
                        namedTypeSymbol.Name,
                        "inherits from ObservableModel",
                        "class");
                    diagnostics.Add(diagnostic);
                    shouldGenerateCode = false;
                }
            }

            if (!shouldGenerateCode)
            {
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

            // Merge methods from all declarations
            var allMethods = new Dictionary<string, MethodDeclarationSyntax>();
            foreach (var decl in classDeclarations)
            {
                var declMethods = decl.CollectMethods();
                foreach (var kvp in declMethods)
                {
                    // Later declarations can override earlier ones
                    allMethods[kvp.Key] = kvp.Value;
                }
            }

            // Merge partial properties from all declarations
            var allPartialProperties = new List<PartialPropertyInfo>();
            var allPartialPropertyDiagnostics = new List<Diagnostic>();
            foreach (var decl in classDeclarations)
            {
                // Get semantic model for this specific declaration's syntax tree
                var declSemanticModel = compilation.GetSemanticModel(decl.SyntaxTree);
                var (props, propDiags) = decl.ExtractPartialPropertiesWithDiagnostics(declSemanticModel);
                allPartialProperties.AddRange(props);
                allPartialPropertyDiagnostics.AddRange(propDiags);
            }

            // Merge command properties from all declarations
            var allCommandProperties = new List<CommandPropertyInfo>();
            var allCommandPropertyDiagnostics = new List<Diagnostic>();
            foreach (var decl in classDeclarations)
            {
                var declSemanticModel = compilation.GetSemanticModel(decl.SyntaxTree);
                var (cmds, cmdDiags) = decl.ExtractCommandPropertiesWithDiagnostics(allMethods, declSemanticModel);
                allCommandProperties.AddRange(cmds);
                allCommandPropertyDiagnostics.AddRange(cmdDiags);
            }

            // Extract model references, DI fields, and model observers from partial constructor parameters
            // Only the primary declaration (with base type) should have the constructor
            var modelReferences = new List<ModelReferenceInfo>();
            var diFields = new List<DIFieldInfo>();
            var unregisteredServices = new List<(string paramName, string paramType, ITypeSymbol? typeSymbol, Location? location)>();
            var diFieldsWithScope = new List<(DIFieldInfo diField, string? serviceScope, Location? location)>();
            var modelObservers = new List<ModelObserverInfo>();
            var modelObserverDiagnostics = new List<Diagnostic>();

            foreach (var decl in classDeclarations)
            {
                var declSemanticModel = compilation.GetSemanticModel(decl.SyntaxTree);
                var (refs, fields, unreg, fieldsScope, observers, observerDiags) =
                    decl.ExtractPartialConstructorDependencies(declSemanticModel, serviceClasses, namedTypeSymbol);
                modelReferences.AddRange(refs);
                diFields.AddRange(fields);
                unregisteredServices.AddRange(unreg);
                diFieldsWithScope.AddRange(fieldsScope);
                modelObservers.AddRange(observers);
                modelObserverDiagnostics.AddRange(observerDiags);

                // Extract additional DI fields from private field declarations
                var additionalDIFields = decl.ExtractDIFields(declSemanticModel, serviceClasses);
                diFields.AddRange(additionalDIFields);
            }

            // Get model scope from the declaration that has the attribute
            var modelScope = "Scoped";
            var hasScopeAttribute = false;
            ClassDeclarationSyntax? scopeAttributeDecl = null;
            foreach (var decl in classDeclarations)
            {
                var declSemanticModel = compilation.GetSemanticModel(decl.SyntaxTree);
                var (scope, hasAttr) = decl.ExtractModelScopeWithAttributeCheck(declSemanticModel);
                if (hasAttr)
                {
                    modelScope = scope;
                    hasScopeAttribute = true;
                    scopeAttributeDecl = decl;
                    break;
                }
            }

            var implementedInterfaces = namedTypeSymbol.ExtractObservableModelInterfaces();
            var genericTypes = namedTypeSymbol.ExtractObservableModelGenericTypes();

            // Get type constraints from primary declaration
            var typeConstrains = primaryDecl.ExtractTypeConstrains();

            // Merge using statements from all declarations
            var allUsingStatements = new HashSet<string>();
            foreach (var decl in classDeclarations)
            {
                var usings = decl.ExtractUsingStatements();
                foreach (var u in usings)
                {
                    allUsingStatements.Add(u);
                }
            }

            var baseModelType = namedTypeSymbol.GetObservableModelBaseType();
            var baseModelTypeName = baseModelType?.ToDisplayString();
            var constructorAccessibility = primaryDecl.GetConstructorAccessibility();
            var classAccessibility = primaryDecl.GetClassAccessibility();

            // Add all diagnostics
            diagnostics.AddRange(allPartialPropertyDiagnostics);
            diagnostics.AddRange(allCommandPropertyDiagnostics);
            diagnostics.AddRange(modelObserverDiagnostics);

            // Check for missing ObservableModelScope attribute (RXBG070)
            if (!hasScopeAttribute)
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.MissingObservableModelScopeWarning,
                    primaryDecl.Identifier.GetLocation(),
                    namedTypeSymbol.Name);
                diagnostics.Add(diagnostic);
            }

            // Check for non-public partial constructor with parameters (RXBG071)
            var partialConstructor = primaryDecl.GetPartialConstructor();
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
                allPartialProperties,
                allCommandProperties,
                allMethods,
                modelReferences,
                modelScope,
                diFields,
                implementedInterfaces,
                genericTypes,
                typeConstrains,
                allUsingStatements.ToList(),
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
                compilation,
                modelSymbolMap);

            // Check for unused model references and derived model issues (RXBG008, RXBG017)
            var modelReferenceDiagnostics = enhancedModelReferences.CreateUnusedModelReferenceDiagnostics(
                namedTypeSymbol,
                primaryDecl,
                compilation);
            diagnostics.AddRange(modelReferenceDiagnostics);

            // Build set of methods that should NOT be auto-detected as internal observers
            // These are methods already used as command execute/canExecute or trigger methods
            var excludedMethods = BuildExcludedMethodsSet(allCommandProperties, allPartialProperties);

            // Analyze private methods from ALL declarations for internal model observers
            var allInternalModelObservers = new List<InternalModelObserverInfo>();
            var allInvalidInternalObservers = new List<InvalidInternalModelObserverInfo>();
            foreach (var decl in classDeclarations)
            {
                var declSemanticModel = compilation.GetSemanticModel(decl.SyntaxTree);
                var (observers, invalid) = decl.AnalyzeInternalModelObserversWithDiagnostics(
                    declSemanticModel,
                    enhancedModelReferences,
                    modelSymbolMap,
                    excludedMethods);
                allInternalModelObservers.AddRange(observers);
                allInvalidInternalObservers.AddRange(invalid);
            }

            // Add RXBG031 (CircularTriggerReferenceError) for internal observers to diagnostics
            // so the analyzer can report them (with code fix support in IDE)
            foreach (var invalidObserver in allInvalidInternalObservers.Where(o => o.IsCircularReference))
            {
                if (invalidObserver.Location is not null)
                {
                    var circularProps = string.Join(", ", invalidObserver.CircularProperties
                        .Select(p => $"{invalidObserver.ModelReferenceName}.{p}"));

                    var properties = ImmutableDictionary.CreateBuilder<string, string?>();
                    properties.Add("CircularProperty", circularProps);
                    properties.Add("MethodName", invalidObserver.MethodName);
                    properties.Add("IsInternalObserver", "true");

                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.CircularTriggerReferenceError,
                        invalidObserver.Location,
                        properties.ToImmutable(),
                        invalidObserver.MethodName,
                        circularProps,
                        invalidObserver.MethodName);
                    diagnostics.Add(diagnostic);
                }
            }

            // Create the final record
            var record = new ObservableModelRecord(
                new ObservableModelInfo(
                    namedTypeSymbol.ContainingNamespace.ToDisplayString(),
                    namedTypeSymbol.Name,
                    namedTypeSymbol.ToDisplayString(),
                    allPartialProperties,
                    allCommandProperties,
                    allMethods,
                    enhancedModelReferences,
                    modelScope,
                    diFields,
                    implementedInterfaces,
                    genericTypes,
                    typeConstrains,
                    allUsingStatements.ToList(),
                    baseModelTypeName,
                    constructorAccessibility,
                    classAccessibility,
                    modelObservers,
                    allInternalModelObservers),
                diagnostics,
                shouldGenerateCode);

            // Store unregistered services, DI scope info, and invalid observers for generator diagnostic reporting
            record.UnregisteredServices = unregisteredServices;
            record.DiFieldsWithScope = diFieldsWithScope;
            record.InvalidInternalObservers = allInvalidInternalObservers;

            // Extract component attribute data
            ExtractComponentAttributeData(namedTypeSymbol, record);

            return record;
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected, just return null
            return null;
        }
    }

    // Properties for generator-specific diagnostics (RXBG020, RXBG021, RXBG082)
    public List<(string paramName, string paramType, ITypeSymbol? typeSymbol, Location? location)> UnregisteredServices { get; private set; } = [];
    public List<(DIFieldInfo diField, string? serviceScope, Location? location)> DiFieldsWithScope { get; private set; } = [];
    public List<InvalidInternalModelObserverInfo> InvalidInternalObservers { get; private set; } = [];

    // Component attribute data (extracted during Create for use in later pipeline stages)
    public bool HasObservableComponentAttribute { get; private set; }
    public bool IncludeReferencedTriggers { get; private set; } = true; // default
    public string? CustomComponentName { get; private set; }
    public string GenericTypes { get; private set; } = string.Empty;
    public string TypeConstraints { get; private set; } = string.Empty;
    // Property names with component triggers (propertyName -> (hasSync, syncHookName, syncBehavior, hasAsync, asyncHookName, asyncBehavior, location))
    public Dictionary<string, (bool hasSync, string? syncHookName, int syncBehavior, bool hasAsync, string? asyncHookName, int asyncBehavior, Location location)> ComponentTriggerProperties { get; private set; } = [];

    // Component information for component generation
    // Set later in pipeline when all records are available for referenced model lookup
    public ComponentInfo? ComponentInfo { get; set; }

    /// <summary>
    /// Returns all diagnostics for this model.
    /// This is the single source of truth for diagnostic logic.
    /// Note: RXBG041, RXBG050, RXBG051, RXBG082, and RXBG014 are reported by the generator in a separate pass
    /// (after cross-model analysis completes).
    /// </summary>
    public List<Diagnostic> Verify()
    {
        return new List<Diagnostic>(_diagnostics);
    }

    /// <summary>
    /// Adds a diagnostic to this record. Used by generator for cross-model diagnostics
    /// that can only be determined after all records are analyzed (e.g., RXBG041).
    /// </summary>
    public void AddDiagnostic(Diagnostic diagnostic)
    {
        _diagnostics.Add(diagnostic);
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

        // Extract component trigger properties with custom hook names, trigger behaviors, and locations
        var componentTriggers = new Dictionary<string, (bool hasSync, string? syncHookName, int syncBehavior, bool hasAsync, string? asyncHookName, int asyncBehavior, Location location)>();
        foreach (var member in namedTypeSymbol.GetMembers())
        {
            if (member is IPropertySymbol propertySymbol)
            {
                var hasSyncTrigger = false;
                string? syncHookName = null;
                var syncTriggerBehavior = 0; // Default: RenderAndHook
                var hasAsyncTrigger = false;
                string? asyncHookName = null;
                var asyncTriggerBehavior = 0; // Default: RenderAndHook
                Location? attributeLocation = null;

                foreach (var attr in propertySymbol.GetAttributes())
                {
                    if (attr.AttributeClass?.Name == "ObservableComponentTriggerAttribute")
                    {
                        hasSyncTrigger = true;
                        // Extract ComponentTriggerType enum from first constructor argument
                        if (attr.ConstructorArguments.Length > 0 &&
                            attr.ConstructorArguments[0].Value is int enumValue)
                        {
                            syncTriggerBehavior = enumValue;
                        }
                        // Extract custom hook name from second constructor argument
                        if (attr.ConstructorArguments.Length > 1 &&
                            attr.ConstructorArguments[1].Value is string customName &&
                            !string.IsNullOrWhiteSpace(customName))
                        {
                            syncHookName = customName;
                        }
                        // Capture attribute location for diagnostic reporting
                        attributeLocation ??= attr.ApplicationSyntaxReference?.GetSyntax().GetLocation();
                    }
                    else if (attr.AttributeClass?.Name == "ObservableComponentTriggerAsyncAttribute")
                    {
                        hasAsyncTrigger = true;
                        // Extract ComponentTriggerType enum from first constructor argument
                        if (attr.ConstructorArguments.Length > 0 &&
                            attr.ConstructorArguments[0].Value is int enumValue)
                        {
                            asyncTriggerBehavior = enumValue;
                        }
                        // Extract custom hook name from second constructor argument
                        if (attr.ConstructorArguments.Length > 1 &&
                            attr.ConstructorArguments[1].Value is string customNameAsync &&
                            !string.IsNullOrWhiteSpace(customNameAsync))
                        {
                            asyncHookName = customNameAsync;
                        }
                        // Capture attribute location for diagnostic reporting
                        attributeLocation ??= attr.ApplicationSyntaxReference?.GetSyntax().GetLocation();
                    }
                }

                if (hasSyncTrigger || hasAsyncTrigger)
                {
                    // Use attribute location, fallback to property location, then None
                    var location = attributeLocation
                        ?? propertySymbol.Locations.FirstOrDefault()
                        ?? Location.None;

                    componentTriggers[propertySymbol.Name] = (hasSyncTrigger, syncHookName, syncTriggerBehavior, hasAsyncTrigger, asyncHookName, asyncTriggerBehavior, location);
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
                var (hasSync, syncHookName, syncBehavior, hasAsync, asyncHookName, asyncBehavior, _) = kvp.Value;

                // Use custom hook names if provided, otherwise ComponentTriggerInfo will use defaults
                if (hasSync)
                {
                    componentTriggers.Add(new ComponentTriggerInfo(
                        propertyName,
                        TriggerHookType.Sync,
                        syncHookName,
                        referencedModelPropertyName: null,
                        triggerBehavior: syncBehavior));
                }

                if (hasAsync)
                {
                    componentTriggers.Add(new ComponentTriggerInfo(
                        propertyName,
                        TriggerHookType.Async,
                        asyncHookName,
                        referencedModelPropertyName: null,
                        triggerBehavior: asyncBehavior));
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

                    Dictionary<string, (bool hasSync, string? syncHookName, int syncBehavior, bool hasAsync, string? asyncHookName, int asyncBehavior, Location location)> triggerProperties;

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
                        triggerProperties = new Dictionary<string, (bool hasSync, string? syncHookName, int syncBehavior, bool hasAsync, string? asyncHookName, int asyncBehavior, Location location)>();
                        foreach (var member in referencedTypeSymbol.GetMembers())
                        {
                            if (member is IPropertySymbol propertySymbol)
                            {
                                bool hasSyncTrigger = false;
                                string? syncHookName = null;
                                int syncTriggerBehavior = 0;
                                bool hasAsyncTrigger = false;
                                string? asyncHookName = null;
                                int asyncTriggerBehavior = 0;

                                foreach (var attr in propertySymbol.GetAttributes())
                                {
                                    if (attr.AttributeClass?.Name == "ObservableComponentTriggerAttribute")
                                    {
                                        hasSyncTrigger = true;
                                        // Extract ComponentTriggerType enum from first constructor argument
                                        if (attr.ConstructorArguments.Length > 0 &&
                                            attr.ConstructorArguments[0].Value is int enumValue)
                                        {
                                            syncTriggerBehavior = enumValue;
                                        }
                                        // Extract custom hook name from second constructor argument
                                        if (attr.ConstructorArguments.Length > 1 &&
                                            attr.ConstructorArguments[1].Value is string customName &&
                                            !string.IsNullOrWhiteSpace(customName))
                                        {
                                            syncHookName = customName;
                                        }
                                    }
                                    else if (attr.AttributeClass?.Name == "ObservableComponentTriggerAsyncAttribute")
                                    {
                                        hasAsyncTrigger = true;
                                        // Extract ComponentTriggerType enum from first constructor argument
                                        if (attr.ConstructorArguments.Length > 0 &&
                                            attr.ConstructorArguments[0].Value is int enumValue)
                                        {
                                            asyncTriggerBehavior = enumValue;
                                        }
                                        // Extract custom hook name from second constructor argument
                                        if (attr.ConstructorArguments.Length > 1 &&
                                            attr.ConstructorArguments[1].Value is string customNameAsync &&
                                            !string.IsNullOrWhiteSpace(customNameAsync))
                                        {
                                            asyncHookName = customNameAsync;
                                        }
                                    }
                                }

                                if (hasSyncTrigger || hasAsyncTrigger)
                                {
                                    // Cross-assembly properties don't have source location
                                    triggerProperties[propertySymbol.Name] = (hasSyncTrigger, syncHookName, syncTriggerBehavior, hasAsyncTrigger, asyncHookName, asyncTriggerBehavior, Location.None);
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
                        var (hasSync, syncHookName, syncBehavior, hasAsync, asyncHookName, asyncBehavior, _) = kvp.Value;

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
                                modelRef.PropertyName,
                                triggerBehavior: syncBehavior));
                        }

                        if (hasAsync)
                        {
                            var hookName = $"{defaultBaseName}ChangedAsync";
                            allComponentTriggers.Add(new ComponentTriggerInfo(
                                propertyName,
                                TriggerHookType.Async,
                                hookName,
                                modelRef.PropertyName,
                                triggerBehavior: asyncBehavior));
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

    /// <summary>
    /// Builds a set of method names that should be excluded from internal observer auto-detection.
    /// These are methods already used as:
    /// - Command execute methods
    /// - Command canExecute methods
    /// - Command trigger canTrigger methods
    /// - Property trigger canTrigger methods
    /// Note: Property trigger execute methods are NOT excluded - they can be both local triggers
    /// AND internal observers for referenced model properties (orthogonal concerns).
    /// </summary>
    private static HashSet<string> BuildExcludedMethodsSet(
        List<CommandPropertyInfo> commandProperties,
        List<PartialPropertyInfo> partialProperties)
    {
        var excludedMethods = new HashSet<string>();

        // Add command execute and canExecute methods
        foreach (var cmd in commandProperties)
        {
            if (!string.IsNullOrEmpty(cmd.ExecuteMethod))
            {
                excludedMethods.Add(cmd.ExecuteMethod);
            }

            if (cmd.CanExecuteMethod is { Length: > 0 } canExecuteMethod)
            {
                excludedMethods.Add(canExecuteMethod);
            }

            // Add command trigger canTrigger methods
            foreach (var trigger in cmd.Triggers)
            {
                if (trigger.CanTriggerMethod is { Length: > 0 } cmdCanTriggerMethod)
                {
                    excludedMethods.Add(cmdCanTriggerMethod);
                }
            }
        }

        // Add property trigger canTrigger methods only (NOT execute methods)
        // Execute methods can still be internal observers for REFERENCED model properties
        // since property triggers handle LOCAL changes and internal observers handle REFERENCED changes
        foreach (var prop in partialProperties)
        {
            foreach (var trigger in prop.Triggers)
            {
                // Don't exclude execute methods - they can be both triggers AND internal observers
                // A method like RecalculateTotal() can be triggered by local property changes
                // AND also observe referenced model property changes

                if (trigger.CanTriggerMethod is { Length: > 0 } propCanTriggerMethod)
                {
                    excludedMethods.Add(propCanTriggerMethod);
                }
            }
        }

        return excludedMethods;
    }

    // IEquatable implementation for incremental generator caching
    public bool Equals(ObservableModelRecord? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Compare properties that affect code generation
        return ModelInfo.Equals(other.ModelInfo) &&
               ShouldGenerateCode == other.ShouldGenerateCode &&
               HasObservableComponentAttribute == other.HasObservableComponentAttribute &&
               IncludeReferencedTriggers == other.IncludeReferencedTriggers &&
               CustomComponentName == other.CustomComponentName &&
               GenericTypes == other.GenericTypes &&
               TypeConstraints == other.TypeConstraints &&
               ComponentTriggerPropertiesEqual(other.ComponentTriggerProperties) &&
               ComponentInfoEqual(other.ComponentInfo);
    }

    private bool ComponentTriggerPropertiesEqual(
        Dictionary<string, (bool hasSync, string? syncHookName, int syncBehavior, bool hasAsync, string? asyncHookName, int asyncBehavior, Location location)> other)
    {
        if (ComponentTriggerProperties.Count != other.Count)
        {
            return false;
        }

        foreach (var kvp in ComponentTriggerProperties)
        {
            if (!other.TryGetValue(kvp.Key, out var otherValue))
            {
                return false;
            }

            // Compare trigger data (skip Location as it doesn't affect code generation)
            if (kvp.Value.hasSync != otherValue.hasSync ||
                kvp.Value.syncHookName != otherValue.syncHookName ||
                kvp.Value.hasAsync != otherValue.hasAsync ||
                kvp.Value.asyncHookName != otherValue.asyncHookName)
            {
                return false;
            }
        }

        return true;
    }

    private bool ComponentInfoEqual(ComponentInfo? other)
    {
        if (ComponentInfo is null && other is null)
        {
            return true;
        }

        if (ComponentInfo is null || other is null)
        {
            return false;
        }

        return ComponentInfo.Equals(other);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ObservableModelRecord);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ModelInfo);
        hash.Add(ShouldGenerateCode);
        hash.Add(HasObservableComponentAttribute);
        hash.Add(IncludeReferencedTriggers);
        hash.Add(CustomComponentName);
        hash.Add(GenericTypes);
        hash.Add(TypeConstraints);

        // Hash component trigger properties
        foreach (var kvp in ComponentTriggerProperties.OrderBy(k => k.Key))
        {
            hash.Add(kvp.Key);
            hash.Add(kvp.Value.hasSync);
            hash.Add(kvp.Value.syncHookName);
            hash.Add(kvp.Value.hasAsync);
            hash.Add(kvp.Value.asyncHookName);
        }

        hash.Add(ComponentInfo);

        return hash.ToHashCode();
    }
}

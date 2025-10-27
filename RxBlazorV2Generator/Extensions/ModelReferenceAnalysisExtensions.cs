using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Analysis;
using RxBlazorV2Generator.Diagnostics;
using RxBlazorV2Generator.Helpers;
using RxBlazorV2Generator.Models;
using System.Linq;

namespace RxBlazorV2Generator.Extensions;

public static class ModelReferenceAnalysisExtensions
{
    public static List<string> AnalyzeModelReferenceUsage(
        this ClassDeclarationSyntax classDecl,
        INamedTypeSymbol referencedModelSymbol)
    {
        var usedProperties = new HashSet<string>();
        var possibleProperties = referencedModelSymbol
            .GetMembers()
            .Select(n => n as IPropertySymbol)
            .Where(p => p is not null)
            .Select(p => p!.Name)
            .ToList();
        
        // Use semantic model to find property access patterns
        foreach (var node in classDecl.DescendantNodes())
        {
            if (node is IdentifierNameSyntax identifierNameSyntax)
            {
                if (possibleProperties.Contains(identifierNameSyntax.Identifier.ValueText))
                {
                    usedProperties.Add(identifierNameSyntax.Identifier.ValueText);
                }
            }
        }

        return usedProperties.ToList();
    }

    public static List<string> AnalyzeCommandMethodsForModelReferences(
        this ObservableModelInfo modelInfo,
        CommandPropertyInfo command,
        string referencedModelName,
        SemanticModel semanticModel,
        ITypeSymbol referencedModelType)
    {
        var usedProperties = new HashSet<string>();

        // Analyze execute method for model property usage
        if (command.ExecuteMethod != null && modelInfo.Methods.TryGetValue(command.ExecuteMethod, out var executeMethod))
        {
            var executeProps = executeMethod.AnalyzeMethodForModelReferences(semanticModel,
                referencedModelType);
            foreach (var prop in executeProps)
            {
                usedProperties.Add(prop);
            }
        }

        // Analyze canExecute method for model property usage
        if (command.CanExecuteMethod != null && modelInfo.Methods.TryGetValue(command.CanExecuteMethod, out var canExecuteMethod))
        {
            var canExecuteProps = canExecuteMethod.AnalyzeMethodForModelReferences(semanticModel,
                referencedModelType);
            foreach (var prop in canExecuteProps)
            {
                usedProperties.Add(prop);
            }
        }

        return usedProperties.ToList();
    }

    public static List<ModelReferenceInfo> EnhanceModelReferencesWithCommandAnalysis(
        this ObservableModelInfo modelInfo,
        SemanticModel semanticModel,
        Dictionary<string, ITypeSymbol> modelSymbols)
    {
        var enhancedModelReferences = new List<ModelReferenceInfo>();

        foreach (var modelRef in modelInfo.ModelReferences)
        {
            var allUsedProperties = new HashSet<string>(modelRef.UsedProperties);

            // Get the type symbol for this model reference
            if (modelSymbols.TryGetValue(modelRef.PropertyName, out var modelSymbol))
            {
                // Analyze command methods for additional property references
                foreach (var cmd in modelInfo.CommandProperties)
                {
                    var cmdUsedProps = modelInfo.AnalyzeCommandMethodsForModelReferences(
                        cmd,
                        modelRef.ReferencedModelTypeName,
                        semanticModel,
                        modelSymbol);
                    foreach (var prop in cmdUsedProps)
                    {
                        allUsedProperties.Add(prop);
                    }
                }
            }

            enhancedModelReferences.Add(new ModelReferenceInfo(
                modelRef.ReferencedModelTypeName,
                modelRef.ReferencedModelNamespace,
                modelRef.PropertyName,
                allUsedProperties.ToList(),
                modelRef.AttributeLocation,
                modelRef.IsDerivedModel,
                modelRef.BaseObservableModelType,
                modelRef.TypeSymbol));
        }

        return enhancedModelReferences;
    }

    /// <summary>
    /// Analyzes enhanced model references for derived models and unused references, and creates diagnostics.
    /// This is the single source of truth for DerivedModelReferenceError (RXBG017) and UnusedModelReferenceError (RXBG008) detection.
    /// A referenced model counts as USED if:
    /// - It has properties used in code, OR
    /// - Current model has [ObservableComponent(includeReferencedTriggers: true)] AND referenced model has [ObservableComponentTrigger] properties
    /// </summary>
    public static List<Diagnostic> CreateUnusedModelReferenceDiagnostics(
        this List<ModelReferenceInfo> enhancedModelReferences,
        INamedTypeSymbol classSymbol,
        ClassDeclarationSyntax classDecl,
        Compilation compilation,
        Dictionary<string, ObservableModelRecord>? allRecords = null)
    {
        var diagnostics = new List<Diagnostic>();

        // Check if current model has [ObservableComponent(includeReferencedTriggers: true)]
        var hasIncludeReferencedTriggers = false;
        foreach (var attribute in classSymbol.GetAttributes())
        {
            if (attribute.AttributeClass?.Name == "ObservableComponentAttribute")
            {
                // Check includeReferencedTriggers parameter (defaults to true)
                hasIncludeReferencedTriggers = true; // default
                if (attribute.ConstructorArguments.Length > 0 &&
                    attribute.ConstructorArguments[0].Value is bool includeRefTriggers)
                {
                    hasIncludeReferencedTriggers = includeRefTriggers;
                }
                break;
            }
        }

        foreach (var modelRef in enhancedModelReferences)
        {
            // Check for derived models first (RXBG017) - this takes precedence over unused properties
            if (modelRef.IsDerivedModel)
            {
                var location = modelRef.AttributeLocation ?? classDecl.Identifier.GetLocation();
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.DerivedModelReferenceError,
                    location,
                    modelRef.ReferencedModelTypeName,
                    modelRef.BaseObservableModelType ?? "ObservableModel");
                diagnostics.Add(diagnostic);
            }
            // Check for unused properties (RXBG008) only if not a derived model
            else if (modelRef.UsedProperties.Count == 0)
            {
                // Check if reference is used via includeReferencedTriggers
                var isUsedViaTriggers = false;

                if (hasIncludeReferencedTriggers)
                {
                    if (allRecords is not null)
                    {
                        // Look up the referenced model's record to check for triggers
                        // ReferencedModelTypeName already contains the fully qualified name
                        var refFullTypeName = modelRef.ReferencedModelTypeName;

                        if (allRecords.TryGetValue(refFullTypeName, out var referencedRecord))
                        {
                            // Check if referenced model has any component triggers
                            if (referencedRecord.ComponentTriggerProperties.Count > 0)
                            {
                                isUsedViaTriggers = true;
                            }
                        }
                    }
                    else
                    {
                        // allRecords not available yet (called during Create phase)
                        // Conservative approach: assume reference MIGHT be used via triggers
                        // Don't report diagnostic now - will be verified later if needed
                        isUsedViaTriggers = true;
                    }
                }

                // Only report unused if not used in code AND not used via triggers
                if (!isUsedViaTriggers)
                {
                    var location = modelRef.AttributeLocation ?? classDecl.Identifier.GetLocation();
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.UnusedModelReferenceError,
                        location,
                        classSymbol.Name,
                        modelRef.ReferencedModelTypeName);
                    diagnostics.Add(diagnostic);
                }
            }
        }

        return diagnostics;
    }
}
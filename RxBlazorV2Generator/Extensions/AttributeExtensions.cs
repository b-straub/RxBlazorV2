using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RxBlazorV2Generator.Extensions;

public static class AttributeExtensions
{
    /// <summary>
    /// Checks if an AttributeSyntax represents a specific attribute type using semantic analysis.
    /// This is the type-safe way to check attribute types, avoiding string-based matching.
    /// </summary>
    /// <param name="attribute">The attribute syntax to check</param>
    /// <param name="semanticModel">The semantic model for symbol resolution</param>
    /// <param name="attributeTypeName">The full type name to check (e.g., "ObservableModelReferenceAttribute")</param>
    /// <returns>True if the attribute is of the specified type</returns>
    public static bool IsAttributeOfType(
        this AttributeSyntax attribute,
        SemanticModel semanticModel,
        string attributeTypeName)
    {
        if (semanticModel is null)
        {
            return false;
        }

        var symbolInfo = semanticModel.GetSymbolInfo(attribute);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return false;
        }

        var attributeClass = methodSymbol.ContainingType;
        if (attributeClass is null)
        {
            return false;
        }

        // Check both with and without "Attribute" suffix
        var typeName = attributeClass.Name;
        return typeName == attributeTypeName ||
               typeName == attributeTypeName.Replace("Attribute", "");
    }

    /// <summary>
    /// Checks if an attribute is ObservableModelScope
    /// </summary>
    public static bool IsObservableModelScope(
        this AttributeSyntax attribute,
        SemanticModel semanticModel)
    {
        return attribute.IsAttributeOfType(semanticModel, "ObservableModelScopeAttribute");
    }

    /// <summary>
    /// Checks if an attribute is ObservableCommand or ObservableCommandAsync
    /// </summary>
    public static bool IsObservableCommand(
        this AttributeSyntax attribute,
        SemanticModel semanticModel)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(attribute);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return false;
        }

        var attributeClass = methodSymbol.ContainingType;
        if (attributeClass is null)
        {
            return false;
        }

        var typeName = attributeClass.Name;
        return typeName == "ObservableCommandAttribute" ||
               typeName == "ObservableCommand" ||
               typeName == "ObservableCommandAsyncAttribute" ||
               typeName == "ObservableCommandAsync";
    }

    /// <summary>
    /// Checks if an attribute is ObservableCommandTrigger or ObservableCommandTrigger&lt;T&gt;
    /// </summary>
    public static bool IsObservableCommandTrigger(
        this AttributeSyntax attribute,
        SemanticModel semanticModel)
    {
        return attribute.IsAttributeOfType(semanticModel, "ObservableCommandTriggerAttribute");
    }

    /// <summary>
    /// Checks if an attribute is ObservableTrigger or ObservableTrigger&lt;T&gt;
    /// </summary>
    public static bool IsObservableTrigger(
        this AttributeSyntax attribute,
        SemanticModel semanticModel)
    {
        return attribute.IsAttributeOfType(semanticModel, "ObservableTriggerAttribute");
    }

    /// <summary>
    /// Checks if an attribute is ObservableTriggerAsync or ObservableTriggerAsync&lt;T&gt;
    /// </summary>
    public static bool IsObservableTriggerAsync(
        this AttributeSyntax attribute,
        SemanticModel semanticModel)
    {
        return attribute.IsAttributeOfType(semanticModel, "ObservableTriggerAsyncAttribute");
    }

    /// <summary>
    /// Checks if an attribute is ObservableModelObserver
    /// </summary>
    public static bool IsObservableModelObserver(
        this AttributeSyntax attribute,
        SemanticModel semanticModel)
    {
        return attribute.IsAttributeOfType(semanticModel, "ObservableModelObserverAttribute");
    }

    /// <summary>
    /// Checks if an AttributeData represents ObservableModelObserver.
    /// Use this for attributes retrieved via ISymbol.GetAttributes().
    /// </summary>
    public static bool IsObservableModelObserver(this AttributeData attribute)
    {
        return attribute.IsAttributeOfType("ObservableModelObserverAttribute");
    }

    /// <summary>
    /// Checks if an AttributeData represents a specific attribute type.
    /// Use this for attributes retrieved via ISymbol.GetAttributes().
    /// </summary>
    /// <param name="attribute">The attribute data to check</param>
    /// <param name="attributeTypeName">The type name to check (with or without "Attribute" suffix)</param>
    /// <returns>True if the attribute is of the specified type</returns>
    public static bool IsAttributeOfType(
        this AttributeData attribute,
        string attributeTypeName)
    {
        if (attribute.AttributeClass is null)
        {
            return false;
        }

        var typeName = attribute.AttributeClass.Name;
        return typeName == attributeTypeName ||
               typeName == attributeTypeName.Replace("Attribute", "") ||
               typeName + "Attribute" == attributeTypeName;
    }
}

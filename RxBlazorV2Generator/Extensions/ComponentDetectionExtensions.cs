using Microsoft.CodeAnalysis;

namespace RxBlazorV2Generator.Extensions;

/// <summary>
/// Extensions for detecting ObservableComponent types across assemblies.
/// </summary>
public static class ComponentDetectionExtensions
{
    /// <summary>
    /// Finds all types in referenced assemblies that inherit from ObservableComponent.
    /// Only scans assemblies that reference RxBlazorV2.
    /// </summary>
    /// <param name="compilation">The current compilation</param>
    /// <returns>Dictionary mapping component type name to (namespace, type symbol)</returns>
    public static Dictionary<string, (string Namespace, INamedTypeSymbol TypeSymbol)> FindCrossAssemblyObservableComponents(
        this Compilation compilation)
    {
        var components = compilation
            .SourceModule
            .ReferencedAssemblySymbols
            .Where(assembly => ReferencesRxBlazorV2(assembly))
            .SelectMany(assembly => GetAllObservableComponentsInAssembly(assembly))
            .ToDictionary(
                component => component.Name,
                component => (component.ContainingNamespace.ToDisplayString(), component));

        return components;
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
    /// Recursively finds all ObservableComponent types in an assembly using yield return.
    /// </summary>
    private static IEnumerable<INamedTypeSymbol> GetAllObservableComponentsInAssembly(IAssemblySymbol assembly)
    {
        return GetObservableComponentsRecursive(assembly.GlobalNamespace);
    }

    /// <summary>
    /// Recursively traverses namespace hierarchy to find all types that inherit from ObservableComponent.
    /// Uses yield return for efficient lazy enumeration.
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
}

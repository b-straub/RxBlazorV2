using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RxBlazorV2Generator.Models;

namespace RxBlazorV2Generator.Analysis;

/// <summary>
/// Discovers singleton ObservableModels across the compilation for automatic aggregation.
/// </summary>
public static class SingletonDiscovery
{
    /// <summary>
    /// Discovers all singleton ObservableModels in the compilation (same + referenced assemblies).
    /// Uses TypeSymbol analysis to find types with [ObservableModelScope(ModelScope.Singleton)].
    /// </summary>
    /// <param name="compilation">The current compilation</param>
    /// <param name="sameAssemblyRecords">Records from the current assembly</param>
    /// <param name="excludeFullyQualifiedName">Type to exclude (e.g., the aggregator itself)</param>
    /// <returns>List of singleton model info, sorted by class name for consistent ordering</returns>
    public static List<SingletonModelInfo> DiscoverSingletons(
        Compilation compilation,
        Dictionary<string, ObservableModelRecord> sameAssemblyRecords,
        string? excludeFullyQualifiedName = null)
    {
        var singletons = new List<SingletonModelInfo>();

        // Collect singleton generic types from records (those with non-empty GenericTypes)
        var singletonGenericTypes = sameAssemblyRecords.Values
            .Where(r => r.ModelInfo.ModelScope == "Singleton" && !string.IsNullOrEmpty(r.ModelInfo.GenericTypes))
            .ToDictionary(r => r.ModelInfo.ClassName, r => r);

        // Discover generic type registrations from invocation expressions
        var genericRegistrations = DiscoverGenericRegistrations(compilation, singletonGenericTypes);

        // 1. Same assembly - use existing records
        foreach (var record in sameAssemblyRecords.Values)
        {
            if (record.ModelInfo.ModelScope != "Singleton")
            {
                continue;
            }

            if (excludeFullyQualifiedName is not null &&
                record.ModelInfo.FullyQualifiedName == excludeFullyQualifiedName)
            {
                continue;
            }

            // Skip open generic types - they'll be handled via registration discovery
            // GenericTypes is extracted from INamedTypeSymbol during record creation
            if (!string.IsNullOrEmpty(record.ModelInfo.GenericTypes))
            {
                continue;
            }

            singletons.Add(new SingletonModelInfo(
                record.ModelInfo.FullyQualifiedName,
                record.ModelInfo.ClassName,
                record.ModelInfo.Namespace,
                isFromReferencedAssembly: false));
        }

        // Add discovered generic registrations (from same assembly)
        foreach (var genericReg in genericRegistrations.Where(g => !g.IsFromReferencedAssembly))
        {
            singletons.Add(genericReg);
        }

        // 2. Referenced assemblies - use TypeSymbol analysis
        var observableModelType = compilation.GetTypeByMetadataName("RxBlazorV2.Model.ObservableModel");
        if (observableModelType is null)
        {
            return SortSingletons(singletons);
        }

        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assemblySymbol)
            {
                continue;
            }

            // Skip system assemblies
            var assemblyName = assemblySymbol.Name;
            if (assemblyName.StartsWith("System") ||
                assemblyName.StartsWith("Microsoft") ||
                assemblyName.StartsWith("netstandard") ||
                assemblyName == "RxBlazorV2" ||
                assemblyName == "R3" ||
                assemblyName == "ObservableCollections")
            {
                continue;
            }

            // Find types inheriting ObservableModel with Singleton scope
            foreach (var type in GetAllTypes(assemblySymbol.GlobalNamespace))
            {
                if (type.IsAbstract)
                {
                    continue;
                }

                // Skip open generic types - they'll be handled via registration discovery
                if (type.IsGenericType && type.TypeArguments.Any(ta => ta.TypeKind == TypeKind.TypeParameter))
                {
                    continue;
                }

                if (!InheritsFrom(type, observableModelType))
                {
                    continue;
                }

                if (!HasSingletonScope(type))
                {
                    continue;
                }

                var fullyQualifiedName = type.ToDisplayString();
                if (excludeFullyQualifiedName is not null && fullyQualifiedName == excludeFullyQualifiedName)
                {
                    continue;
                }

                singletons.Add(new SingletonModelInfo(
                    fullyQualifiedName,
                    type.Name,
                    type.ContainingNamespace.ToDisplayString(),
                    isFromReferencedAssembly: true));
            }
        }

        // Add discovered generic registrations (from referenced assemblies)
        foreach (var genericReg in genericRegistrations.Where(g => g.IsFromReferencedAssembly))
        {
            singletons.Add(genericReg);
        }

        return SortSingletons(singletons);
    }

    /// <summary>
    /// Checks if the current compilation is the main app project that should generate the aggregator.
    /// Detection: project has singleton models from referenced assemblies (indicating it's the host).
    /// </summary>
    public static bool IsMainAppProject(
        Compilation compilation,
        Dictionary<string, ObservableModelRecord> sameAssemblyRecords)
    {
        var singletons = DiscoverSingletons(compilation, sameAssemblyRecords);

        // Main app is the one that references other projects with singleton models
        return singletons.Any(s => s.IsFromReferencedAssembly);
    }

    /// <summary>
    /// Discovers generic model registrations by analyzing invocation expressions.
    /// Looks for patterns like: ObservableModels.GenericModelsBaseModel&lt;string, int&gt;(services)
    /// Uses the known singleton generic types from records to match invocations.
    /// </summary>
    private static List<SingletonModelInfo> DiscoverGenericRegistrations(
        Compilation compilation,
        Dictionary<string, ObservableModelRecord> singletonGenericTypes)
    {
        var results = new List<SingletonModelInfo>();

        if (singletonGenericTypes.Count == 0)
        {
            return results;
        }

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();

            // Find all invocation expressions
            var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocations)
            {
                // Look for pattern: ObservableModels.MethodName<T1, T2>(services)
                if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                {
                    continue;
                }

                // Check if it's accessing ObservableModels
                if (memberAccess.Expression is not IdentifierNameSyntax identifier ||
                    identifier.Identifier.Text != "ObservableModels")
                {
                    continue;
                }

                // Check if it has generic type arguments
                if (memberAccess.Name is not GenericNameSyntax genericName)
                {
                    continue;
                }

                var methodName = genericName.Identifier.Text;
                var typeArgs = genericName.TypeArgumentList.Arguments;

                if (typeArgs.Count == 0)
                {
                    continue;
                }

                // Check if this matches a known singleton generic type
                if (!singletonGenericTypes.TryGetValue(methodName, out var record))
                {
                    continue;
                }

                // Verify arity matches (count commas in GenericTypes + 1)
                var expectedArity = record.ModelInfo.GenericTypes.Count(c => c == ',') + 1;
                if (typeArgs.Count != expectedArity)
                {
                    continue;
                }

                // Extract concrete type arguments
                var typeArgSymbols = new List<ITypeSymbol>();
                foreach (var typeArg in typeArgs)
                {
                    var typeSymbol = semanticModel.GetTypeInfo(typeArg).Type;
                    if (typeSymbol is null)
                    {
                        break;
                    }
                    typeArgSymbols.Add(typeSymbol);
                }

                if (typeArgSymbols.Count != typeArgs.Count)
                {
                    continue;
                }

                // Build the closed generic type name
                var typeArgNames = string.Join(", ", typeArgSymbols.Select(t => t.ToDisplayString()));
                var className = $"{record.ModelInfo.ClassName}<{typeArgNames}>";
                var fullyQualifiedName = $"{record.ModelInfo.Namespace}.{className}";

                results.Add(new SingletonModelInfo(
                    fullyQualifiedName,
                    className,
                    record.ModelInfo.Namespace,
                    isFromReferencedAssembly: false));
            }
        }

        return results;
    }

    private static bool HasSingletonScope(INamedTypeSymbol type)
    {
        foreach (var attr in type.GetAttributes())
        {
            if (attr.AttributeClass?.Name != "ObservableModelScopeAttribute")
            {
                continue;
            }

            if (attr.ConstructorArguments.Length == 0)
            {
                continue;
            }

            // ModelScope.Singleton = 0
            if (attr.ConstructorArguments[0].Value is int scope && scope == 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool InheritsFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        var current = type.BaseType;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }

            // Also check by name for cross-assembly comparison
            if (current.ToDisplayString() == baseType.ToDisplayString())
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol namespaceSymbol)
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamedTypeSymbol typeSymbol)
            {
                yield return typeSymbol;

                // Include nested types
                foreach (var nested in GetNestedTypes(typeSymbol))
                {
                    yield return nested;
                }
            }
            else if (member is INamespaceSymbol nestedNamespace)
            {
                foreach (var type in GetAllTypes(nestedNamespace))
                {
                    yield return type;
                }
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol type)
    {
        foreach (var member in type.GetTypeMembers())
        {
            yield return member;

            foreach (var nested in GetNestedTypes(member))
            {
                yield return nested;
            }
        }
    }

    private static List<SingletonModelInfo> SortSingletons(List<SingletonModelInfo> singletons)
    {
        // Sort by class name for consistent ordering
        return singletons.OrderBy(s => s.ClassName).ToList();
    }
}

using Microsoft.CodeAnalysis;

namespace RxBlazorV2Generator.Models;

/// <summary>
/// Unified context containing all metadata about observable models and components
/// from both current and referenced assemblies. This serves as a single source of truth
/// during code generation to avoid fragmented parallel tracking systems.
/// Implements IEquatable for incremental generator caching.
/// </summary>
public class GeneratorContext : IEquatable<GeneratorContext>
{
    public Dictionary<string, ComponentMetadata> AllComponents { get; }
    public Dictionary<string, ModelMetadata> AllModels { get; }

    public GeneratorContext(
        Dictionary<string, ComponentMetadata> components,
        Dictionary<string, ModelMetadata> models)
    {
        AllComponents = components;
        AllModels = models;
    }

    public bool Equals(GeneratorContext? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return DictionariesEqual(AllComponents, other.AllComponents) &&
               DictionariesEqual(AllModels, other.AllModels);
    }

    private static bool DictionariesEqual<TValue>(
        Dictionary<string, TValue> dict1,
        Dictionary<string, TValue> dict2)
        where TValue : IEquatable<TValue>
    {
        if (dict1.Count != dict2.Count)
        {
            return false;
        }

        foreach (var kvp in dict1)
        {
            if (!dict2.TryGetValue(kvp.Key, out var otherValue))
            {
                return false;
            }

            if (!kvp.Value.Equals(otherValue))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as GeneratorContext);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        // Hash components in sorted order by key
        foreach (var kvp in AllComponents.OrderBy(c => c.Key))
        {
            hash.Add(kvp.Key);
            hash.Add(kvp.Value);
        }

        // Hash models in sorted order by key
        foreach (var kvp in AllModels.OrderBy(m => m.Key))
        {
            hash.Add(kvp.Key);
            hash.Add(kvp.Value);
        }

        return hash.ToHashCode();
    }
}

/// <summary>
/// Metadata about an observable component from either current or referenced assembly.
/// Implements IEquatable for incremental generator caching.
/// </summary>
public class ComponentMetadata : IEquatable<ComponentMetadata>
{
    public string FullyQualifiedName { get; }
    public string Namespace { get; }
    public string ClassName { get; }
    public AssemblySource Source { get; }
    public HashSet<string> FilterableProperties { get; }
    public bool HasTriggers { get; }
    public string? BaseComponentType { get; }
    public string? BaseComponentNamespace { get; }
    public ComponentInfo? ComponentInfo { get; }
    public string? ModelTypeName { get; } // For referenced components

    public ComponentMetadata(
        string fullyQualifiedName,
        string namespaceName,
        string className,
        AssemblySource source,
        HashSet<string>? filterableProperties = null,
        bool hasTriggers = false,
        string? baseComponentType = null,
        string? baseComponentNamespace = null,
        ComponentInfo? componentInfo = null,
        string? modelTypeName = null)
    {
        FullyQualifiedName = fullyQualifiedName;
        Namespace = namespaceName;
        ClassName = className;
        Source = source;
        FilterableProperties = filterableProperties ?? [];
        HasTriggers = hasTriggers;
        BaseComponentType = baseComponentType;
        BaseComponentNamespace = baseComponentNamespace;
        ComponentInfo = componentInfo;
        ModelTypeName = modelTypeName;
    }

    public bool Equals(ComponentMetadata? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return FullyQualifiedName == other.FullyQualifiedName &&
               Namespace == other.Namespace &&
               ClassName == other.ClassName &&
               Source == other.Source &&
               FilterableProperties.SetEquals(other.FilterableProperties) &&
               HasTriggers == other.HasTriggers &&
               BaseComponentType == other.BaseComponentType &&
               BaseComponentNamespace == other.BaseComponentNamespace &&
               ComponentInfoEqual(other.ComponentInfo) &&
               ModelTypeName == other.ModelTypeName;
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
        return Equals(obj as ComponentMetadata);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(FullyQualifiedName);
        hash.Add(Namespace);
        hash.Add(ClassName);
        hash.Add(Source);

        // Hash filterable properties in sorted order
        foreach (var prop in FilterableProperties.OrderBy(p => p))
        {
            hash.Add(prop);
        }

        hash.Add(HasTriggers);
        hash.Add(BaseComponentType);
        hash.Add(BaseComponentNamespace);
        hash.Add(ComponentInfo);
        hash.Add(ModelTypeName);

        return hash.ToHashCode();
    }
}

/// <summary>
/// Metadata about an observable model from either current or referenced assembly.
/// Implements IEquatable for incremental generator caching.
/// </summary>
public class ModelMetadata : IEquatable<ModelMetadata>
{
    public string FullyQualifiedName { get; }
    public string Namespace { get; }
    public string ClassName { get; }
    public AssemblySource Source { get; }
    public ObservableModelInfo? ModelInfo { get; }
    public string? BaseModelTypeName { get; }
    public HashSet<string> AllProperties { get; }
    public HashSet<string> DirectProperties { get; }
    public HashSet<string> CommandProperties { get; }
    public List<ModelReferenceInfo> ModelReferences { get; }

    public ModelMetadata(
        string fullyQualifiedName,
        string namespaceName,
        string className,
        AssemblySource source,
        ObservableModelInfo? modelInfo = null,
        string? baseModelTypeName = null,
        HashSet<string>? allProperties = null,
        HashSet<string>? directProperties = null,
        HashSet<string>? commandProperties = null,
        List<ModelReferenceInfo>? modelReferences = null)
    {
        FullyQualifiedName = fullyQualifiedName;
        Namespace = namespaceName;
        ClassName = className;
        Source = source;
        ModelInfo = modelInfo;
        BaseModelTypeName = baseModelTypeName;
        AllProperties = allProperties ?? [];
        DirectProperties = directProperties ?? [];
        CommandProperties = commandProperties ?? [];
        ModelReferences = modelReferences ?? [];
    }

    public bool Equals(ModelMetadata? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return FullyQualifiedName == other.FullyQualifiedName &&
               Namespace == other.Namespace &&
               ClassName == other.ClassName &&
               Source == other.Source &&
               ModelInfoEqual(other.ModelInfo) &&
               BaseModelTypeName == other.BaseModelTypeName &&
               AllProperties.SetEquals(other.AllProperties) &&
               DirectProperties.SetEquals(other.DirectProperties) &&
               CommandProperties.SetEquals(other.CommandProperties) &&
               ModelReferencesEqual(other.ModelReferences);
    }

    private bool ModelInfoEqual(ObservableModelInfo? other)
    {
        if (ModelInfo is null && other is null)
        {
            return true;
        }

        if (ModelInfo is null || other is null)
        {
            return false;
        }

        return ModelInfo.Equals(other);
    }

    private bool ModelReferencesEqual(List<ModelReferenceInfo> other)
    {
        if (ModelReferences.Count != other.Count)
        {
            return false;
        }

        for (int i = 0; i < ModelReferences.Count; i++)
        {
            if (!ModelReferences[i].Equals(other[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ModelMetadata);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(FullyQualifiedName);
        hash.Add(Namespace);
        hash.Add(ClassName);
        hash.Add(Source);
        hash.Add(ModelInfo);
        hash.Add(BaseModelTypeName);

        // Hash properties in sorted order
        foreach (var prop in AllProperties.OrderBy(p => p))
        {
            hash.Add(prop);
        }

        foreach (var prop in DirectProperties.OrderBy(p => p))
        {
            hash.Add(prop);
        }

        foreach (var prop in CommandProperties.OrderBy(p => p))
        {
            hash.Add(prop);
        }

        foreach (var modelRef in ModelReferences)
        {
            hash.Add(modelRef);
        }

        return hash.ToHashCode();
    }
}

/// <summary>
/// Indicates whether a component/model is from the current compilation or a referenced assembly
/// </summary>
public enum AssemblySource
{
    /// <summary>
    /// From the assembly currently being compiled
    /// </summary>
    Current,

    /// <summary>
    /// From a referenced assembly
    /// </summary>
    Referenced
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RxBlazorV2Generator.Models;

// Data classes for source generation
public class ServiceInfoList
{
    public List<ServiceInfo> Services { get; } = [];

    public void AddService(ServiceInfo service)
    {
        Services.Add(service);
    }
}

public class ServiceInfo(string namespaceName, string className, string fullyQualifiedName, string? serviceScope = null)
{
    public string Namespace { get; } = namespaceName;
    public string ClassName { get; } = className;
    public string FullyQualifiedName { get; } = fullyQualifiedName;
    public string? ServiceScope { get; } = serviceScope;
}

public class ObservableModelInfo
{
    public string Namespace { get; }
    public string ClassName { get; }
    public string FullyQualifiedName { get; }
    public List<PartialPropertyInfo> PartialProperties { get; }
    public List<CommandPropertyInfo> CommandProperties { get; }
    public Dictionary<string, MethodDeclarationSyntax> Methods { get; }
    public List<ModelReferenceInfo> ModelReferences { get; }
    public string ModelScope { get; }
    public List<DIFieldInfo> DIFields { get; }
    public List<string> ImplementedInterfaces { get; }

    public string GenericTypes { get; }

    public string TypeConstrains { get; }

    public List<string> UsingStatements { get; }

    public string? BaseModelTypeName { get; }

    public string ConstructorAccessibility { get; }

    public string ClassAccessibility { get; }

    public List<ModelObserverInfo> ModelObservers { get; }

    public List<InternalModelObserverInfo> InternalModelObservers { get; }

    public ObservableModelInfo(string namespaceName, string className, string fullyQualifiedName,
        List<PartialPropertyInfo> partialProperties, List<CommandPropertyInfo> commandProperties,
        Dictionary<string, MethodDeclarationSyntax> methods, List<ModelReferenceInfo> modelReferences,
        string modelScope = "Singleton", List<DIFieldInfo>? diFields = null, List<string>? implementedInterfaces = null,
        string? genericTypes = null, string? typeConstrains = null, List<string>? usingStatements = null,
        string? baseModelTypeName = null, string constructorAccessibility = "public", string classAccessibility = "public",
        List<ModelObserverInfo>? modelObservers = null, List<InternalModelObserverInfo>? internalModelObservers = null)
    {
        Namespace = namespaceName;
        ClassName = className;
        FullyQualifiedName = fullyQualifiedName;
        PartialProperties = partialProperties;
        CommandProperties = commandProperties;
        Methods = methods;
        ModelReferences = modelReferences;
        ModelScope = modelScope;
        DIFields = diFields ?? [];
        ImplementedInterfaces = implementedInterfaces ?? [];
        GenericTypes = genericTypes ?? string.Empty;
        TypeConstrains = typeConstrains ?? string.Empty;
        UsingStatements = usingStatements ?? [];
        BaseModelTypeName = baseModelTypeName;
        ConstructorAccessibility = constructorAccessibility;
        ClassAccessibility = classAccessibility;
        ModelObservers = modelObservers ?? [];
        InternalModelObservers = internalModelObservers ?? [];
    }
}

public class PartialPropertyInfo
{
    public string Name { get; }
    public string Type { get; }
    public bool IsObservableCollection { get; }
    public bool IsEquatable { get; }
    public string[]? BatchIds { get; }
    public bool HasRequiredModifier { get; }
    public bool HasInitAccessor { get; }
    public string Accessibility { get; }
    public List<PropertyTriggerInfo> Triggers { get; }

    public PartialPropertyInfo(string name, string type, bool isObservableCollection = false, bool isEquatable = false, string[]? batchIds = null, bool hasRequiredModifier = false, bool hasInitAccessor = false, string accessibility = "public", List<PropertyTriggerInfo>? triggers = null)
    {
        Name = name;
        Type = type;
        IsObservableCollection = isObservableCollection;
        IsEquatable = isEquatable;
        BatchIds = batchIds;
        HasRequiredModifier = hasRequiredModifier;
        HasInitAccessor = hasInitAccessor;
        Accessibility = accessibility;
        Triggers = triggers ?? [];
    }
}

public class CommandPropertyInfo
{
    public string Name { get; }
    public string Type { get; }
    public string ExecuteMethod { get; }
    public string? CanExecuteMethod { get; }
    public bool SupportsCancellation { get; }
    public List<CommandTriggerInfo> Triggers { get; }
    public string Accessibility { get; }

    public CommandPropertyInfo(string name, string type, string executeMethod, string? canExecuteMethod, bool supportsCancellation = false, List<CommandTriggerInfo>? triggers = null, string accessibility = "public")
    {
        Name = name;
        Type = type;
        ExecuteMethod = executeMethod;
        CanExecuteMethod = canExecuteMethod;
        SupportsCancellation = supportsCancellation;
        Triggers = triggers ?? [];
        Accessibility = accessibility;
    }
}

public class CommandTriggerInfo(string triggerProperty, string? canTriggerMethod = null, string? parameter = null)
{
    public string TriggerProperty { get; } = triggerProperty;
    public string? CanTriggerMethod { get; } = canTriggerMethod;
    public string? Parameter { get; } = parameter;
}

public class PropertyTriggerInfo(string executeMethod, string? canTriggerMethod = null, string? parameter = null, bool supportsCancellation = false, bool isAsync = false)
{
    public string ExecuteMethod { get; } = executeMethod;
    public string? CanTriggerMethod { get; } = canTriggerMethod;
    public string? Parameter { get; } = parameter;
    public bool SupportsCancellation { get; } = supportsCancellation;
    public bool IsAsync { get; } = isAsync;
    public string PropertyName { get; set; } = "";
}

/// <summary>
/// Information about a model reference (dependency on another ObservableModel).
/// TODO: Convert to record for better incremental generator caching
/// </summary>
public class ModelReferenceInfo : IEquatable<ModelReferenceInfo>
{
    public string ReferencedModelTypeName { get; }
    public string ReferencedModelNamespace { get; }
    public string PropertyName { get; }
    public List<string> UsedProperties { get; }
    public Location? AttributeLocation { get; }
    public bool IsDerivedModel { get; }
    public string? BaseObservableModelType { get; }
    public ITypeSymbol? TypeSymbol { get; }

    public ModelReferenceInfo(
        string referencedModelTypeName,
        string referencedModelNamespace,
        string propertyName,
        List<string> usedProperties,
        Location? attributeLocation = null,
        bool isDerivedModel = false,
        string? baseObservableModelType = null,
        ITypeSymbol? typeSymbol = null)
    {
        ReferencedModelTypeName = referencedModelTypeName;
        ReferencedModelNamespace = referencedModelNamespace;
        PropertyName = propertyName;
        UsedProperties = usedProperties;
        AttributeLocation = attributeLocation;
        IsDerivedModel = isDerivedModel;
        BaseObservableModelType = baseObservableModelType;
        TypeSymbol = typeSymbol;
    }

    public bool Equals(ModelReferenceInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Compare properties that affect code generation (skip Location and TypeSymbol)
        return ReferencedModelTypeName == other.ReferencedModelTypeName &&
               ReferencedModelNamespace == other.ReferencedModelNamespace &&
               PropertyName == other.PropertyName &&
               UsedPropertiesEqual(other.UsedProperties) &&
               IsDerivedModel == other.IsDerivedModel &&
               BaseObservableModelType == other.BaseObservableModelType;
    }

    private bool UsedPropertiesEqual(List<string> other)
    {
        if (UsedProperties.Count != other.Count)
        {
            return false;
        }

        for (int i = 0; i < UsedProperties.Count; i++)
        {
            if (UsedProperties[i] != other[i])
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ModelReferenceInfo);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ReferencedModelTypeName);
        hash.Add(ReferencedModelNamespace);
        hash.Add(PropertyName);

        foreach (var prop in UsedProperties)
        {
            hash.Add(prop);
        }

        hash.Add(IsDerivedModel);
        hash.Add(BaseObservableModelType);

        return hash.ToHashCode();
    }
}

public class DIFieldInfo : IEquatable<DIFieldInfo>
{
    public string FieldName { get; }
    public string FieldType { get; }
    public bool HasModelObservers { get; }

    public DIFieldInfo(string fieldName, string fieldType, bool hasModelObservers = false)
    {
        FieldName = fieldName;
        FieldType = fieldType;
        HasModelObservers = hasModelObservers;
    }

    public bool Equals(DIFieldInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return FieldName == other.FieldName &&
               FieldType == other.FieldType &&
               HasModelObservers == other.HasModelObservers;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as DIFieldInfo);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FieldName, FieldType, HasModelObservers);
    }
}

/// <summary>
/// Information about a model observer method in a service class.
/// Used to generate subscriptions in OnContextReadyIntern().
/// </summary>
public class ModelObserverInfo : IEquatable<ModelObserverInfo>
{
    public string ServiceFieldName { get; }
    public string MethodName { get; }
    public string PropertyName { get; }
    public bool IsAsync { get; }
    public bool HasCancellationToken { get; }
    public Location? Location { get; }

    public ModelObserverInfo(
        string serviceFieldName,
        string methodName,
        string propertyName,
        bool isAsync,
        bool hasCancellationToken,
        Location? location = null)
    {
        ServiceFieldName = serviceFieldName;
        MethodName = methodName;
        PropertyName = propertyName;
        IsAsync = isAsync;
        HasCancellationToken = hasCancellationToken;
        Location = location;
    }

    public bool Equals(ModelObserverInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return ServiceFieldName == other.ServiceFieldName &&
               MethodName == other.MethodName &&
               PropertyName == other.PropertyName &&
               IsAsync == other.IsAsync &&
               HasCancellationToken == other.HasCancellationToken;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ModelObserverInfo);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ServiceFieldName, MethodName, PropertyName, IsAsync, HasCancellationToken);
    }
}

/// <summary>
/// Information about a private method in a model that observes properties from a referenced model.
/// Auto-detected by analyzing method body for referenced model property access.
/// Used to generate subscriptions in the model constructor.
/// </summary>
public class InternalModelObserverInfo : IEquatable<InternalModelObserverInfo>
{
    /// <summary>
    /// The name of the model reference property (e.g., "Settings")
    /// </summary>
    public string ModelReferenceName { get; }

    /// <summary>
    /// The name of the private method to call when properties change
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// The properties from the referenced model that this method accesses
    /// </summary>
    public List<string> ObservedProperties { get; }

    /// <summary>
    /// Whether the method is async (returns Task/ValueTask)
    /// </summary>
    public bool IsAsync { get; }

    /// <summary>
    /// Whether the method has a CancellationToken parameter
    /// </summary>
    public bool HasCancellationToken { get; }

    public InternalModelObserverInfo(
        string modelReferenceName,
        string methodName,
        List<string> observedProperties,
        bool isAsync,
        bool hasCancellationToken)
    {
        ModelReferenceName = modelReferenceName;
        MethodName = methodName;
        ObservedProperties = observedProperties;
        IsAsync = isAsync;
        HasCancellationToken = hasCancellationToken;
    }

    public bool Equals(InternalModelObserverInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return ModelReferenceName == other.ModelReferenceName &&
               MethodName == other.MethodName &&
               ObservedPropertiesEqual(other.ObservedProperties) &&
               IsAsync == other.IsAsync &&
               HasCancellationToken == other.HasCancellationToken;
    }

    private bool ObservedPropertiesEqual(List<string> other)
    {
        if (ObservedProperties.Count != other.Count)
        {
            return false;
        }

        for (int i = 0; i < ObservedProperties.Count; i++)
        {
            if (ObservedProperties[i] != other[i])
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as InternalModelObserverInfo);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ModelReferenceName);
        hash.Add(MethodName);
        hash.Add(IsAsync);
        hash.Add(HasCancellationToken);

        foreach (var prop in ObservedProperties)
        {
            hash.Add(prop);
        }

        return hash.ToHashCode();
    }
}

/// <summary>
/// Information about a method that accesses referenced model properties but has an invalid signature
/// or has a circular reference issue.
/// Used for reporting RXBG082 (invalid signature) or RXBG031 (circular reference) diagnostics.
/// </summary>
public class InvalidInternalModelObserverInfo
{
    /// <summary>
    /// The name of the method that has an invalid signature
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// The name of the model reference property (e.g., "Settings")
    /// </summary>
    public string ModelReferenceName { get; }

    /// <summary>
    /// The properties from the referenced model that this method accesses
    /// </summary>
    public List<string> AccessedProperties { get; }

    /// <summary>
    /// Description of why the signature is invalid or the circular reference issue
    /// </summary>
    public string InvalidReason { get; }

    /// <summary>
    /// The location of the method for diagnostic reporting
    /// </summary>
    public Location? Location { get; }

    /// <summary>
    /// Whether this is a circular reference issue (true) or an invalid signature issue (false)
    /// </summary>
    public bool IsCircularReference { get; }

    /// <summary>
    /// The properties that are involved in the circular reference (if IsCircularReference is true)
    /// </summary>
    public List<string> CircularProperties { get; }

    /// <summary>
    /// The location of the first circular property modification (for code fix placement).
    /// This is where the diagnostic should be reported for circular references.
    /// </summary>
    public Location? ModificationLocation { get; }

    public InvalidInternalModelObserverInfo(
        string methodName,
        string modelReferenceName,
        List<string> accessedProperties,
        string invalidReason,
        Location? location,
        bool isCircularReference = false,
        List<string>? circularProperties = null,
        Location? modificationLocation = null)
    {
        MethodName = methodName;
        ModelReferenceName = modelReferenceName;
        AccessedProperties = accessedProperties;
        InvalidReason = invalidReason;
        Location = location;
        IsCircularReference = isCircularReference;
        CircularProperties = circularProperties ?? [];
        ModificationLocation = modificationLocation;
    }
}

public class ComponentInfo : IEquatable<ComponentInfo>
{
    public string ComponentClassName { get; }
    public string ComponentNamespace { get; }
    public string ModelTypeName { get; }
    public string ModelNamespace { get; }
    public List<ComponentTriggerInfo> ComponentTriggers { get; }
    public string GenericTypes { get; }
    public string TypeConstrains { get; }
    public List<ModelReferenceInfo> ModelReferences { get; }
    public List<DIFieldInfo> DIFields { get; }
    public bool IncludeReferencedTriggers { get; }

    public ComponentInfo(
        string componentClassName,
        string componentNamespace,
        string modelTypeName,
        string modelNamespace,
        List<ComponentTriggerInfo>? componentTriggers = null,
        string? genericTypes = null,
        string? typeConstrains = null,
        List<ModelReferenceInfo>? modelReferences = null,
        List<DIFieldInfo>? diFields = null,
        bool includeReferencedTriggers = true)
    {
        ComponentClassName = componentClassName;
        ComponentNamespace = componentNamespace;
        ModelTypeName = modelTypeName;
        ModelNamespace = modelNamespace;
        ComponentTriggers = componentTriggers ?? [];
        GenericTypes = genericTypes ?? string.Empty;
        TypeConstrains = typeConstrains ?? string.Empty;
        ModelReferences = modelReferences ?? [];
        DIFields = diFields ?? [];
        IncludeReferencedTriggers = includeReferencedTriggers;
    }

    public bool Equals(ComponentInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return ComponentClassName == other.ComponentClassName &&
               ComponentNamespace == other.ComponentNamespace &&
               ModelTypeName == other.ModelTypeName &&
               ModelNamespace == other.ModelNamespace &&
               ListEqual(ComponentTriggers, other.ComponentTriggers) &&
               GenericTypes == other.GenericTypes &&
               TypeConstrains == other.TypeConstrains &&
               ListEqual(ModelReferences, other.ModelReferences) &&
               ListEqual(DIFields, other.DIFields) &&
               IncludeReferencedTriggers == other.IncludeReferencedTriggers;
    }

    private static bool ListEqual<T>(List<T> list1, List<T> list2) where T : IEquatable<T>
    {
        if (list1.Count != list2.Count)
        {
            return false;
        }

        for (int i = 0; i < list1.Count; i++)
        {
            if (!list1[i].Equals(list2[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ComponentInfo);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ComponentClassName);
        hash.Add(ComponentNamespace);
        hash.Add(ModelTypeName);
        hash.Add(ModelNamespace);

        foreach (var trigger in ComponentTriggers)
        {
            hash.Add(trigger);
        }

        hash.Add(GenericTypes);
        hash.Add(TypeConstrains);

        foreach (var modelRef in ModelReferences)
        {
            hash.Add(modelRef);
        }

        foreach (var diField in DIFields)
        {
            hash.Add(diField);
        }

        hash.Add(IncludeReferencedTriggers);

        return hash.ToHashCode();
    }
}

public enum TriggerHookType
{
    Sync,
    Async,
    Both
}

public class ComponentTriggerInfo : IEquatable<ComponentTriggerInfo>
{
    public string PropertyName { get; }
    public TriggerHookType HookType { get; }
    public string HookMethodName { get; }
    public string? ReferencedModelPropertyName { get; }
    public string QualifiedPropertyPath { get; }
    public int TriggerBehavior { get; } // 0=RenderAndHook, 1=RenderOnly, 2=HookOnly

    public ComponentTriggerInfo(
        string propertyName,
        TriggerHookType hookType,
        string? hookMethodName = null,
        string? referencedModelPropertyName = null,
        int triggerBehavior = 0) // Default to RenderAndHook
    {
        PropertyName = propertyName;
        HookType = hookType;
        ReferencedModelPropertyName = referencedModelPropertyName;
        TriggerBehavior = triggerBehavior;

        // For referenced model triggers, construct qualified path like "Model.Settings.IsDay"
        QualifiedPropertyPath = referencedModelPropertyName is not null
            ? $"Model.{referencedModelPropertyName}.{propertyName}"
            : $"Model.{propertyName}";

        // Use custom hook name if provided, otherwise generate based on trigger type
        HookMethodName = !string.IsNullOrWhiteSpace(hookMethodName)
            ? hookMethodName!
            : hookType == TriggerHookType.Async
                ? $"On{propertyName}ChangedAsync"
                : $"On{propertyName}Changed";
    }

    public bool Equals(ComponentTriggerInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return PropertyName == other.PropertyName &&
               HookType == other.HookType &&
               HookMethodName == other.HookMethodName &&
               ReferencedModelPropertyName == other.ReferencedModelPropertyName &&
               QualifiedPropertyPath == other.QualifiedPropertyPath &&
               TriggerBehavior == other.TriggerBehavior;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ComponentTriggerInfo);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(PropertyName, HookType, HookMethodName, ReferencedModelPropertyName, QualifiedPropertyPath, TriggerBehavior);
    }
}

/// <summary>
/// Information about a singleton ObservableModel discovered for aggregation.
/// </summary>
public class SingletonModelInfo : IEquatable<SingletonModelInfo>
{
    /// <summary>
    /// Fully qualified type name (e.g., "ReactivePatternSample.Auth.Models.AuthModel")
    /// </summary>
    public string FullyQualifiedName { get; }

    /// <summary>
    /// Class name without namespace (e.g., "AuthModel")
    /// </summary>
    public string ClassName { get; }

    /// <summary>
    /// Namespace (e.g., "ReactivePatternSample.Auth.Models")
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// Property name for the aggregator (e.g., "Auth" from "AuthModel")
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Parameter name for the constructor (e.g., "auth" from "AuthModel")
    /// </summary>
    public string ParameterName { get; }

    /// <summary>
    /// Whether this model is from a referenced assembly (vs same assembly)
    /// </summary>
    public bool IsFromReferencedAssembly { get; }

    public SingletonModelInfo(
        string fullyQualifiedName,
        string className,
        string namespaceName,
        bool isFromReferencedAssembly)
    {
        FullyQualifiedName = fullyQualifiedName;
        ClassName = className;
        Namespace = namespaceName;
        IsFromReferencedAssembly = isFromReferencedAssembly;

        // For generic types like "GenericModelsBaseModel<string, int>", extract base name
        var baseName = className;
        var genericIndex = className.IndexOf('<');
        if (genericIndex > 0)
        {
            baseName = className.Substring(0, genericIndex);
        }

        // Derive property name: remove "Model" suffix if present
        PropertyName = baseName.EndsWith("Model")
            ? baseName.Substring(0, baseName.Length - 5)
            : baseName;

        // Derive parameter name: camelCase of property name
        ParameterName = char.ToLowerInvariant(PropertyName[0]) + PropertyName.Substring(1);
    }

    public bool Equals(SingletonModelInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        return FullyQualifiedName == other.FullyQualifiedName;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as SingletonModelInfo);
    }

    public override int GetHashCode()
    {
        return FullyQualifiedName.GetHashCode();
    }
}
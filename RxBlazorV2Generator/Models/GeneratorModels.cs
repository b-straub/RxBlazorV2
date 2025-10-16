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

    public ObservableModelInfo(string namespaceName, string className, string fullyQualifiedName,
        List<PartialPropertyInfo> partialProperties, List<CommandPropertyInfo> commandProperties,
        Dictionary<string, MethodDeclarationSyntax> methods, List<ModelReferenceInfo> modelReferences,
        string modelScope = "Singleton", List<DIFieldInfo>? diFields = null, List<string>? implementedInterfaces = null,
        string? genericTypes = null, string? typeConstrains = null, List<string>? usingStatements = null,
        string? baseModelTypeName = null, string constructorAccessibility = "public", string classAccessibility = "public")
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

public class PropertyTriggerInfo(string executeMethod, string? canTriggerMethod = null, string? parameter = null, bool supportsCancellation = false)
{
    public string ExecuteMethod { get; } = executeMethod;
    public string? CanTriggerMethod { get; } = canTriggerMethod;
    public string? Parameter { get; } = parameter;
    public bool SupportsCancellation { get; } = supportsCancellation;
    public string PropertyName { get; set; } = "";
}

public class ModelReferenceInfo
{
    public string ReferencedModelTypeName { get; }
    public string ReferencedModelNamespace { get; }
    public string PropertyName { get; }
    public List<string> UsedProperties { get; }
    public Location? AttributeLocation { get; }
    public bool IsDerivedModel { get; }
    public string? BaseObservableModelType { get; }

    public ModelReferenceInfo(string referencedModelTypeName, string referencedModelNamespace, string propertyName, List<string> usedProperties, Location? attributeLocation = null, bool isDerivedModel = false, string? baseObservableModelType = null)
    {
        ReferencedModelTypeName = referencedModelTypeName;
        ReferencedModelNamespace = referencedModelNamespace;
        PropertyName = propertyName;
        UsedProperties = usedProperties;
        AttributeLocation = attributeLocation;
        IsDerivedModel = isDerivedModel;
        BaseObservableModelType = baseObservableModelType;
    }
}

public class DIFieldInfo
{
    public string FieldName { get; }
    public string FieldType { get; }
    
    public DIFieldInfo(string fieldName, string fieldType)
    {
        FieldName = fieldName;
        FieldType = fieldType;
    }
}

public class ComponentInfo
{
    public string ComponentClassName { get; }
    public string ComponentNamespace { get; }
    public string ModelTypeName { get; }
    public string ModelNamespace { get; }
    public List<ComponentTriggerInfo> ComponentTriggers { get; }
    public Dictionary<string, List<string>> BatchSubscriptions { get; }
    public bool HasSubscriptions { get; }
    public string GenericTypes { get; }
    public string TypeConstrains { get; }
    public List<ModelReferenceInfo> ModelReferences { get; }
    public List<DIFieldInfo> DIFields { get; }

    public ComponentInfo(
        string componentClassName,
        string componentNamespace,
        string modelTypeName,
        string modelNamespace,
        List<ComponentTriggerInfo>? componentTriggers = null,
        Dictionary<string, List<string>>? batchSubscriptions = null,
        string? genericTypes = null,
        string? typeConstrains = null,
        List<ModelReferenceInfo>? modelReferences = null,
        List<DIFieldInfo>? diFields = null)
    {
        ComponentClassName = componentClassName;
        ComponentNamespace = componentNamespace;
        ModelTypeName = modelTypeName;
        ModelNamespace = modelNamespace;
        ComponentTriggers = componentTriggers ?? [];
        BatchSubscriptions = batchSubscriptions ?? new Dictionary<string, List<string>>();
        HasSubscriptions = ComponentTriggers.Any() || BatchSubscriptions.Any();
        GenericTypes = genericTypes ?? string.Empty;
        TypeConstrains = typeConstrains ?? string.Empty;
        ModelReferences = modelReferences ?? [];
        DIFields = diFields ?? [];
    }
}

public enum TriggerHookType
{
    Sync,
    Async,
    Both
}

public class ComponentTriggerInfo(string propertyName, TriggerHookType hookType, string? hookMethodName = null)
{
    public string PropertyName { get; } = propertyName;
    public TriggerHookType HookType { get; } = hookType;
    public string HookMethodName { get; } = hookMethodName ?? (hookType == TriggerHookType.Async ? $"On{propertyName}ChangedAsync" : $"On{propertyName}Changed");
}
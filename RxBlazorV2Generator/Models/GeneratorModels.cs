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

public class ServiceInfo(string namespaceName, string className, string fullyQualifiedName)
{
    public string Namespace { get; } = namespaceName;
    public string ClassName { get; } = className;
    public string FullyQualifiedName { get; } = fullyQualifiedName;
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

    public ObservableModelInfo(string namespaceName, string className, string fullyQualifiedName,
        List<PartialPropertyInfo> partialProperties, List<CommandPropertyInfo> commandProperties,
        Dictionary<string, MethodDeclarationSyntax> methods, List<ModelReferenceInfo> modelReferences,
        string modelScope = "Singleton", List<DIFieldInfo>? diFields = null, List<string>? implementedInterfaces = null,
        string? genericTypes = null, string? typeConstrains = null, List<string>? usingStatements = null,
        string? baseModelTypeName = null)
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

    public PartialPropertyInfo(string name, string type, bool isObservableCollection = false, bool isEquatable = false, string[]? batchIds = null, bool hasRequiredModifier = false, bool hasInitAccessor = false)
    {
        Name = name;
        Type = type;
        IsObservableCollection = isObservableCollection;
        IsEquatable = isEquatable;
        BatchIds = batchIds;
        HasRequiredModifier = hasRequiredModifier;
        HasInitAccessor = hasInitAccessor;
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

    public CommandPropertyInfo(string name, string type, string executeMethod, string? canExecuteMethod, bool supportsCancellation = false, List<CommandTriggerInfo>? triggers = null)
    {
        Name = name;
        Type = type;
        ExecuteMethod = executeMethod;
        CanExecuteMethod = canExecuteMethod;
        SupportsCancellation = supportsCancellation;
        Triggers = triggers ?? [];
    }
}

public class CommandTriggerInfo(string triggerProperty, string? canTriggerMethod = null, string? parameter = null)
{
    public string TriggerProperty { get; } = triggerProperty;
    public string? CanTriggerMethod { get; } = canTriggerMethod;
    public string? Parameter { get; } = parameter;
}

public class ModelReferenceInfo
{
    public string ReferencedModelTypeName { get; }
    public string ReferencedModelNamespace { get; }
    public string PropertyName { get; }
    public List<string> UsedProperties { get; }
    public Location? AttributeLocation { get; }

    public ModelReferenceInfo(string referencedModelTypeName, string referencedModelNamespace, string propertyName, List<string> usedProperties, Location? attributeLocation = null)
    {
        ReferencedModelTypeName = referencedModelTypeName;
        ReferencedModelNamespace = referencedModelNamespace;
        PropertyName = propertyName;
        UsedProperties = usedProperties;
        AttributeLocation = attributeLocation;
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

public class RazorCodeBehindInfo(
    string namespaceName,
    string className,
    List<string> observableModelFields,
    List<string> usedProperties,
    Dictionary<string, string>? fieldToTypeMap = null,
    Dictionary<string, List<string>>? fieldToPropertiesMap = null,
    bool hasDiagnosticIssue = false)
{
    public string Namespace => namespaceName;
    public string ClassName => className;
    
    public List<string> ObservableModelFields => observableModelFields;
    public List<string> UsedProperties => usedProperties;
    public Dictionary<string, string> FieldToTypeMap => fieldToTypeMap ?? new Dictionary<string, string>();
    public Dictionary<string, List<string>> FieldToPropertiesMap => fieldToPropertiesMap ?? new Dictionary<string, List<string>>();
    public bool HasDiagnosticIssue => hasDiagnosticIssue;
}
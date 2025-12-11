using Microsoft.CodeAnalysis;
using RxBlazorV2Generator.Diagnostics;
using RxBlazorV2Generator.Models;
using System.Collections.Immutable;

namespace RxBlazorV2Generator.Extensions;

/// <summary>
/// Extensions for analyzing service classes for ObservableModelObserver methods.
/// </summary>
public static class ServiceAnalysisExtensions
{
    /// <summary>
    /// Extracts model observer methods from a service type that target a specific model type.
    /// Also collects diagnostics for invalid method signatures.
    /// </summary>
    /// <param name="serviceType">The service type to analyze</param>
    /// <param name="serviceFieldName">The field name used to access this service in the model</param>
    /// <param name="modelType">The model type that observers should target</param>
    /// <returns>Tuple of valid model observers and diagnostics for invalid signatures</returns>
    public static (List<ModelObserverInfo> Observers, List<Diagnostic> Diagnostics) ExtractModelObserversWithDiagnostics(
        this INamedTypeSymbol serviceType,
        string serviceFieldName,
        INamedTypeSymbol modelType)
    {
        var observers = new List<ModelObserverInfo>();
        var diagnostics = new List<Diagnostic>();

        foreach (var member in serviceType.GetMembers())
        {
            if (member is not IMethodSymbol method)
            {
                continue;
            }

            foreach (var attribute in method.GetAttributes())
            {
                if (!attribute.IsObservableModelObserver())
                {
                    continue;
                }

                // Extract property name from attribute constructor argument
                var propertyName = ExtractPropertyNameFromAttribute(attribute);
                if (propertyName is null || string.IsNullOrEmpty(propertyName))
                {
                    continue;
                }

                // Validate method signature
                var (isValid, signatureError) = ValidateMethodSignature(method, modelType);
                if (!isValid)
                {
                    var properties = ImmutableDictionary.CreateBuilder<string, string?>();
                    properties.Add("ModelTypeName", modelType.Name);

                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.ObservableModelObserverInvalidSignatureError,
                        method.Locations.FirstOrDefault(),
                        properties.ToImmutable(),
                        method.Name,
                        serviceType.Name,
                        modelType.Name);
                    diagnostics.Add(diagnostic);
                    continue;
                }

                // Check if method is async (returns Task)
                var isAsync = IsAsyncMethod(method);

                // Check if method has CancellationToken parameter
                var hasCancellationToken = HasCancellationTokenParameter(method);

                observers.Add(new ModelObserverInfo(
                    serviceFieldName,
                    method.Name,
                    propertyName,
                    isAsync,
                    hasCancellationToken,
                    method.Locations.FirstOrDefault()));
            }
        }

        return (observers, diagnostics);
    }

    /// <summary>
    /// Extracts model observer methods from a service type that target a specific model type.
    /// </summary>
    /// <param name="serviceType">The service type to analyze</param>
    /// <param name="serviceFieldName">The field name used to access this service in the model</param>
    /// <param name="modelType">The model type that observers should target</param>
    /// <returns>List of model observer information for methods targeting this model</returns>
    public static List<ModelObserverInfo> ExtractModelObservers(
        this INamedTypeSymbol serviceType,
        string serviceFieldName,
        INamedTypeSymbol modelType)
    {
        var (observers, _) = ExtractModelObserversWithDiagnostics(serviceType, serviceFieldName, modelType);
        return observers;
    }

    /// <summary>
    /// Extracts the property name from the ObservableModelObserver attribute constructor argument.
    /// </summary>
    private static string? ExtractPropertyNameFromAttribute(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length == 0)
        {
            return null;
        }

        var arg = attribute.ConstructorArguments[0];
        if (arg.Value is not string propertyName)
        {
            return null;
        }

        // Handle nameof() usage - the value will be just the property name
        // e.g., nameof(SomeModel.CurrentUser) becomes "CurrentUser"
        // But it might also be the full path like "SomeModel.CurrentUser"
        var lastDotIndex = propertyName.LastIndexOf('.');
        if (lastDotIndex >= 0)
        {
            propertyName = propertyName.Substring(lastDotIndex + 1);
        }

        return propertyName;
    }

    /// <summary>
    /// Validates the method signature for ObservableModelObserver.
    /// Valid signatures:
    /// - Sync: void MethodName(ModelType model)
    /// - Async: Task MethodName(ModelType model)
    /// - Async with CT: Task MethodName(ModelType model, CancellationToken ct)
    /// </summary>
    /// <returns>Tuple of (isValid, errorMessage)</returns>
    private static (bool IsValid, string? ErrorMessage) ValidateMethodSignature(IMethodSymbol method, INamedTypeSymbol expectedModelType)
    {
        // Must have at least one parameter
        if (method.Parameters.Length == 0)
        {
            return (false, "Method must have at least one parameter (the model type)");
        }

        // First parameter must be the model type
        var firstParam = method.Parameters[0];
        if (!SymbolEqualityComparer.Default.Equals(firstParam.Type, expectedModelType))
        {
            return (false, $"First parameter must be of type {expectedModelType.Name}");
        }

        var isAsync = IsAsyncMethod(method);

        if (isAsync)
        {
            // Async methods: Task MethodName(Model) or Task MethodName(Model, CancellationToken)
            if (method.Parameters.Length > 2)
            {
                return (false, "Async methods can have at most 2 parameters: (ModelType model) or (ModelType model, CancellationToken ct)");
            }

            if (method.Parameters.Length == 2)
            {
                var secondParam = method.Parameters[1];
                if (secondParam.Type.Name != "CancellationToken" ||
                    secondParam.Type.ContainingNamespace?.ToDisplayString() != "System.Threading")
                {
                    return (false, "Second parameter of async method must be CancellationToken");
                }
            }
        }
        else
        {
            // Sync methods: void MethodName(Model)
            if (method.Parameters.Length != 1)
            {
                return (false, "Sync methods must have exactly one parameter (the model type)");
            }

            if (method.ReturnType.SpecialType != SpecialType.System_Void)
            {
                return (false, "Sync methods must return void");
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Checks if the method returns Task (is async).
    /// </summary>
    private static bool IsAsyncMethod(IMethodSymbol method)
    {
        var returnType = method.ReturnType;

        // Check for Task
        if (returnType.Name == "Task" &&
            returnType.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks")
        {
            return true;
        }

        // Check for ValueTask
        if (returnType.Name == "ValueTask" &&
            returnType.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks")
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the method has a CancellationToken parameter.
    /// </summary>
    private static bool HasCancellationTokenParameter(IMethodSymbol method)
    {
        foreach (var param in method.Parameters)
        {
            if (param.Type.Name == "CancellationToken" &&
                param.Type.ContainingNamespace?.ToDisplayString() == "System.Threading")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a service type has any methods with ObservableModelObserver attributes.
    /// </summary>
    public static bool HasModelObserverMethods(this INamedTypeSymbol serviceType)
    {
        foreach (var member in serviceType.GetMembers())
        {
            if (member is not IMethodSymbol method)
            {
                continue;
            }

            foreach (var attribute in method.GetAttributes())
            {
                if (attribute.IsObservableModelObserver())
                {
                    return true;
                }
            }
        }

        return false;
    }
}

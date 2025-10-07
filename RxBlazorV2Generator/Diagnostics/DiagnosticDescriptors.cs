using Microsoft.CodeAnalysis;

namespace RxBlazorV2Generator.Diagnostics;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor ObservableModelAnalysisError = new(
        id: "RXBG001",
        title: "Observable model analysis error",
        messageFormat: "Error analyzing observable model '{0}': {1}",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An error occurred while analyzing an observable model class.");

    public static readonly DiagnosticDescriptor RazorAnalysisError = new(
        id: "RXBG002", 
        title: "Razor component analysis error",
        messageFormat: "Error analyzing razor component '{0}': {1}",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An error occurred while analyzing a razor component.");

    public static readonly DiagnosticDescriptor CodeGenerationError = new(
        id: "RXBG003",
        title: "Code generation error", 
        messageFormat: "Error generating code for '{0}': {1}",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An error occurred while generating source code.");

    public static readonly DiagnosticDescriptor MethodAnalysisWarning = new(
        id: "RXBG004",
        title: "Method analysis warning",
        messageFormat: "Warning analyzing method '{0}' in class '{1}': {2}",
        category: "RxBlazorGenerator", 
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A warning occurred while analyzing a method for property usage.");

    public static readonly DiagnosticDescriptor RazorFileReadError = new(
        id: "RXBG005",
        title: "Razor file read error",
        messageFormat: "Error reading razor file '{0}': {1}",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An error occurred while reading a razor file.");

    public static readonly DiagnosticDescriptor CircularModelReferenceError = new(
        id: "RXBG006",
        title: "Circular model reference detected",
        messageFormat: "Circular reference detected between models '{0}' and '{1}'",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Circular references between observable models are not allowed.",
        helpLinkUri: null,
        customTags: ["Remove this circular model reference", "Remove all circular model references"]);

    public static readonly DiagnosticDescriptor InvalidModelReferenceTargetError = new(
        id: "RXBG007",
        title: "Invalid model reference target",
        messageFormat: "Referenced model '{0}' does not inherit from ObservableModel",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Model references must target classes that inherit from ObservableModel.");

    public static readonly DiagnosticDescriptor ComponentNotObservableError = new(
        id: "RXBG009",
        title: "Component contains ObservableModel fields but does not inherit from ObservableComponent",
        messageFormat: "Component '{0}' contains ObservableModel fields but does not inherit from ObservableComponent<T> or LayoutComponentBase. Components using ObservableModel should inherit from ObservableComponent<T> for reactive binding.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Blazor components that contain ObservableModel fields must inherit from ObservableComponent<T> for reactive binding, or from LayoutComponentBase which doesn't require disposing subscriptions.");

    public static readonly DiagnosticDescriptor SharedModelNotSingletonError = new(
        id: "RXBG010",
        title: "ObservableModel used by multiple components must have Singleton scope",
        messageFormat: "ObservableModel '{0}' is used by multiple ObservableComponents but has '{1}' scope. Models shared between components must use [ObservableModelScope(ModelScope.Singleton)] or no scope attribute (Singleton is default).",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When an ObservableModel is used by multiple ObservableComponent instances, it must be registered as Singleton (default when no attribute is specified) to ensure data consistency across components.",
        customTags: [WellKnownDiagnosticTags.CompilationEnd]
        );
    
    public static readonly DiagnosticDescriptor TriggerTypeArgumentsMismatchError = new(
        id: "RXBG011",
        title: "Command trigger type arguments mismatch",
        messageFormat: "Trigger type arguments [{1}] do not match command type arguments [{0}]. Ensure the trigger attribute has the same generic type parameters as the command.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ObservableCommandTrigger generic type arguments must match the command's generic type arguments for proper type safety.");
    
    public static readonly DiagnosticDescriptor CircularTriggerReferenceError = new(
        id: "RXBG012",
        title: "Circular trigger reference detected",
        messageFormat: "Command '{0}' is triggered by property '{1}' but the execution method '{2}' modifies the same property. This creates an infinite loop. Remove the trigger or modify a different property.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ObservableCommandTrigger cannot listen to a property that the command's execution method modifies, as this creates an infinite loop.");

    public static readonly DiagnosticDescriptor GenericArityMismatchError = new(
        id: "RXBG013",
        title: "Generic type arity mismatch",
        messageFormat: "Referenced generic type '{0}' has {1} type parameters but the referencing class '{2}' has {3} type parameters. Generic arity must match for type parameter substitution.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When referencing open generic types, the number of type parameters must match between the referenced and referencing types.",
        helpLinkUri: null,
        customTags: ["Adjust type parameters to match referenced type", "Remove reference"]);

    public static readonly DiagnosticDescriptor TypeConstraintMismatchError = new(
        id: "RXBG014",
        title: "Type constraint mismatch",
        messageFormat: "Type parameter '{0}' in referenced type '{1}' has constraints '{2}' but the corresponding type parameter in referencing class '{3}' has constraints '{4}'. Constraints must be compatible.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Type constraints must be compatible between referenced and referencing generic types to ensure type safety.",
        helpLinkUri: null,
        customTags: ["Adjust type parameters to match referenced type", "Remove reference"]);

    public static readonly DiagnosticDescriptor InvalidOpenGenericReferenceError = new(
        id: "RXBG015",
        title: "Invalid open generic type reference",
        messageFormat: "Cannot reference open generic type '{0}' from non-generic class '{1}'. Open generic types can only be referenced from generic classes with compatible type parameters.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Open generic types (typeof(MyType<,>)) can only be referenced from generic classes that can provide the required type parameters.",
        helpLinkUri: null,
        customTags: ["Adjust type parameters to match referenced type", "Remove reference"]);
}
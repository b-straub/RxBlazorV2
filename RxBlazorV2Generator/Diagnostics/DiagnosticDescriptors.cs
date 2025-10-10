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
        description: "An error occurred while analyzing an observable model class.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG001.md");

    public static readonly DiagnosticDescriptor RazorAnalysisError = new(
        id: "RXBG002",
        title: "Razor component analysis error",
        messageFormat: "Error analyzing razor component '{0}': {1}",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An error occurred while analyzing a razor component.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG002.md");

    public static readonly DiagnosticDescriptor CodeGenerationError = new(
        id: "RXBG003",
        title: "Code generation error",
        messageFormat: "Error generating code for '{0}': {1}",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An error occurred while generating source code.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG003.md");

    public static readonly DiagnosticDescriptor MethodAnalysisWarning = new(
        id: "RXBG004",
        title: "Method analysis warning",
        messageFormat: "Warning analyzing method '{0}' in class '{1}': {2}",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A warning occurred while analyzing a method for property usage.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG004.md");

    public static readonly DiagnosticDescriptor RazorFileReadError = new(
        id: "RXBG005",
        title: "Razor file read error",
        messageFormat: "Error reading razor file '{0}': {1}",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An error occurred while reading a razor file.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG005.md");

    public static readonly DiagnosticDescriptor CircularModelReferenceError = new(
        id: "RXBG006",
        title: "Circular model reference detected",
        messageFormat: "Circular reference detected between models '{0}' and '{1}'",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Circular references between observable models are not allowed.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG006.md",
        customTags: ["Remove this circular model reference", "Remove all circular model references"]);

    public static readonly DiagnosticDescriptor InvalidModelReferenceTargetError = new(
        id: "RXBG007",
        title: "Invalid model reference target",
        messageFormat: "Referenced model '{0}' does not inherit from ObservableModel",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Model references must target classes that inherit from ObservableModel.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG007.md");

    public static readonly DiagnosticDescriptor UnusedModelReferenceError = new(
        id: "RXBG008",
        title: "Referenced model has no used properties",
        messageFormat: "Model '{0}' references '{1}' but does not use any of its properties. Remove the [ObservableModelReference<{1}>] attribute or use at least one property from the referenced model.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ObservableModelReference attributes should only be used when the parent model actually uses properties from the referenced model.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG008.md");

    public static readonly DiagnosticDescriptor ComponentNotObservableError = new(
        id: "RXBG009",
        title: "Component contains ObservableModel fields but does not inherit from ObservableComponent",
        messageFormat: "Component '{0}' contains ObservableModel fields but does not inherit from ObservableComponent<T> or LayoutComponentBase. Components using ObservableModel should inherit from ObservableComponent<T> for reactive binding.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Blazor components that contain ObservableModel fields must inherit from ObservableComponent<T> for reactive binding, or from LayoutComponentBase which doesn't require disposing subscriptions.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG009.md");

    public static readonly DiagnosticDescriptor SharedModelNotSingletonError = new(
        id: "RXBG010",
        title: "ObservableModel used by multiple components must have Singleton scope",
        messageFormat: "ObservableModel '{0}' is used by multiple ObservableComponents but has '{1}' scope. Models shared between components must use [ObservableModelScope(ModelScope.Singleton)] or no scope attribute (Singleton is default).",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When an ObservableModel is used by multiple ObservableComponent instances, it must be registered as Singleton (default when no attribute is specified) to ensure data consistency across components.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG010.md",
        customTags: [WellKnownDiagnosticTags.CompilationEnd]
        );
    
    public static readonly DiagnosticDescriptor TriggerTypeArgumentsMismatchError = new(
        id: "RXBG011",
        title: "Command trigger type arguments mismatch",
        messageFormat: "Trigger type arguments [{1}] do not match command type arguments [{0}]. Ensure the trigger attribute has the same generic type parameters as the command.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ObservableCommandTrigger generic type arguments must match the command's generic type arguments for proper type safety.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG011.md");
    
    public static readonly DiagnosticDescriptor CircularTriggerReferenceError = new(
        id: "RXBG012",
        title: "Circular trigger reference detected",
        messageFormat: "Command '{0}' is triggered by property '{1}' but the execution method '{2}' modifies the same property. This creates an infinite loop. Remove the trigger or modify a different property.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ObservableCommandTrigger cannot listen to a property that the command's execution method modifies, as this creates an infinite loop.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG012.md");

    public static readonly DiagnosticDescriptor GenericArityMismatchError = new(
        id: "RXBG013",
        title: "Generic type arity mismatch",
        messageFormat: "Referenced generic type '{0}' has {1} type parameters but the referencing class '{2}' has {3} type parameters. Generic arity must match for type parameter substitution.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When referencing open generic types, the number of type parameters must match between the referenced and referencing types.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG013.md",
        customTags: ["Adjust type parameters to match referenced type", "Remove reference"]);

    public static readonly DiagnosticDescriptor TypeConstraintMismatchError = new(
        id: "RXBG014",
        title: "Type constraint mismatch",
        messageFormat: "Type parameter '{0}' in referenced type '{1}' has constraints '{2}' but the corresponding type parameter in referencing class '{3}' has constraints '{4}'. Constraints must be compatible.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Type constraints must be compatible between referenced and referencing generic types to ensure type safety.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG014.md",
        customTags: ["Adjust type parameters to match referenced type", "Remove reference"]);

    public static readonly DiagnosticDescriptor InvalidOpenGenericReferenceError = new(
        id: "RXBG015",
        title: "Invalid open generic type reference",
        messageFormat: "Cannot reference open generic type '{0}' from non-generic class '{1}'. Open generic types can only be referenced from generic classes with compatible type parameters.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Open generic types (typeof(MyType<,>)) can only be referenced from generic classes that can provide the required type parameters.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG015.md",
        customTags: ["Adjust type parameters to match referenced type", "Remove reference"]);

    public static readonly DiagnosticDescriptor InvalidInitPropertyError = new(
        id: "RXBG016",
        title: "Invalid init accessor on partial property",
        messageFormat: "Property '{0}' uses 'init' accessor but type '{1}' does not implement IObservableCollection. The 'init' accessor is only valid for IObservableCollection properties where reactivity comes from observing the collection. For other types, use 'set' accessor instead of 'init'.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The 'init' accessor is only allowed for partial properties that implement IObservableCollection, as these get reactivity from observing the collection rather than property changes. For non-IObservableCollection properties, use a 'set' accessor to enable reactive property notifications.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG016.md",
        customTags: ["Convert 'init' to 'set'"]);

    public static readonly DiagnosticDescriptor DerivedModelReferenceError = new(
        id: "RXBG017",
        title: "Cannot reference derived ObservableModel",
        messageFormat: "Referenced model '{0}' is a derived ObservableModel (inherits from '{1}') and is not registered in DI. Derived models cannot be used with [ObservableModelReference] because they are excluded from dependency injection. Use the base model '{1}' instead, or refactor to use composition over inheritance.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Derived ObservableModels (those that inherit from another ObservableModel) are not registered in dependency injection and cannot be used with ObservableModelReference attributes.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG017.md",
        customTags: ["Remove ObservableModelReference attribute"]);
}
using Microsoft.CodeAnalysis;

namespace RxBlazorV2Generator.Diagnostics;

public static class DiagnosticDescriptors
{
    // ============================================================================
    // RXBG001-RXBG009: Internal/Generator Analysis Errors
    // ============================================================================

    public static readonly DiagnosticDescriptor ObservableModelAnalysisError = new(
        id: "RXBG001",
        title: "Observable model analysis error",
        messageFormat: "Error analyzing observable model '{0}': {1}",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An error occurred while analyzing an observable model class.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG001.md");

    public static readonly DiagnosticDescriptor CodeGenerationError = new(
        id: "RXBG002",
        title: "Code generation error",
        messageFormat: "Error generating code for '{0}': {1}",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An error occurred while generating source code.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG002.md");

    public static readonly DiagnosticDescriptor MethodAnalysisWarning = new(
        id: "RXBG003",
        title: "Method analysis warning",
        messageFormat: "Warning analyzing method '{0}' in class '{1}': {2}",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A warning occurred while analyzing a method for property usage.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG003.md");

    /// <summary>
    /// Generic generator error that wraps analyzer diagnostics for compilation output.
    /// Reports the original diagnostic's title and ID to help user find the issue.
    /// This prevents duplicate code fixes in IDE while ensuring build fails with clear message.
    /// </summary>
    public static readonly DiagnosticDescriptor GeneratorDiagnosticError = new(
        id: "RXBG004",
        title: "Source generator encountered diagnostic errors",
        messageFormat: "Source generator failed: {0} ({1}). Check IDE/analyzer output for details.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The source generator encountered diagnostic errors during code generation. The diagnostic title and ID are included in the message. The compiler shows the location. Check the IDE analyzer output or previous build messages for detailed error information and available code fixes.");

    // ============================================================================
    // RXBG010-RXBG019: Model Structure & References
    // ============================================================================

    public static readonly DiagnosticDescriptor CircularModelReferenceError = new(
        id: "RXBG010",
        title: "Circular model reference detected",
        messageFormat: "Circular reference detected between models '{0}' and '{1}'",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Circular references between observable models are not allowed.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG010.md",
        customTags: ["Remove this circular model reference", "Remove all circular model references"]);

    public static readonly DiagnosticDescriptor InvalidModelReferenceTargetError = new(
        id: "RXBG011",
        title: "Invalid model reference target",
        messageFormat: "Referenced model '{0}' does not inherit from ObservableModel",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Model references must target classes that inherit from ObservableModel.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG011.md");

    public static readonly DiagnosticDescriptor UnusedModelReferenceError = new(
        id: "RXBG012",
        title: "Referenced model has no used properties",
        messageFormat: "Model '{0}' references '{1}' but does not use any of its properties. Remove the constructor parameter or use at least one property from the referenced model.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Constructor parameters that are ObservableModels should only be used when the parent model actually uses properties from the referenced model.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG012.md",
        customTags: ["Remove unused constructor parameter"]);

    public static readonly DiagnosticDescriptor DerivedModelReferenceError = new(
        id: "RXBG013",
        title: "Cannot reference derived ObservableModel",
        messageFormat: "Referenced model '{0}' is a derived ObservableModel (inherits from '{1}') and is not registered in DI. Derived models cannot be injected via constructor parameters because they are excluded from dependency injection. Use the base model '{1}' instead, or refactor to use composition over inheritance.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Derived ObservableModels (those that inherit from another ObservableModel) are not registered in dependency injection and cannot be injected via constructor parameters.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG013.md",
        customTags: ["Remove constructor parameter"]);

    public static readonly DiagnosticDescriptor SharedModelNotSingletonError = new(
        id: "RXBG014",
        title: "ObservableModel used by multiple components must have Singleton scope",
        messageFormat: "ObservableModel '{0}' is used by multiple ObservableComponents but has '{1}' scope. Models shared between components must use [ObservableModelScope(ModelScope.Singleton)] or no scope attribute (Singleton is default).",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When an ObservableModel is used by multiple ObservableComponent instances, it must be registered as Singleton (default when no attribute is specified) to ensure data consistency across components.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG014.md"
        );

    // ============================================================================
    // RXBG020-RXBG029: Generic Type System
    // ============================================================================

    public static readonly DiagnosticDescriptor GenericArityMismatchError = new(
        id: "RXBG020",
        title: "Generic type arity mismatch",
        messageFormat: "Referenced generic type '{0}' has {1} type parameters but the referencing class '{2}' has {3} type parameters. Generic arity must match for type parameter substitution.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When referencing open generic types, the number of type parameters must match between the referenced and referencing types.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG020.md",
        customTags: ["Adjust type parameters to match referenced type", "Remove reference"]);

    public static readonly DiagnosticDescriptor TypeConstraintMismatchError = new(
        id: "RXBG021",
        title: "Type constraint mismatch",
        messageFormat: "Type parameter '{0}' in referenced type '{1}' has constraints '{2}' but the corresponding type parameter in referencing class '{3}' has constraints '{4}'. Constraints must be compatible.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Type constraints must be compatible between referenced and referencing generic types to ensure type safety.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG021.md",
        customTags: ["Adjust type parameters to match referenced type", "Remove reference"]);

    public static readonly DiagnosticDescriptor InvalidOpenGenericReferenceError = new(
        id: "RXBG022",
        title: "Invalid open generic type reference",
        messageFormat: "Cannot reference open generic type '{0}' from non-generic class '{1}'. Open generic types can only be referenced from generic classes with compatible type parameters.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Open generic types (typeof(MyType<,>)) can only be referenced from generic classes that can provide the required type parameters.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG022.md",
        customTags: ["Adjust type parameters to match referenced type", "Remove reference"]);

    // ============================================================================
    // RXBG030-RXBG039: Command Triggers
    // ============================================================================

    public static readonly DiagnosticDescriptor TriggerTypeArgumentsMismatchError = new(
        id: "RXBG030",
        title: "Command trigger type arguments mismatch",
        messageFormat: "Trigger type arguments [{1}] do not match command type arguments [{0}]. Ensure the trigger attribute has the same generic type parameters as the command.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ObservableCommandTrigger generic type arguments must match the command's generic type arguments for proper type safety.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG030.md");

    public static readonly DiagnosticDescriptor CircularTriggerReferenceError = new(
        id: "RXBG031",
        title: "Circular trigger reference detected",
        messageFormat: "Command '{0}' is triggered by property '{1}' but the execution method '{2}' modifies the same property. This creates an infinite loop. Remove the trigger or modify a different property.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ObservableCommandTrigger cannot listen to a property that the command's execution method modifies, as this creates an infinite loop.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG031.md");

    public static readonly DiagnosticDescriptor CommandMethodReturnsValueError = new(
        id: "RXBG032",
        title: "Command execute method should not return a value",
        messageFormat: "Command '{0}' is declared as '{1}' which does not support return values, but its execute method '{2}' returns '{3}'. Change the command property type to 'IObservableCommandR<{3}>' or 'IObservableCommandRAsync<{3}>' to support return values, or change the execute method to return void/Task.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "IObservableCommand and IObservableCommandAsync do not support return values. Use IObservableCommandR or IObservableCommandRAsync if you need to return a value from the execute method.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG032.md");

    public static readonly DiagnosticDescriptor CommandMethodMissingReturnValueError = new(
        id: "RXBG033",
        title: "Command execute method must return a value",
        messageFormat: "Command '{0}' is declared as '{1}' which expects a return value of type '{2}', but its execute method '{3}' returns '{4}'. Change the execute method to return '{2}' or 'Task<{2}>', or change the command property type to 'IObservableCommand' or 'IObservableCommandAsync' if no return value is needed.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "IObservableCommandR and IObservableCommandRAsync require execute methods that return the specified type. The return type of the execute method must match the generic type parameter.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG033.md");

    // ============================================================================
    // RXBG040-RXBG049: Properties
    // ============================================================================

    public static readonly DiagnosticDescriptor InvalidInitPropertyError = new(
        id: "RXBG040",
        title: "Invalid init accessor on partial property",
        messageFormat: "Property '{0}' uses 'init' accessor but type '{1}' does not implement IObservableCollection. The 'init' accessor is only valid for IObservableCollection properties where reactivity comes from observing the collection. For other types, use 'set' accessor instead of 'init'.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The 'init' accessor is only allowed for partial properties that implement IObservableCollection, as these get reactivity from observing the collection rather than property changes. For non-IObservableCollection properties, use a 'set' accessor to enable reactive property notifications.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG040.md",
        customTags: ["Convert 'init' to 'set'"]);

    public static readonly DiagnosticDescriptor UnusedObservableComponentTriggerWarning = new(
        id: "RXBG041",
        title: "ObservableComponentTrigger attribute has no effect",
        messageFormat: "Property '{0}' in model '{1}' has [ObservableComponentTrigger] or [ObservableComponentTriggerAsync] attribute, but this model neither has [ObservableComponent] attribute nor is referenced by a model with [ObservableComponent(includeReferencedTriggers: true)]. The trigger attribute will be ignored and no hook methods will be generated. Either add [ObservableComponent] to this model, or have another model with [ObservableComponent] reference this model via partial constructor parameter.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "ObservableComponentTrigger attributes only generate hook methods when the model has an ObservableComponent (generates hooks in its own component) or is referenced by another model with ObservableComponent(includeReferencedTriggers: true) (generates hooks in the referencing component). Without either condition, the trigger attributes have no effect.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG041.md",
        customTags: ["Add [ObservableComponent] attribute", "Remove trigger attributes"]);

    // ============================================================================
    // RXBG050-RXBG059: Dependency Injection
    // ============================================================================

    public static readonly DiagnosticDescriptor UnregisteredServiceWarning = new(
        id: "RXBG050",
        title: "Partial constructor parameter type may not be registered in DI",
        messageFormat: "Parameter '{0}' of type '{1}' is not detected as registered in the dependency injection container. If this service is not registered, add a service registration in your Program.cs or Startup.cs (e.g., {2}). If the service is already registered via an interface or factory, you can ignore this warning.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Partial constructor parameters should be registered in the DI container. The generator will create the constructor implementation. This is an informational warning - if the service is registered via interfaces, factories, or other means not detectable by static analysis, you can safely ignore it.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG050.md");

    public static readonly DiagnosticDescriptor DiServiceScopeViolationError = new(
        id: "RXBG051",
        title: "DI service scope violation",
        messageFormat: "ObservableModel '{0}' with '{1}' scope is injecting service '{2}' of type '{3}' with '{4}' scope. This violates DI scoping rules and will cause runtime errors: {1} services cannot depend on {4} services as it creates captive dependencies.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Dependency injection scoping rules must be followed: Singleton services can only inject Singleton services. Scoped services can inject Singleton and Scoped services. Transient services can inject any scope. Violating these rules causes runtime DI container exceptions.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG051.md");

    // ============================================================================
    // RXBG060-RXBG069: Components
    // ============================================================================

    public static readonly DiagnosticDescriptor DirectObservableComponentInheritanceError = new(
        id: "RXBG060",
        title: "Direct inheritance from ObservableComponent is not supported",
        messageFormat: "Component '{0}' directly inherits from ObservableComponent{1}. To use reactive components: (1) Add [ObservableComponent] attribute to your model class (optionally specify component name), (2) Change this razor file to inherit from the generated component class instead.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Direct inheritance from ObservableComponent or ObservableComponent<TModel> in razor files is not supported. Use the [ObservableComponent] attribute on the model to generate a component class (defaults to {ModelName}Component), then inherit from that generated class in your razor file.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG060.md");

    public static readonly DiagnosticDescriptor SameAssemblyComponentCompositionError = new(
        id: "RXBG061",
        title: "Generated component used for composition in same assembly without @page directive",
        messageFormat: "Razor file '{0}' inherits from generated component '{1}' without @page directive and is rendered in the same assembly. Components that are both generated and rendered in the same assembly require @page due to compilation order (see Razor warning RZ10012). Solutions: (1) Add @page to make this a routable page, (2) Move the model to a separate assembly, or (3) Don't render this component in the same assembly (render it from another assembly instead).",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When a source-generated component is rendered as a child component (e.g., <MyComponent />) within the same assembly where it's defined, the Razor compiler cannot find it because source generators run after Razor compilation. Solutions: (1) Add @page directive to make it a routable page instead of a child component, (2) Move the ObservableModel to a separate assembly so the component is pre-generated, or (3) Only render the component from a different assembly where the generated code already exists.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG061.md");

    public static readonly DiagnosticDescriptor NonReactiveComponentError = new(
        id: "RXBG062",
        title: "Component has no reactive properties or triggers",
        messageFormat: "Component '{0}' does not use any properties from its model and has no ObservableComponentTrigger hooks. This component will not react to any model changes and serves no reactive purpose. Either use model properties in the razor file to enable automatic re-rendering, or add [ObservableComponentTrigger] attributes to specific properties to generate hook methods.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ObservableComponent-based components must either observe model properties (for automatic re-rendering) or define trigger hooks (for manual handling of specific property changes). A component with neither serves no reactive purpose and should either use model properties, add trigger attributes, or not inherit from ObservableComponent.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG062.md");

    // ============================================================================
    // RXBG070-RXBG079: Attributes
    // ============================================================================

    public static readonly DiagnosticDescriptor MissingObservableModelScopeWarning = new(
        id: "RXBG070",
        title: "ObservableModel is missing ObservableModelScope attribute",
        messageFormat: "ObservableModel '{0}' is missing the [ObservableModelScope] attribute. While the default scope is Scoped, it is recommended to explicitly specify the scope for clarity. Add [ObservableModelScope(ModelScope.Scoped)], [ObservableModelScope(ModelScope.Singleton)], or [ObservableModelScope(ModelScope.Transient)].",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "ObservableModel classes should explicitly declare their dependency injection scope using the [ObservableModelScope] attribute. This makes the DI lifetime explicit and prevents confusion. The default scope when the attribute is missing is Scoped.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG070.md",
        customTags: ["Add [ObservableModelScope(ModelScope.Scoped)]", "Add [ObservableModelScope(ModelScope.Singleton)]", "Add [ObservableModelScope(ModelScope.Transient)]"]);

    public static readonly DiagnosticDescriptor NonPublicPartialConstructorError = new(
        id: "RXBG071",
        title: "Partial constructor with DI parameters must be public",
        messageFormat: "Partial constructor in ObservableModel '{0}' is '{1}' but must be 'public'. Dependency injection can only resolve constructors that are public. Change the constructor accessibility to 'public'.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Partial constructors with parameters in ObservableModel classes must be public because dependency injection containers can only resolve dependencies through public constructors. Protected, private, or internal constructors cannot be used for DI.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG071.md",
        customTags: ["Change constructor to public"]);

    public static readonly DiagnosticDescriptor ObservableEntityMissingPartialModifierError = new(
        id: "RXBG072",
        title: "Observable entity must be declared as partial",
        messageFormat: "{0} '{1}' {2} but is not declared as 'partial'. The source generator cannot generate code for non-partial members. Add the 'partial' modifier to the {3} declaration.",
        category: "RxBlazorGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Observable entities such as classes inheriting from ObservableModel or properties implementing IObservableCommand must be declared as partial to allow the source generator to generate the required implementation code. This error causes subsequent compiler errors about missing implementations, which will be resolved once the partial modifier is added.",
        helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG072.md",
        customTags: ["Add 'partial' modifier"]);
}

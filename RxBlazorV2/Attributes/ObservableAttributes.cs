// ReSharper disable once CheckNamespace
namespace RxBlazorV2.Model;

/// <summary>
/// Defines the behavior of component triggers for properties with [ObservableComponentTrigger] or [ObservableComponentTriggerAsync].
/// Controls whether property changes trigger component re-rendering, hook execution, or both.
/// </summary>
public enum ComponentTriggerType
{
    /// <summary>
    /// Default behavior: Both renders the component AND executes the hook method when the property changes.
    /// The property is automatically added to the component's Filter() for re-rendering,
    /// and the generated hook method (OnPropertyChanged or OnPropertyChangedAsync) is called.
    /// Use this for most scenarios where you want both UI updates and custom logic execution.
    /// </summary>
    /// <example>
    /// <code>
    /// [ObservableComponentTrigger] // or [ObservableComponentTrigger(ComponentTriggerType.RenderAndHook)]
    /// public partial string UserName { get; set; }
    ///
    /// // Generated hook is called AND component re-renders:
    /// protected override void OnUserNameChanged()
    /// {
    ///     Console.WriteLine($"User changed to: {Model.UserName}");
    ///     // Component will also re-render after this hook executes
    /// }
    /// </code>
    /// </example>
    RenderAndHook,

    /// <summary>
    /// Render-only behavior: Triggers component re-rendering but does NOT generate or call a hook method.
    /// The property is added to the component's Filter() for automatic re-rendering,
    /// but no OnPropertyChanged hook method is generated or called.
    /// Use this when you only need UI updates without custom logic.
    /// Note: This is rarely needed as you can just omit the attribute entirely and reference the property in the razor template.
    /// </summary>
    /// <example>
    /// <code>
    /// [ObservableComponentTrigger(ComponentTriggerType.RenderOnly)]
    /// public partial int Counter { get; set; }
    ///
    /// // Component re-renders when Counter changes, but no hook method is generated
    /// // Equivalent to just using @Model.Counter in the razor template
    /// </code>
    /// </example>
    RenderOnly,

    /// <summary>
    /// Hook-only behavior: Executes the hook method but does NOT trigger component re-rendering.
    /// The property is NOT added to the component's Filter(), so StateHasChanged won't be called automatically.
    /// Use this when you need custom logic but don't want UI updates for this specific property change.
    /// You can manually call StateHasChanged() inside the hook if needed.
    /// </summary>
    /// <example>
    /// <code>
    /// [ObservableComponentTrigger(ComponentTriggerType.HookOnly)]
    /// public partial string BackgroundTask { get; set; }
    ///
    /// // Hook is called but component does NOT re-render automatically:
    /// protected override void OnBackgroundTaskChanged()
    /// {
    ///     StartBackgroundProcessing(Model.BackgroundTask);
    ///     // Component will NOT re-render unless you call StateHasChanged() here
    /// }
    /// </code>
    /// </example>
    HookOnly
}
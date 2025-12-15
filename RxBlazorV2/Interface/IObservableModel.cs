using R3;

namespace RxBlazorV2.Interface;

/// <summary>
/// Defines the contract for reactive model objects in the RxBlazorV2 framework.
/// </summary>
/// <remarks>
/// <para>
/// Observable models provide automatic property change notifications using R3 observables.
/// They are typically created by the source generator from partial classes that inherit from
/// <c>ObservableModel</c>.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
///   <item><description>Reactive property change notifications via <see cref="Observable"/></description></item>
///   <item><description>Automatic subscription cleanup via <see cref="Subscriptions"/></description></item>
///   <item><description>Lifecycle management with <see cref="ContextReady"/> and <see cref="ContextReadyAsync"/></description></item>
///   <item><description>Support for cross-model reactive subscriptions</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Models are created via partial classes with the source generator
/// [ObservableModelScope(ModelScope.Scoped)]
/// public partial class MyModel : ObservableModel
/// {
///     public partial string Name { get; set; }
/// }
///
/// // Subscribe to property changes
/// model.Observable.Subscribe(props => Console.WriteLine($"Changed: {string.Join(", ", props)}"));
/// </code>
/// </example>
public interface IObservableModel : IDisposable
{
    /// <summary>
    /// Gets the unique identifier for this model instance.
    /// Used for distinguishing between different model types in the reactive system.
    /// </summary>
    string ModelID { get; }
    
    /// <summary>
    /// Gets the observable stream that emits property change notifications.
    /// Emits arrays of property names that have changed.
    /// </summary>
    Observable<string[]> Observable { get; }
    
    /// <summary>
    /// Initializes the model when the context is ready.
    /// Called once during the model's lifetime for synchronous initialization.
    /// </summary>
    void ContextReady();
    
    /// <summary>
    /// Initializes the model when the context is ready (async version).
    /// Called once during the model's lifetime for asynchronous initialization.
    /// </summary>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    Task ContextReadyAsync();
    
    /// <summary>
    /// Gets the collection of subscriptions managed by this model.
    /// All reactive subscriptions should be added to this collection for automatic cleanup on disposal.
    /// </summary>
    CompositeDisposable Subscriptions { get; }

    /// <summary>
    /// Gets a value indicating whether the model has been initialized.
    /// Returns <see langword="true"/> after <see cref="ContextReady"/> or <see cref="ContextReadyAsync"/> has been called.
    /// </summary>
    bool Initialized { get; }
}
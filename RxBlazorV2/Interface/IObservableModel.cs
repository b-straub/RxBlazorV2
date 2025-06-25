using R3;

namespace RxBlazorV2.Interface;

/// <summary>
/// Interface for ObservableModel functionality.
/// Provides the contract for reactive model objects with property change notifications.
/// </summary>
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
}
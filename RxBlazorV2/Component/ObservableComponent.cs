using Microsoft.AspNetCore.Components;
using R3;
using RxBlazorV2.Model;

namespace RxBlazorV2.Component;

/// <summary>
/// Abstract base Blazor component that binds to a reactive model and manages its lifecycle.
/// </summary>
/// <typeparam name="TModel">The observable model type this component is bound to.</typeparam>
public abstract class ObservableComponent<TModel> : OwningComponentBase<TModel> where TModel : ObservableModel
{
    /// <summary>
    /// Gets the composite disposable that manages all reactive subscriptions for this component.
    /// </summary>
    protected CompositeDisposable Subscriptions { get; } = new();

    /// <summary>
    /// Gets the reactive model instance resolved from dependency injection.
    /// </summary>
    public TModel Model => Service;

    /// <summary>
    /// Gets or sets the content to be rendered inside the component.
    /// </summary>
    [Parameter]
    public RenderFragment? Body { get; set; }

    /// <summary>
    /// Initializes generated code and model context on first render.
    /// </summary>
    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (firstRender)
        {
            InitializeGeneratedCode();
            OnContextReady();
            Model.ContextReady();
        }
    }
    
    /// <summary>
    /// Performs async initialization of generated code and model context on first render.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            await InitializeGeneratedCodeAsync();
            await OnContextReadyAsync();
            await Model.ContextReadyAsync();
        }
    }
    
    /// <summary>
    /// Called on first render to set up generated subscriptions and triggers; override in generated code only.
    /// </summary>
    protected virtual void InitializeGeneratedCode()
    {
    }

    /// <summary>
    /// Called on first render for async generated setup; override in generated code only.
    /// </summary>
    protected virtual Task InitializeGeneratedCodeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns property names this component subscribes to for selective re-rendering.
    /// </summary>
    protected virtual string[] Filter()
    {
        return [];
    }

    /// <summary>
    /// Called after first render; override to perform custom synchronous initialization.
    /// </summary>
    protected virtual void OnContextReady()
    {
    }

    /// <summary>
    /// Called after first render; override to perform custom asynchronous initialization.
    /// </summary>
    protected virtual Task OnContextReadyAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called during disposal; override to perform custom synchronous cleanup.
    /// </summary>
    protected virtual void OnDispose()
    {
    }

    /// <summary>
    /// Disposes component subscriptions and invokes custom cleanup via OnDispose.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            OnDispose();
            Subscriptions.Dispose();
        }
        base.Dispose(disposing);
    }
    
    /// <summary>
    /// Called during async disposal; override to perform custom asynchronous cleanup.
    /// </summary>
    protected virtual ValueTask OnDisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Performs async disposal by invoking OnDisposeAsync and the base implementation.
    /// </summary>
    protected override async ValueTask DisposeAsyncCore()
    {
        await OnDisposeAsync();
        await base.DisposeAsyncCore();
    }
}
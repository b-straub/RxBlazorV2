using Microsoft.AspNetCore.Components;
using R3;
using RxBlazorV2.Model;

namespace RxBlazorV2.Component;

public abstract class ObservableComponent<TModel> : OwningComponentBase<TModel> where TModel : ObservableModel
{
    protected CompositeDisposable Subscriptions { get; } = new();

    public TModel Model => Service;
    
    /// <summary>
    /// Gets the content to be rendered inside the component
    /// </summary>
    [Parameter]
    public RenderFragment? Body { get; set; }

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
    
    protected virtual void InitializeGeneratedCode()
    {
    }

    protected virtual Task InitializeGeneratedCodeAsync()
    {
        return Task.CompletedTask;
    }
    
    protected virtual string[] Filter()
    {
        return [];    
    }
    
    protected virtual void OnContextReady()
    {
    }
    
    protected virtual Task OnContextReadyAsync()
    {
        return Task.CompletedTask;
    }
    
    protected virtual void OnDispose()
    {
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            OnDispose();
            Subscriptions.Dispose();
        }
        base.Dispose(disposing);
    }
    
    protected virtual ValueTask OnDisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await OnDisposeAsync();
        await base.DisposeAsyncCore();
    }
}
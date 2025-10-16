using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using R3;
using RxBlazorV2.Model;

namespace RxBlazorV2.Component;

public abstract class ObservableComponent : OwningComponentBase, IAsyncDisposable
{
    protected CompositeDisposable Subscriptions { get; } = new();
    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (firstRender)
        {
            InitializeGeneratedCode();
            OnContextReady();
        }
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            await InitializeGeneratedCodeAsync();
            await OnContextReadyAsync();
        }
    }

    protected virtual void InitializeGeneratedCode()
    {
    }

    protected virtual Task InitializeGeneratedCodeAsync()
    {
        return Task.CompletedTask;
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
    
    protected virtual ValueTask OnDisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await OnDisposeAsync();
        Subscriptions.Dispose();
    }
}

public abstract class ObservableComponent<T> : OwningComponentBase<T>, IAsyncDisposable where T : ObservableModel
{
    protected CompositeDisposable Subscriptions { get; } = new();

    public T Model => Service;
    
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
    
    protected virtual ValueTask OnDisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await OnDisposeAsync();
        Subscriptions.Dispose();
    }
}
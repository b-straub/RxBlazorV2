using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using RxBlazorV2.Model;

namespace RxBlazorV2.Component;

public class ObservableComponent : OwningComponentBase, IAsyncDisposable
{
    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (firstRender)
        {
            OnInitialize();
            OnContextReady();
        }
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            await OnContextReadyAsync();
        }
    }
    
    protected virtual void OnInitialize()
    {
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
    }
}

public class ObservableComponent<T> : OwningComponentBase<T>, IAsyncDisposable where T : ObservableModel
{
    public T Model => Service;
    
    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (firstRender)
        {
            OnInitialize();
            OnContextReady();
            Model.ContextReady();
        }
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            await OnContextReadyAsync();
            await Model.ContextReadyAsync();
        }
    }
    
    protected virtual void OnInitialize()
    {
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
    }
}

public class ObservableLayoutComponentBase : ObservableComponent
{
    internal const string BodyPropertyName = nameof(Body);

    /// <summary>
    /// Gets the content to be rendered inside the layout.
    /// </summary>
    [Parameter]
    public RenderFragment? Body { get; set; }

    /// <inheritdoc />
    // Derived instances of LayoutComponentBase do not appear in any statically analyzable
    // calls of OpenComponent<T> where T is well-known. Consequently we have to explicitly provide a hint to the trimmer to preserve
    // properties.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ObservableLayoutComponentBase))]
    public override Task SetParametersAsync(ParameterView parameters) => base.SetParametersAsync(parameters);
}
using Microsoft.AspNetCore.Components;
using RxBlazorV2.Model;

namespace RxBlazorV2.Component;

public class ObservableComponent<T> : OwningComponentBase<T> where T : ObservableModel
{
    protected T Model => Service;
    
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
}
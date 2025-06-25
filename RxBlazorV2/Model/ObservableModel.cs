using R3;
using RxBlazorV2.Interface;

namespace RxBlazorV2.Model;

public abstract class ObservableModel : IObservableModel
{
    public abstract string ModelID { get; }
    
    private bool _initialized;
    private bool _initializedAsync;
    
    public Observable<string[]> Observable { get; }
    protected abstract IDisposable Subscriptions { get; }

    public void ContextReady()
    {
        if (!_initialized)
        {
            OnContextReady();
            _initialized = true;
        }
    }
    
    public async Task ContextReadyAsync()
    {
        if (!_initializedAsync)
        {
            await OnContextReadyAsync();
            _initializedAsync = true;
        }
    }

    protected virtual void OnContextReady()
    {
    }
    
    protected virtual Task OnContextReadyAsync()
    {
        return Task.CompletedTask;
    }
    
    protected void StateHasChanged(string? propertyName = null)
    {
        PropertyChangedSubject.OnNext([propertyName ?? ModelID]);
    }
    
    protected void StateHasChanged(string[] propertyNames)
    {
        PropertyChangedSubject.OnNext(propertyNames.Length == 0 ? [ModelID] : propertyNames);
    }
    
    protected internal Subject<string[]> PropertyChangedSubject { get; } = new();

    protected ObservableModel()
    {
        Observable = PropertyChangedSubject.Publish().RefCount();
    }
    
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        Subscriptions.Dispose();
    }
}
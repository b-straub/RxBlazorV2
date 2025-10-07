using RxBlazorV2.Model;

namespace RxBlazorV2.CoreTests.TestFixtures;

[ObservableModelScope(ModelScope.Transient)]
public partial class TestObservableModel : ObservableModel
{
    private bool _contextReadyCalled;
    private bool _contextReadyAsyncCalled;

    public partial int Counter { get; set; }
    public partial string Name { get; set; } = string.Empty;

    [ObservableBatch("batch1")]
    public partial int BatchProperty1 { get; set; }

    [ObservableBatch("batch1")]
    [ObservableBatch("batch2")]
    public partial int BatchProperty2 { get; set; }

    public bool ContextReadyCalled => _contextReadyCalled;
    public bool ContextReadyAsyncCalled => _contextReadyAsyncCalled;

    protected override void OnContextReady()
    {
        _contextReadyCalled = true;
    }

    protected override Task OnContextReadyAsync()
    {
        _contextReadyAsyncCalled = true;
        return Task.CompletedTask;
    }

    public void TriggerStateChanged(string propertyName)
    {
        StateHasChanged(propertyName);
    }

    public void TriggerStateChanged(string[] propertyNames)
    {
        StateHasChanged(propertyNames);
    }
}

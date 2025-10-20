using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorVSSampleComponents.ErrorManager;

namespace RxBlazorV2Sample.Models;

[ObservableComponent(false)]
[ObservableModelScope(ModelScope.Singleton)]
public partial class CounterModel : ObservableModel
{
    [ObservableTrigger("HighError")]
    public partial int Counter1 { get; set; }
    public partial int Counter2 { get; set; }

    [ObservableBatch("counters")]
    [ObservableBatch("batch2")]
    public partial int Counter3 { get; set; }
    
    [ObservableCommand(nameof(IncrementCount1))]
    public partial IObservableCommand Increment1 { get; }
    
    [ObservableCommand(nameof(IncrementCount1Async))]
    public partial IObservableCommandAsync IncrementAsync1 { get; }
    
    [ObservableCommand(nameof(AddCount2), nameof(CanAddCount2))]
    public partial IObservableCommand<int> Add2 { get; }
    
    [ObservableCommand(nameof(AddCount2Async), nameof(CanAddCount2))]
    public partial IObservableCommandAsync<int> AddAsync2 { get; }
    
    [ObservableCommand(nameof(AddCountAsyncCombinedAsync), nameof(CanAddCountCombined))]
    public partial IObservableCommandAsync<int> AddCombined { get; }

    protected override async Task OnContextReadyAsync()
    {
        await Task.Delay(2000);
        Counter2 = 10;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // do something
        }
        
        base.Dispose(disposing);
    }
    
    private void IncrementCount1()
    {
        Counter1++;
    }
    
    private async Task IncrementCount1Async()
    {
        await Task.Delay(200);
        Counter1++;
    }
    
    private void AddCount2(int value)
    {
        Counter2 += value;
    }
    
    private async Task AddCount2Async(int value)
    {
        await Task.Delay(1000);
        Counter2 += value;
    }
    
    private bool CanAddCount2()
    {
        return Counter1 > 2;
    }
    
    private async Task AddCountAsyncCombinedAsync(int value, CancellationToken token)
    {
        await Task.Delay(1000, token);
        Counter2 += value;
        Counter3 += value;
    }
    
    private bool CanAddCountCombined()
    {
        return Counter2 > 2;
    }

    private void HighError()
    {
        if (Counter1 > 5)
        {
            ErrorModel.Message = "Counter 1 is to high";
        }
    }

    public partial CounterModel(ErrorModel errorModel);
}
using RxBlazorV2.Model;

namespace RxBlazorV2Sample.Model;

public partial class CounterModel
{
    public partial int Counter1
    {
        get;
        set
        {
            field = value;
            PropertyChangedSubject.OnNext([nameof(Counter1)]);
        }
    }

    public partial int Counter2
    {
        get;
        set
        {
            field = value;
            PropertyChangedSubject.OnNext([nameof(Counter2)]);
        }
    }

    public partial int Counter3
    {
        get;
        set
        {
            field = value;
            PropertyChangedSubject.OnNext([nameof(Counter3)]);
        }
    }

    public CounterModel()
    {
        // This line causes the error! IncrementCount is void, but ObservableCommandAsyncFactory expects Func<Task>
        Increment = new ObservableCommandAsyncFactory(this, ["Counter1"], IncrementCount);
        
        // This should work - IncrementCountAsync is async Task, matching Func<Task>
        IncrementAsync = new ObservableCommandAsyncFactory(this, ["Counter1"], IncrementCountAsync);
        
        // This should work - AddCount is void AddCount(int), matching Action<int>
        Add = new ObservableCommandFactory<int>(this, ["Counter1"], AddCount, CanAddCount);
        
        // This should work - AddCountAsync has correct signature
        AddAsync = new ObservableCommandAsyncCancelableFactory<int>(this, ["Counter1"], AddCountAsync, CanAddCount);
        
        // This should work - AddCountAsyncCombined has correct signature  
        AddCombined = new ObservableCommandAsyncCancelableFactory<int>(this, ["Counter2"], AddCountAsyncCombined, CanAddCountCombined);
    }
}
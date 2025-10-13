using RxBlazorV2Sample.Interfaces;

namespace RxBlazorV2Sample.Models;

public partial class PartialCtorTest
{
    partial PartialCtorTest(CounterModel counter, ISettingsModel settings);
}

public partial class PartialCtorTest
{
    protected CounterModel Counter { get; }
    protected ISettingsModel Settings { get; }
    
    partial PartialCtorTest(CounterModel counter, ISettingsModel settings)
    {
        Counter = counter;
        Settings = settings;
    }
}
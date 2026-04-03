# Common Architecture Scenarios

## Scenario 1: Cross-Model Workflow (Save → Sync)

**Problem**: ModelB needs to do something when ModelA's save command succeeds.

**Wrong approach** (property as event signal):
```csharp
// ModelA
private async Task SaveAsync()
{
    await DataService.SaveAsync(Item);
    SuccessMessage = "Saved!";  // ModelB watches this
}

// ModelB — internal observer (auto-detected)
private async Task OnSaveCompleted()
{
    if (ModelA.SuccessMessage is not null)  // Fires multiple times, fragile
    {
        await SyncService.PushAsync();
    }
}
```

**Correct approach** (atomic command with service call):
```csharp
// ModelA — inject the sync service, call directly in command
public partial ModelA(IDataService dataService, ISyncService syncService);

private async Task SaveAsync()
{
    var result = await DataService.SaveAsync(Item);
    if (result.Success)
    {
        await SyncService.PushAsync();  // Direct, atomic, predictable
        Status = "Saved";
    }
}
```

**When internal observer IS correct** (pure UI state reaction):
```csharp
// SettingsModel changes theme
public partial bool IsDay { get; set; }

// DashboardModel reacts to update its display format — this is UI state
private void OnThemeChanged()
{
    ChartColorScheme = Settings.IsDay ? LightScheme : DarkScheme;
}
```

---

## Scenario 2: Service Extraction

**Symptoms**: Model over 300 lines, domain logic mixed with UI state, methods reused across models.

**Before** (domain logic in model):
```csharp
public partial class OrderModel : ObservableModel
{
    public partial OrderModel(IHttpClient http, ICrypto crypto);

    private async Task SubmitAsync()
    {
        // 50 lines of API orchestration, signing, retry logic...
        var signed = await Crypto.SignAsync(payload);
        var response = await Http.PostAsync("/orders", signed);
        // ... error handling, parsing ...
    }
}
```

**After** (extracted to service):
```csharp
// Service handles domain logic
public interface IOrderService
{
    Task<OrderResult> SubmitAsync(Order order);
}

// Model handles UI state only
public partial class OrderModel : ObservableModel
{
    public partial OrderModel(IOrderService orderService);

    public partial bool Submitting { get; set; }
    public partial string? ErrorMessage { get; set; }

    private async Task SubmitAsync()
    {
        Submitting = true;
        var result = await OrderService.SubmitAsync(CurrentOrder);
        if (!result.Success)
        {
            ErrorMessage = result.Error;
        }
        Submitting = false;
    }
}
```

---

## Scenario 3: Multi-Project Service Boundaries

**Problem**: ProjectA.Model needs functionality implemented in ProjectB, but ProjectA cannot reference ProjectB.

**Solution**: Interface in consuming project, implementation in providing project.

```
ProjectA (UI)
├── Models/
│   └── UserModel.cs          — uses IDataSync
├── Services/
│   └── IDataSync.cs          — interface defined here
│
ProjectB (Data)
├── Services/
│   └── DataSyncService.cs    — implements IDataSync
│   └── Extensions.cs         — registers: services.AddSingleton<IDataSync, DataSyncService>()
```

```csharp
// ProjectA — interface
public interface IDataSync
{
    Task SyncAsync();
}

// ProjectA — model uses interface
public partial class UserModel : ObservableModel
{
    public partial UserModel(IDataSync dataSync);
}

// ProjectB — implementation
public class DataSyncService(IApiClient api) : IDataSync
{
    public async Task SyncAsync() => await api.PushAsync();
}
```

---

## Scenario 4: Collection Observation

**Problem**: React to changes in an `ObservableList<T>`.

```csharp
// Collection property — changes to the collection trigger StateHasChanged
[ObservableTrigger(nameof(UpdateCounts))]
public partial ObservableList<TodoItem> Items { get; init; } = [];

// Trigger fires when Items collection changes
private void UpdateCounts()
{
    TotalCount = Items.Count;
    CompletedCount = Items.Count(i => i.IsCompleted);
}
```

---

## Scenario 5: Command with CanExecute Guard

```csharp
[ObservableCommand(nameof(SubmitAsync), nameof(CanSubmit))]
public partial IObservableCommandAsync SubmitCommand { get; }

// CanExecute is re-evaluated when any observed property changes
private bool CanSubmit() =>
    !string.IsNullOrWhiteSpace(Name) && Items.Count > 0;

private async Task SubmitAsync()
{
    // Only runs when CanSubmit() returns true
    await DataService.SaveAsync(new Order(Name, Items.ToList()));
}
```

---

## Scenario 6: Auto-Execute Command on Property Change

```csharp
// Command auto-executes when SearchTerm changes
[ObservableCommand(nameof(SearchAsync))]
[ObservableCommandTrigger(nameof(SearchTerm))]
public partial IObservableCommandAsync SearchCommand { get; }

[ObservableTrigger(nameof(ResetSearch))]
public partial string SearchTerm { get; set; } = "";

private void ResetSearch()
{
    Results = [];  // Clear while searching
}

private async Task SearchAsync(CancellationToken ct)
{
    Results = await SearchService.QueryAsync(SearchTerm, ct);
}
```

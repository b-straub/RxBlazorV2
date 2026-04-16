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

---

## Scenario 7: Form Data in Model, Not Component

**Problem**: Component hook copies loaded data into a local field for form binding.

**Before** (form data in component):
```csharp
// Component
private UserProfileData _formData = new();

protected override void OnProfileLoadedChanged()
{
    if (Model.ProfileLoaded && Model.Profile is not null)
    {
        _formData = new UserProfileData
        {
            Username = Model.Profile.Username,
            Email = Model.Profile.Email
        };
    }
}
```

**After** (form data in model):
```csharp
// Model - prepare FormData inside the command
public partial UserProfileData? FormData { get; set; }

private async Task LoadProfileAsync(CancellationToken ct)
{
    var profile = await ProfileService.LoadAsync(ct);
    if (profile is not null)
    {
        FormData = new UserProfileData
        {
            Username = profile.Username,
            Email = profile.Email
        };
    }
}
```

```razor
@* Component - just binds, no hook needed *@
@if (Model.FormData is not null)
{
    <EditForm Model="Model.FormData" OnValidSubmit="OnValidSubmitAsync">
        <MudTextField @bind-Value="Model.FormData.Username" />
    </EditForm>
}
```

**Why**: The component hook was a middleman -- it received a notification, transformed data, and stored it locally. Moving the preparation into the command keeps the component as a pure view.

---

## Scenario 8: Status Forwarding via Trigger, Not Component Hook

**Problem**: Component hook forwards model property to status service.

**Before** (component middleman):
```csharp
// Component hook
protected override void OnSuccessMessageChanged()
{
    if (Model.SuccessMessage is not null)
    {
        Model.StatusModel.AddInfo(Model.SuccessMessage);
    }
}
```

**After** (model trigger):
```csharp
// Model - handles its own side effects
[ObservableTrigger(nameof(OnSuccessMessageChanged))]
public partial string? SuccessMessage { get; set; }

private void OnSuccessMessageChanged()
{
    if (SuccessMessage is not null)
    {
        StatusModel.AddInfo(SuccessMessage);
    }
}
```

**Why**: The component was just relaying -- the model already has access to `StatusModel` via DI. Use `[ObservableTrigger]` to keep side effects in the model layer.

---

## Scenario 9: EventCallback Bubbling Between Reactive Components ("EventCallback Hell")

**Problem**: A child `*ModelComponent` declares `[Parameter] EventCallback OnXxxRequested` parameters and bubbles button clicks up to a parent that opens a dialog, navigates, or calls a service. The parent and child both inherit from a generated `*ModelComponent` (or both have access to the same Model via DI).

This is the most common AI-generated fallback pattern -- when the model isn't recognized as the communication channel, generated code defaults to vanilla Blazor parent-child event plumbing.

**Before** (event bubbling):
```razor
@* ContactsPanel.razor — child *@
@inherits ContactsModelComponent

<MudButton OnClick="@(() => OnCreateInviteRequested.InvokeAsync())">
    Create Invite Link
</MudButton>
<MudButton OnClick="@(() => OnCheckResponsesRequested.InvokeAsync())">
    Check Responses
</MudButton>

@code {
    [Parameter] public EventCallback OnCreateInviteRequested { get; set; }
    [Parameter] public EventCallback OnCheckResponsesRequested { get; set; }
}
```

```razor
@* Contacts.razor — parent *@
@inherits ContactsModelComponent
@inject IDialogService DialogService

<ContactsPanel OnCreateInviteRequested="OpenCreateInviteDialogAsync"
               OnCheckResponsesRequested="OpenCheckResponsesDialogAsync" />

@code {
    private async Task OpenCreateInviteDialogAsync()
    {
        if (!await Model.PrfModel.EnsureKeysAsync()) return;
        await InviteLinkModel.LoadProfileAsync();
        await DialogService.ShowAsync<CreateInviteLinkDialog>("Create Invite Link", options);
    }
}
```

**Why it's wrong:**
- Both components share the same `Model` instance (DI-injected by the generated `*ModelComponent` base)
- Child re-uses `EventCallback` plumbing instead of the reactive Model -- defeats the entire reactive pipeline
- Loses CanExecute auto-disable, Executing state, cancellation token support
- Creates parent-child coupling: the child can't be reused without the parent's exact handler shape

**After — Option A: Move dialog logic into the child** (preferred — shortest, most cohesive):
```razor
@* ContactsPanel.razor — handles its own dialogs *@
@inherits ContactsModelComponent
@inject IDialogService DialogService
@inject InviteLinkModel InviteLinkModel

<MudButtonAsyncRx Command="Model.OpenCreateInvite">Create Invite Link</MudButtonAsyncRx>
<MudButtonAsyncRx Command="Model.OpenCheckResponses">Check Responses</MudButtonAsyncRx>

@code {
    // Dialog show is UI behaviour; keep it in the component but driven by command
    protected override async Task OnContextReadyAsync()
    {
        Model.OpenCreateInvite.RegisterHandler(OpenCreateInviteDialogAsync);
        Model.OpenCheckResponses.RegisterHandler(OpenCheckResponsesDialogAsync);
    }
    // ... dialog show methods stay here, parent no longer needs to know
}
```

**After — Option B: Make it a model command with a dialog service** (when the same action is needed from multiple components):
```csharp
// Inject a dialog presenter service into the model
public partial ContactsModel(IDialogPresenter dialogs, PrfModel prfModel, InviteLinkModel inviteLinkModel);

[ObservableCommand(nameof(OpenCreateInviteAsync))]
public partial IObservableCommandAsync OpenCreateInvite { get; }

private async Task OpenCreateInviteAsync()
{
    if (!await PrfModel.EnsureKeysAsync()) return;
    await InviteLinkModel.LoadProfileAsync();
    await _dialogs.ShowAsync<CreateInviteLinkDialog>("Create Invite Link");
}
```

```razor
@* Both parent and child just bind — no EventCallback plumbing *@
<MudButtonAsyncRx Command="Model.OpenCreateInvite">Create Invite Link</MudButtonAsyncRx>
```

**After — Option C: Signal via model property + ObservableComponentTrigger** (when the parent is the only one that knows how to render the dialog):
```csharp
// Model
[ObservableComponentTrigger]
public partial DialogRequest? RequestedDialog { get; set; }

[ObservableCommand(nameof(RequestCreateInvite))]
public partial IObservableCommand OpenCreateInvite { get; }

private void RequestCreateInvite() => RequestedDialog = DialogRequest.CreateInvite;
```

```csharp
// Parent component reacts via generated hook — no callback declared
protected override async Task OnRequestedDialogChangedAsync(CancellationToken ct)
{
    if (Model.RequestedDialog is null) return;
    var request = Model.RequestedDialog;
    Model.RequestedDialog = null;  // consume
    await ShowDialogAsync(request, ct);
}
```

**Decision rule:**
- Action is purely UI (show a dialog, navigate, focus an element) → **Option A** (handle in child)
- Action needs to be triggered from many places, or includes async/business logic → **Option B** (model command + service)
- Only the parent has the right context (e.g., layout-specific dialog) → **Option C** (property + component trigger)

**Anti-pattern signal**: If you're writing `[Parameter] EventCallback` on a `*ModelComponent`-derived component, ask: "Why doesn't the child use the Model directly?" The answer is almost always "because it can — I just defaulted to event plumbing."

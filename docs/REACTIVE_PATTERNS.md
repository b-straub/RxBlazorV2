# RxBlazorV2 Reactive Patterns Best Practices

This document provides comprehensive guidance for implementing reactive patterns in RxBlazorV2. It is designed to be AI-friendly and serves as the primary reference for understanding when and how to use each reactive pattern.

## Table of Contents

1. [Core Concepts](#core-concepts)
2. [Quick Reference: Pattern Decision Matrix](#quick-reference-pattern-decision-matrix)
3. [Pattern Catalog](#pattern-catalog)
4. [Abstract Base Class Contracts](#11-abstract-base-class-contracts)
5. [File Organization Best Practices](#file-organization-best-practices)
6. [Model Lifecycle](#model-lifecycle)
7. [Domain Architecture Patterns](#domain-architecture-patterns)
8. [Multi-Assembly Considerations](#multi-assembly-considerations)
9. [Anti-Patterns](#anti-patterns)
10. [Diagnostic Reference](#diagnostic-reference)

---

## Core Concepts

### What is RxBlazorV2?

RxBlazorV2 is a reactive programming framework for Blazor applications built on R3 (Reactive Extensions). It uses Roslyn source generation to automatically create:

- Observable properties with change notifications
- Command bindings with async support
- Dependency injection registration
- Component integration with hook methods

### Fundamental Principle

**All reactive behavior is declarative through attributes.** The source generator handles subscription management, ensuring correct patterns and preventing memory leaks.

### Critical Anti-Pattern: Direct Observable Access (RXBG090)

**NEVER access the `Observable` property directly in user code:**

```csharp
// WRONG - triggers RXBG090 warning
Subscriptions.Add(Observable
    .Where(p => p.Intersect(["Model.Name"]).Any())
    .Subscribe(_ => DoSomething()));

// CORRECT - use attributes instead
[ObservableTrigger(nameof(DoSomething))]
public partial string Name { get; set; }
```

The `Observable` property is for generated code only. User code should always use attribute-based patterns.

---

## Quick Reference: Pattern Decision Matrix

Use this matrix to quickly determine which pattern fits your use case:

| I want to... | Use this pattern | Attribute/Mechanism |
|--------------|------------------|---------------------|
| Execute code when my own property changes | Property Trigger | `[ObservableTrigger]` / `[ObservableTriggerAsync]` |
| Execute code when injected model's property changes | Internal Observer | Auto-detected private methods |
| Bind a button click to a method | Command | `[ObservableCommand]` |
| Auto-execute a command when a property changes | Command Trigger | `[ObservableCommandTrigger]` |
| React to property changes in UI component | Component Trigger | `[ObservableComponentTrigger]` |
| Have external service observe model changes | External Observer | `[ObservableModelObserver]` |
| Share state between models | Model Reference | Partial constructor injection |
| Define reusable reactive contracts | Abstract Base Class | `override partial` + attribute transfer |

### Pattern Selection by Property Location

| Property belongs to... | Pattern | Example |
|------------------------|---------|---------|
| Same model (`this.Name`) | `[ObservableTrigger]` | Validation, computed values |
| Injected model (`Settings.Theme`) | Internal Observer (auto-detected) | Cross-model reactions |
| Any model (from external service) | `[ObservableModelObserver]` | Logging, analytics, side effects |

---

## Pattern Catalog

### 1. Partial Properties

**Purpose:** Reactive state with automatic change notifications.

**Declaration:**
```csharp
public partial string Name { get; set; } = "Default";
public partial int Count { get; set; }
```

**Generated Code:**
```csharp
public partial string Name
{
    get => field;
    set
    {
        if (field != value)
        {
            field = value;
            StateHasChanged("Model.Name");
        }
    }
}
```

**Key Points:**
- Must use `partial` keyword
- Initializers go on declaration, not generated code
- Change detection uses equality comparison

---

### 2. Property Triggers (`[ObservableTrigger]`)

**Purpose:** Execute internal methods automatically when properties change.

**When to use:**
- Validation logic
- Computed property updates
- Logging/auditing
- Side effects within the same model

**Sync Trigger:**
```csharp
[ObservableTrigger(nameof(ValidateEmail))]
public partial string Email { get; set; } = "";

private void ValidateEmail()
{
    IsEmailValid = Email.Contains('@') && Email.Contains('.');
}
```

**Async Trigger with Guard:**
```csharp
[ObservableTriggerAsync(nameof(AutoSaveAsync), nameof(CanAutoSave))]
public partial string DocumentContent { get; set; } = "";

private async Task AutoSaveAsync()
{
    await _repository.SaveAsync(DocumentContent);
}

private bool CanAutoSave() => !string.IsNullOrWhiteSpace(DocumentContent);
```

**Parametrized Trigger:**
```csharp
[ObservableTrigger<string>(nameof(LogChange), "Counter changed")]
public partial int Counter { get; set; }

private void LogChange(string message)
{
    _logger.Log($"{message}: {Counter}");
}
```

**Multiple Triggers on Same Property:**
```csharp
[ObservableTrigger(nameof(UpdateFullName))]
[ObservableTrigger(nameof(ValidateName))]
public partial string FirstName { get; set; } = "";
```

**Generated Code (simplified):**
```csharp
// In constructor:
Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.Email"]).Any())
    .Subscribe(_ => ValidateEmail()));

Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.DocumentContent"]).Any())
    .Where(_ => CanAutoSave())
    .SubscribeAwait(async (_, ct) => await AutoSaveAsync(), AwaitOperation.Switch));
```

**Valid Method Signatures:**
| Pattern | Signature |
|---------|-----------|
| Sync | `private void MethodName()` |
| Async | `private Task MethodName()` |
| Async with CT | `private Task MethodName(CancellationToken ct)` |
| Parametrized sync | `private void MethodName(T value)` |
| Parametrized async | `private Task MethodName(T value, CancellationToken ct)` |

---

### 3. Commands (`[ObservableCommand]`)

**Purpose:** Bind UI actions (buttons, etc.) to methods with execution state tracking.

**Basic Command:**
```csharp
[ObservableCommand(nameof(IncrementSync))]
public partial IObservableCommand IncrementCommand { get; }

private void IncrementSync()
{
    Counter++;
}
```

**Async Command:**
```csharp
[ObservableCommand(nameof(LoadDataAsync))]
public partial IObservableCommandAsync LoadDataCommand { get; }

private async Task LoadDataAsync()
{
    Data = await _service.GetDataAsync();
}
```

**Command with CanExecute:**
```csharp
[ObservableCommand(nameof(Submit), nameof(CanSubmit))]
public partial IObservableCommand SubmitCommand { get; }

private void Submit() { /* ... */ }
private bool CanSubmit() => IsValid && !IsSubmitting;
```

**Parametrized Command:**
```csharp
[ObservableCommand(nameof(DeleteItem))]
public partial IObservableCommand<int> DeleteItemCommand { get; }

private void DeleteItem(int itemId)
{
    Items.RemoveAt(itemId);
}
```

**Command with Return Value:**
```csharp
[ObservableCommand(nameof(Calculate))]
public partial IObservableCommandR<int> CalculateCommand { get; }

private int Calculate() => A + B;
```

**Command Interfaces:**
| Interface | Use Case |
|-----------|----------|
| `IObservableCommand` | Sync, no params, no return |
| `IObservableCommandAsync` | Async, no params, no return |
| `IObservableCommand<T>` | Sync, with param, no return |
| `IObservableCommandAsync<T>` | Async, with param, no return |
| `IObservableCommandR<R>` | Sync, no params, with return |
| `IObservableCommandRAsync<R>` | Async, no params, with return |

---

### 4. Command Triggers (`[ObservableCommandTrigger]`)

**Purpose:** Auto-execute commands when specific properties change.

**Use Cases:**
- Search-as-you-type
- Auto-refresh on settings change
- Cascading updates

```csharp
[ObservableCommand(nameof(SearchAsync), nameof(CanSearch))]
[ObservableCommandTrigger(nameof(SearchText))]
public partial IObservableCommandAsync SearchCommand { get; }

public partial string SearchText { get; set; } = "";

private async Task SearchAsync(CancellationToken ct)
{
    Results = await _searchService.SearchAsync(SearchText, ct);
}

private bool CanSearch() => SearchText.Length >= 3;
```

**Cross-Model Command Trigger:**
```csharp
// Trigger command when referenced model's property changes
// Use string with dot notation (nameof doesn't work for paths)
[ObservableCommand(nameof(RefreshAsync))]
[ObservableCommandTrigger("Settings.IsDay")]
public partial IObservableCommandAsync RefreshCommand { get; }

public partial ModelName(SettingsModel settings);
```

**Limitation:** Only ONE level of nesting is supported. Deep paths like `"PrfModel.InviteModel.Property"` don't work. Solution: Inject the model directly or use the Service-Model Interaction pattern (Section 9).

**Cancellation Strategies:**

When command has `CancellationToken` parameter → **Switch** (cancels previous):
```csharp
// Only latest execution runs, previous cancelled
private async Task SearchAsync(CancellationToken ct)
```

When command has no `CancellationToken` → **Merge** (all run to completion):
```csharp
// All executions run in parallel
private async Task NotifyAsync()
```

---

### 5. Component Triggers (`[ObservableComponentTrigger]`)

**Purpose:** Generate hook methods in Blazor components for specific property changes.

**When to use:**
- UI-specific reactions (animations, focus, scroll)
- DOM manipulation
- Third-party JS interop
- Component-level side effects

**Basic Component Trigger:**
```csharp
[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial string Theme { get; set; } = "Light";
}
```

**Generated Component Hook:**
```csharp
// In SettingsModelComponent.g.cs
protected virtual void OnThemeChanged() { }
```

**Implement in Razor:**
```csharp
// SettingsPage.razor.cs
public partial class SettingsPage : SettingsModelComponent
{
    protected override void OnThemeChanged()
    {
        // Apply theme via JS interop
        JS.InvokeVoidAsync("applyTheme", Model.Theme);
    }
}
```

**Trigger Types:**
| Type | Behavior |
|------|----------|
| `RenderAndHook` (default) | Re-renders component AND calls hook |
| `RenderOnly` | Re-renders only, no hook method generated |
| `HookOnly` | Calls hook only, no automatic re-render |

```csharp
[ObservableComponentTrigger(ComponentTriggerType.HookOnly)]
public partial string BackgroundStatus { get; set; }
```

**Async Component Trigger:**
```csharp
[ObservableComponentTriggerAsync(hookMethodName: "HandleUserUpdateAsync")]
public partial string UserName { get; set; }

// Generated:
protected virtual Task HandleUserUpdateAsync(CancellationToken ct) => Task.CompletedTask;
```

**Referenced Model Triggers:**

When a model references another model, component triggers propagate:

```csharp
// SettingsModel.cs (no [ObservableComponent] needed here)
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial bool IsDay { get; set; }
}

// WeatherModel.cs
[ObservableComponent] // includeReferencedTriggers: true by default
public partial class WeatherModel : ObservableModel
{
    public partial WeatherModel(SettingsModel settings);
}

// Generated in WeatherModelComponent:
protected virtual void OnSettingsIsDayChanged() { }
```

---

### 6. Model References (Partial Constructor Injection)

**Purpose:** Share state between models with automatic subscription management.

```csharp
[ObservableModelScope(ModelScope.Scoped)]
public partial class WeatherModel : ObservableModel
{
    // Declare dependency via partial constructor
    public partial WeatherModel(SettingsModel settings, IWeatherService weatherService);

    // Access via generated protected property
    public bool UseMetric => Settings.UseMetricSystem;
}
```

**Generated Code:**
```csharp
public partial class WeatherModel
{
    protected SettingsModel Settings { get; }
    private readonly IWeatherService _weatherService;

    public WeatherModel(SettingsModel settings, IWeatherService weatherService)
    {
        Settings = settings;
        _weatherService = weatherService;

        // Auto-subscribe to Settings.Observable
        Subscriptions.Add(Settings.Observable
            .Where(p => FilterUsedProperties(p))
            .Subscribe(StateHasChanged));
    }
}
```

**Rules:**
- `ObservableModel` parameters → `protected` properties (PascalCase)
- Other services → `private readonly` fields (underscore prefix)
- Referenced models must be used (RXBG012)
- No circular references allowed (RXBG010)

---

### 7. Internal Model Observers (Auto-Detected)

**Purpose:** React to changes in injected ObservableModel properties without explicit attributes.

Private methods that access properties from injected models are auto-detected:

```csharp
public partial class WeatherModel : ObservableModel
{
    public partial WeatherModel(SettingsModel settings);

    // Auto-detected: accesses Settings.AutoRefresh and Settings.RefreshInterval
    private void UpdateAutoRefreshTimer()
    {
        if (Settings.AutoRefresh)
        {
            StartTimer(Settings.RefreshInterval);
        }
    }
}
```

**Generated Subscription:**
```csharp
// In constructor:
Subscriptions.Add(Settings.Observable
    .Where(p => p.Intersect(["Model.AutoRefresh", "Model.RefreshInterval"]).Any())
    .Subscribe(_ => UpdateAutoRefreshTimer()));
```

**Valid Signatures for Internal Observers:**
| Signature | Generated Subscription |
|-----------|----------------------|
| `private void Method()` | `.Subscribe(_ => Method())` |
| `private Task Method()` | `.SubscribeAwait(async _ => await Method())` |
| `private Task Method(CancellationToken ct)` | `.SubscribeAwait(async (_, ct) => await Method(ct))` |

---

### 8. External Model Observers (`[ObservableModelObserver]`)

**Purpose:** Allow external services to react to model property changes.

```csharp
// Service class
public class LoggingService
{
    [ObservableModelObserver(nameof(UserModel.Username))]
    public void OnUsernameChanged(UserModel model)
    {
        _logger.Log($"Username changed to: {model.Username}");
    }

    [ObservableModelObserver(nameof(UserModel.Email))]
    public async Task OnEmailChangedAsync(UserModel model, CancellationToken ct)
    {
        await _notifier.NotifyEmailChangeAsync(model.Email, ct);
    }
}

// Model injects the service
public partial class UserModel : ObservableModel
{
    public partial UserModel(LoggingService logger);

    public partial string Username { get; set; }
    public partial string Email { get; set; }
}
```

**Multiple Properties on One Observer:**
```csharp
[ObservableModelObserver(nameof(UserModel.Theme))]
[ObservableModelObserver(nameof(UserModel.Language))]
public void OnAppearanceChanged(UserModel model)
{
    // Called when Theme OR Language changes
}
```

**Valid Signatures:**
| Signature | Use Case |
|-----------|----------|
| `void Method(Model model)` | Sync observer |
| `Task Method(Model model)` | Async observer |
| `Task Method(Model model, CancellationToken ct)` | Async with cancellation |

---

### 9. Automatic Error Handling (`IErrorModel`)

**Purpose:** Centralized error handling for all command exceptions without manual try/catch.

**How It Works:**
When a model injects `IErrorModel` via partial constructor, all command exceptions are automatically captured and routed to the `HandleError` method.

**Error Model Implementation:**
```csharp
using ObservableCollections;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;

[ObservableComponent]
[ObservableModelScope(ModelScope.Singleton)]
public partial class ErrorModel : ObservableModel, IErrorModel
{
    [ObservableComponentTrigger]
    public ObservableList<string> Errors { get; } = [];

    public void HandleError(Exception error)
    {
        Errors.Add(error.Message);
    }
}
```

**Model with Automatic Error Capture:**
```csharp
[ObservableModelScope(ModelScope.Scoped)]
public partial class OrderModel : ObservableModel
{
    // Inject IErrorModel to enable automatic error capture
    public partial OrderModel(IErrorModel errorModel, IOrderService orderService);

    [ObservableCommand(nameof(SubmitOrderAsync))]
    public partial IObservableCommandAsync SubmitCommand { get; }

    private async Task SubmitOrderAsync(CancellationToken ct)
    {
        // No try/catch needed - exceptions automatically go to ErrorModel.HandleError()
        await OrderService.SubmitAsync(CurrentOrder, ct);
    }
}
```

**Generated Error Handling Code:**
```csharp
// The generator wraps command execution with error handling:
public partial OrderModel
{
    // In constructor, commands are created with error handler:
    SubmitCommand = ObservableCommandAsyncFactory.Create(
        async ct => await SubmitOrderAsync(ct),
        canExecute: null,
        errorHandler: _errorModel);  // Auto-injected error handler
}
```

**Key Points:**
- Inject `IErrorModel` via partial constructor (generates `_errorModel` field)
- All command exceptions are automatically routed to `HandleError(Exception)`
- No manual try/catch needed in command methods
- Works with both sync and async commands
- Use `RxBlazorV2.MudBlazor.StatusDisplay` for UI integration

**UI Integration with StatusDisplay:**
```razor
@using RxBlazorV2.MudBlazor.Components

<MudAppBar>
    <MudSpacer />
    <StatusDisplay />  @* Shows errors automatically *@
</MudAppBar>
```

**When to Use:**
- Any application that needs centralized error handling
- Applications with many commands that could throw
- When you want consistent error UI feedback
- To avoid repetitive try/catch blocks in command methods

---

### 10. Service-Model Interaction Pattern

**Purpose:** When a model needs to call an external service and other models need to react to completion.

**The Correct Pattern:**
```
Property changes → [ObservableCommandTrigger] Command executes →
Service called → Status property set → Other model's internal observer reacts
```

**Example - ProcessingModel (calls service):**
```csharp
[ObservableModelScope(ModelScope.Scoped)]
public partial class ProcessingModel : ObservableModel
{
    public partial ProcessingModel(ProcessingService processingService);

    /// <summary>
    /// Input that triggers processing when set.
    /// </summary>
    public partial string? InputToProcess { get; set; }

    /// <summary>
    /// Status message - THIS IS THE COMPLETION SIGNAL.
    /// Other models observe this to know when processing completes.
    /// </summary>
    public partial ProcessingStatus? Status { get; set; }

    /// <summary>
    /// Command auto-triggered when InputToProcess changes.
    /// </summary>
    [ObservableCommand(nameof(ProcessAsync))]
    [ObservableCommandTrigger(nameof(InputToProcess))]
    public partial IObservableCommandAsync ProcessCommand { get; }

    private async Task ProcessAsync(CancellationToken ct)
    {
        try
        {
            var result = await ProcessingService.DoWorkAsync(InputToProcess!, ct);
            Status = new ProcessingStatus(result, Severity.Success);
        }
        catch (Exception ex)
        {
            Status = new ProcessingStatus(ex.Message, Severity.Error);
        }
    }
}
```

**Example - ResultsModel (reacts to completion):**
```csharp
[ObservableModelScope(ModelScope.Scoped)]
public partial class ResultsModel : ObservableModel
{
    public partial ResultsModel(ProcessingModel processingModel);

    /// <summary>
    /// Internal observer - AUTO-DETECTED because it accesses ProcessingModel.Status.
    /// Called automatically when Status changes.
    /// </summary>
    private void OnProcessingCompleted()
    {
        if (ProcessingModel.Status?.Severity == Severity.Success)
        {
            // React to successful completion, e.g., reload data
            _ = LoadDataCommand.ExecuteAsync();
        }
    }
}
```

**Key Points:**
- Model owns the command, not an external service
- Service is injected into model and called from command method
- Status property carries semantic meaning (success/error)
- Other models use internal observers (auto-detected) to react
- NO toggle properties, NO callbacks, NO external observer orchestration

**When to use External Observers (`[ObservableModelObserver]`):**
External observers are for **fire-and-forget side effects** only:
- Persistence (save to storage)
- Logging/analytics
- External notifications

They should NOT orchestrate workflows or call back to models.

---

### 11. Abstract Base Class Contracts

**Purpose:** Define reusable reactive contracts in abstract base classes with automatic attribute transfer to concrete implementations.

**When to use:**
- Shared reactive patterns across multiple models
- Cross-assembly base classes (e.g., library providing base, app providing concrete)
- Standardizing reactive behavior (e.g., status handling, error patterns)
- Separating contract definition from implementation

**Abstract Base Class:**
```csharp
/// <summary>
/// Abstract base class defining reactive contract.
/// Concrete implementations get all reactive behavior automatically.
/// </summary>
public abstract class StatusBaseModel : ObservableModel
{
    /// <summary>
    /// Messages collection - triggers component hook when changed.
    /// </summary>
    [ObservableComponentTrigger]
    public abstract ObservableList<StatusMessage> Messages { get; }

    /// <summary>
    /// Guard property - triggers both component hook and method call.
    /// </summary>
    [ObservableComponentTrigger]
    [ObservableTrigger(nameof(CanAddMessageTrigger))]
    public abstract bool CanAddMessage { get; set; }

    /// <summary>
    /// Command - auto-executes when CanAddMessage changes.
    /// </summary>
    [ObservableCommandTrigger(nameof(CanAddMessage))]
    public abstract IObservableCommand AddMessageCommand { get; }

    protected abstract void CanAddMessageTrigger();

    // Non-abstract members work as normal
    public void AddError(string message) => Messages.Add(new StatusMessage(message, Severity.Error));
    public void ClearMessages() => Messages.Clear();
}
```

**Concrete Implementation:**
```csharp
[ObservableComponent]
[ObservableModelScope(ModelScope.Singleton)]
public partial class AppStatusModel : StatusBaseModel
{
    // Override abstract properties - attributes transfer automatically
    public override ObservableList<StatusMessage> Messages { get; } = [];

    // Use 'override partial' for properties that need code generation
    public override partial bool CanAddMessage { get; set; }

    // Command needs [ObservableCommand] attribute for implementation binding
    [ObservableCommand(nameof(AddMessage))]
    public override partial IObservableCommand AddMessageCommand { get; }

    // Implement abstract trigger method
    protected override void CanAddMessageTrigger()
    {
        // Called when CanAddMessage changes (from [ObservableTrigger] on base)
    }

    // Command implementation
    private void AddMessage()
    {
        Messages.Add(new StatusMessage("New message", Severity.Info));
    }
}
```

**Generated Code:**
```csharp
public partial class AppStatusModel
{
    // Override modifier is included
    public override partial bool CanAddMessage
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                StateHasChanged("Model.CanAddMessage");
            }
        }
    }

    private ObservableCommand _addMessageCommand;

    public override partial IObservableCommand AddMessageCommand
    {
        get => _addMessageCommand;
    }

    public AppStatusModel() : base()
    {
        // Collection observation (from [ObservableComponentTrigger] on Messages)
        Subscriptions.Add(Messages.ObserveChanged()
            .Subscribe(_ => StateHasChanged("Model.Messages")));

        // Command initialization
        _addMessageCommand = new ObservableCommandFactory(this, [""], "AddMessageCommand", "AddMessage", AddMessage);

        // Command trigger (from [ObservableCommandTrigger] on base)
        Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.CanAddMessage"]).Any())
            .Subscribe(_ => _addMessageCommand.Execute()));

        // Property trigger (from [ObservableTrigger] on base)
        Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.CanAddMessage"]).Any())
            .Subscribe(_ => CanAddMessageTrigger()));
    }
}
```

**Transferred Attributes:**
| Base Class Attribute | Transfer Behavior |
|---------------------|-------------------|
| `[ObservableComponentTrigger]` | Generates component hook subscription |
| `[ObservableComponentTriggerAsync]` | Generates async component hook subscription |
| `[ObservableTrigger]` | Generates property trigger subscription |
| `[ObservableTriggerAsync]` | Generates async property trigger subscription |
| `[ObservableCommandTrigger]` | Generates command auto-execute subscription |

**Key Points:**
- Abstract base class = reactive **contract** definition
- Concrete class uses `override partial` = attribute **transfer** + code generation
- Works **across assemblies** (base in library, concrete in app)
- Generator produces `override` modifier automatically
- Commands still need `[ObservableCommand]` on concrete class for method binding
- Non-partial override properties (like `Messages`) don't get generated code but still trigger component hooks

**Use Cases:**
1. **Status/Error Handling**: `StatusBaseModel` → `AppStatusModel`, `ErrorModel`
2. **Form Validation**: `FormBaseModel` → `ContactFormModel`, `LoginFormModel`
3. **Data Repositories**: `RepositoryBaseModel<T>` → `ProductRepository`, `OrderRepository`
4. **Settings Patterns**: `SettingsBaseModel` → `UserSettingsModel`, `AppSettingsModel`

---

## File Organization Best Practices

For larger models with many properties, commands, triggers, and observers, split the partial class across multiple files. This improves readability and maintainability.

### Recommended File Structure

```
Models/
├── MyModel.cs              # Properties, constructor, attributes
├── MyModel.Commands.cs     # Command properties and implementations
├── MyModel.Triggers.cs     # Trigger method implementations
├── MyModel.Observers.cs    # Internal observer methods
└── MyModel.Methods.cs      # Public API methods
```

### Example: TodoListModel Organization

**TodoListModel.cs** - Properties and constructor:
```csharp
/// <summary>
/// Todo list domain model - manages todo items.
///
/// File organization:
/// - TodoListModel.cs: Constructor and properties
/// - TodoListModel.Commands.cs: Commands with their implementations
/// - TodoListModel.Observers.cs: Internal observer methods
/// - TodoListModel.Methods.cs: Public API methods
/// </summary>
[ObservableComponent]
[ObservableModelScope(ModelScope.Singleton)]
public partial class TodoListModel : ObservableModel
{
    public partial TodoListModel(StorageModel storage, AuthModel auth, StatusModel status);

    [ObservableComponentTrigger]
    public partial ObservableList<TodoItem> UserItems { get; init; } = [];

    public partial string NewItemTitle { get; set; } = string.Empty;
    public partial int CompletedCount { get; set; }
    public partial int PendingCount { get; set; }
}
```

**TodoListModel.Commands.cs** - Commands:
```csharp
public partial class TodoListModel
{
    [ObservableCommand(nameof(AddItem), nameof(CanAddItem))]
    public partial IObservableCommand AddCommand { get; }

    [ObservableCommand(nameof(ClearCompletedAsync))]
    public partial IObservableCommandAsync ClearCompletedCommand { get; }

    private bool CanAddItem() =>
        Auth.IsAuthenticated && !string.IsNullOrWhiteSpace(NewItemTitle);

    private void AddItem()
    {
        // Implementation
    }

    private async Task ClearCompletedAsync(CancellationToken ct)
    {
        // Implementation
    }
}
```

**TodoListModel.Observers.cs** - Internal observers:
```csharp
public partial class TodoListModel
{
    /// <summary>
    /// Internal observer - auto-detected because it accesses Auth.CurrentUser.
    /// </summary>
    private void RefreshItems()
    {
        UserItems.Clear();
        if (Auth.CurrentUser is null)
        {
            return;
        }
        // Load items for user
    }
}
```

**TodoListModel.Methods.cs** - Public API:
```csharp
public partial class TodoListModel
{
    public void ToggleItem(Guid itemId)
    {
        // Implementation
    }

    public void RemoveItem(Guid itemId)
    {
        // Implementation
    }
}
```

### When to Split Files

| Criteria | Single File | Multiple Files |
|----------|-------------|----------------|
| Properties | < 5 | ≥ 5 |
| Commands | < 2 | ≥ 2 |
| Has internal observers | No | Yes |
| Has public API methods | No | Yes |
| Total lines | < 100 | > 100 |

---

## Model Lifecycle

### OnContextReady / OnContextReadyAsync

Override these methods for initialization logic that runs after all DI dependencies are injected:

```csharp
[ObservableModelScope(ModelScope.Singleton)]
public partial class StorageModel : ObservableModel
{
    /// <summary>
    /// Sync initialization - runs first.
    /// </summary>
    protected override void OnContextReady()
    {
        SeedDemoData();
    }

    /// <summary>
    /// Async initialization - runs after OnContextReady.
    /// </summary>
    protected override async Task OnContextReadyAsync()
    {
        var data = await LoadPersistedDataAsync();
        // Apply data
    }
}
```

**Key Points:**
- `OnContextReady()` is called synchronously first
- `OnContextReadyAsync()` is called after, awaiting completion
- Referenced models' lifecycle methods run before dependent models
- Use for: seed data, loading persisted state, initial subscriptions

### SuspendNotifications Pattern

When making multiple property changes that should fire as a single notification:

```csharp
protected override async Task OnContextReadyAsync()
{
    // SuspendNotifications batches all changes into ONE notification
    using (SuspendNotifications())
    {
        var (items, settings) = await LoadPersistedDataAsync();

        if (settings is not null)
        {
            Settings = settings;  // No notification yet
        }

        if (items is { Count: > 0 })
        {
            Items.AddRange(items);  // No notification yet
        }
    }
    // Single notification fires here when disposed
}
```

**Use cases:**
- Loading multiple properties from storage
- Resetting form fields
- Bulk updates that shouldn't trigger observers multiple times

### UI-less ObservableModel (Backend Models)

Models without `[ObservableComponent]` attribute are used for backend/data concerns:

```csharp
/// <summary>
/// Storage domain - UI-less in-memory database.
/// No [ObservableComponent] = no generated component base class.
/// </summary>
[ObservableModelScope(ModelScope.Singleton)]
public partial class StorageModel : ObservableModel
{
    public partial StorageModel(StoragePersistenceObserver persistenceObserver);

    public partial ObservableList<TodoItem> Items { get; init; } = [];
    public partial ObservableList<User> Users { get; init; } = [];
    public partial AppSettings Settings { get; set; } = new();

    // CRUD methods
    public void AddItem(TodoItem item) { /* ... */ }
    public bool RemoveItem(Guid id) { /* ... */ }
}
```

**When to use UI-less models:**
- Data/repository layer
- Shared state without direct UI binding
- Backend services that other models observe
- Models used only via injection, not components

---

## Domain Architecture Patterns

### Data/Persistence Domain

**Repository Pattern with Async Loading:**
```csharp
[ObservableModelScope(ModelScope.Scoped)]
public partial class ProductCatalogModel : ObservableModel
{
    public partial ProductCatalogModel(IProductRepository repository);

    public partial bool IsLoading { get; set; }
    public partial Product[]? Products { get; set; }
    public partial string? ErrorMessage { get; set; }

    [ObservableCommand(nameof(LoadProductsAsync), nameof(CanLoad))]
    public partial IObservableCommandAsync LoadCommand { get; }

    [ObservableCommand(nameof(RefreshAsync))]
    [ObservableCommandTrigger(nameof(SearchFilter))]
    public partial IObservableCommandAsync RefreshCommand { get; }

    public partial string SearchFilter { get; set; } = "";

    private bool CanLoad() => !IsLoading;

    private async Task LoadProductsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            Products = await Repository.GetProductsAsync(SearchFilter);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        await Task.Delay(300, ct); // Debounce
        await LoadProductsAsync();
    }
}
```

**Cache Invalidation Pattern:**
```csharp
public partial class CacheModel : ObservableModel
{
    [ObservableTriggerAsync(nameof(InvalidateCacheAsync))]
    public partial DateTime LastModified { get; set; }

    private async Task InvalidateCacheAsync()
    {
        await _cache.InvalidateAsync();
        await ReloadDataAsync();
    }
}
```

---

### Authentication Domain

**User State Management:**
```csharp
[ObservableModelScope(ModelScope.Singleton)] // Singleton for auth state
public partial class AuthModel : ObservableModel
{
    public partial AuthModel(IAuthService authService);

    public partial bool IsAuthenticated { get; set; }
    public partial string? Username { get; set; }
    public partial string[]? Roles { get; set; }

    [ObservableCommand(nameof(LoginAsync))]
    public partial IObservableCommandAsync<LoginCredentials> LoginCommand { get; }

    [ObservableCommand(nameof(LogoutAsync))]
    public partial IObservableCommandAsync LogoutCommand { get; }

    private async Task LoginAsync(LoginCredentials credentials)
    {
        var result = await AuthService.LoginAsync(credentials);
        IsAuthenticated = result.Success;
        Username = result.Username;
        Roles = result.Roles;
    }

    private async Task LogoutAsync()
    {
        await AuthService.LogoutAsync();
        IsAuthenticated = false;
        Username = null;
        Roles = null;
    }
}
```

**Auth-Dependent Model:**
```csharp
[ObservableModelScope(ModelScope.Scoped)]
public partial class UserProfileModel : ObservableModel
{
    public partial UserProfileModel(AuthModel auth);

    // Internal observer: auto-detected access to Auth.IsAuthenticated
    private async Task OnAuthStateChanged()
    {
        if (Auth.IsAuthenticated)
        {
            await LoadProfileAsync();
        }
        else
        {
            ClearProfile();
        }
    }
}
```

---

### UI/Presentation Domain

**Form Validation Pattern:**
```csharp
[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class ContactFormModel : ObservableModel
{
    [ObservableTrigger(nameof(ValidateEmail))]
    public partial string Email { get; set; } = "";

    [ObservableTrigger(nameof(ValidateName))]
    public partial string Name { get; set; } = "";

    public partial string? EmailError { get; set; }
    public partial string? NameError { get; set; }
    public partial bool IsValid { get; set; }

    private void ValidateEmail()
    {
        EmailError = string.IsNullOrEmpty(Email) ? "Email required" :
                     !Email.Contains('@') ? "Invalid email" : null;
        UpdateIsValid();
    }

    private void ValidateName()
    {
        NameError = string.IsNullOrEmpty(Name) ? "Name required" :
                    Name.Length < 2 ? "Name too short" : null;
        UpdateIsValid();
    }

    private void UpdateIsValid()
    {
        IsValid = EmailError is null && NameError is null;
    }
}
```

**Loading State Pattern:**
```csharp
[ObservableComponent]
public partial class DataViewModel : ObservableModel
{
    public partial bool IsLoading { get; set; }
    public partial bool HasError { get; set; }
    public partial string? ErrorMessage { get; set; }

    // Component triggers for UI feedback
    [ObservableComponentTrigger(ComponentTriggerType.HookOnly)]
    public partial bool IsLoading { get; set; }

    [ObservableComponentTriggerAsync]
    public partial bool HasError { get; set; }
}

// In component:
protected override void OnIsLoadingChanged()
{
    // Show/hide loading spinner via JS
}

protected override async Task OnHasErrorChangedAsync(CancellationToken ct)
{
    if (Model.HasError)
    {
        await ShowToastAsync(Model.ErrorMessage);
    }
}
```

---

### Business Logic Domain

**Cross-Model Calculations:**
```csharp
[ObservableModelScope(ModelScope.Scoped)]
public partial class ShoppingCartModel : ObservableModel
{
    public partial ShoppingCartModel(ProductCatalogModel catalog, UserProfileModel user);

    public partial ObservableList<CartItem> Items { get; init; } = [];
    public partial decimal Subtotal { get; set; }
    public partial decimal Tax { get; set; }
    public partial decimal Total { get; set; }

    // Internal observer: reacts to User.DiscountLevel changes
    private void RecalculateTotals()
    {
        Subtotal = Items.Sum(i => i.Price * i.Quantity);
        var discount = User.DiscountLevel * 0.1m; // 10% per level
        Tax = (Subtotal * (1 - discount)) * 0.2m;
        Total = Subtotal * (1 - discount) + Tax;
    }

    [ObservableTrigger(nameof(RecalculateTotals))]
    public partial ObservableList<CartItem> Items { get; init; }
}
```

**Workflow Orchestration:**
```csharp
[ObservableModelScope(ModelScope.Scoped)]
public partial class CheckoutModel : ObservableModel
{
    public partial CheckoutModel(
        ShoppingCartModel cart,
        PaymentModel payment,
        ShippingModel shipping);

    public partial CheckoutStep CurrentStep { get; set; }

    [ObservableCommand(nameof(ProcessCheckoutAsync))]
    public partial IObservableCommandAsync CheckoutCommand { get; }

    private async Task ProcessCheckoutAsync()
    {
        CurrentStep = CheckoutStep.ValidatingCart;
        await ValidateCartAsync();

        CurrentStep = CheckoutStep.ProcessingPayment;
        await Payment.ProcessAsync();

        CurrentStep = CheckoutStep.ArrangingShipping;
        await Shipping.ScheduleAsync();

        CurrentStep = CheckoutStep.Complete;
    }
}
```

---

## Multi-Assembly Considerations

### When to Split Models into Assemblies

**Split when:**
- Shared models used by multiple applications
- Circular reference issues between domains
- Team boundaries require separation
- Reusable component libraries

**Keep together when:**
- Component triggers need to propagate (RXBG052)
- Tight coupling is intentional
- Single team ownership

### Cross-Assembly Limitation (RXBG052)

Component triggers from referenced models **only work within the same assembly**:

```csharp
// SharedLibrary.dll
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger] // This trigger...
    public partial bool IsDay { get; set; }
}

// MainApp.dll
[ObservableComponent]
public partial class WeatherModel : ObservableModel
{
    public partial WeatherModel(SettingsModel settings);
    // ...WILL NOT generate OnSettingsIsDayChanged() hook
    // because SettingsModel is in different assembly
}
```

**Solution:** Use internal observers or external observers instead:

```csharp
// MainApp.dll
public partial class WeatherModel : ObservableModel
{
    public partial WeatherModel(SettingsModel settings);

    // Internal observer works across assemblies
    private void OnSettingsChanged()
    {
        if (Settings.IsDay)
        {
            // Handle day mode
        }
    }
}
```

### Recommended Assembly Structure

```
Solution/
├── Domain.Shared/           # Shared models (Singleton scope)
│   ├── AuthModel.cs
│   └── SettingsModel.cs
├── Domain.Data/             # Data access
│   ├── ProductModel.cs
│   └── OrderModel.cs
├── Domain.UI/               # UI-specific models
│   ├── NavigationModel.cs
│   └── ThemeModel.cs
└── MainApp/                 # Application with components
    ├── Pages/
    └── Components/
```

---

## Anti-Patterns

### 1. Direct Observable Access (RXBG090)

```csharp
// WRONG
Subscriptions.Add(Observable
    .Where(p => p.Contains("Name"))
    .Subscribe(_ => HandleNameChange()));

// CORRECT
[ObservableTrigger(nameof(HandleNameChange))]
public partial string Name { get; set; }
```

### 2. Circular Triggers (RXBG031)

```csharp
// WRONG - infinite loop
[ObservableTrigger(nameof(UpdateA))]
public partial int A { get; set; }

private void UpdateA()
{
    A = A + 1; // Triggers itself!
}

// CORRECT - update different property
[ObservableTrigger(nameof(UpdateB))]
public partial int A { get; set; }

public partial int B { get; set; }

private void UpdateB()
{
    B = A * 2; // Updates B, not A
}
```

### 3. Scope Violations (RXBG051)

```csharp
// WRONG - Singleton depending on Scoped
[ObservableModelScope(ModelScope.Singleton)]
public partial class AppModel : ObservableModel
{
    public partial AppModel(UserModel user); // UserModel is Scoped = ERROR
}

// CORRECT - Match scopes appropriately
[ObservableModelScope(ModelScope.Scoped)]
public partial class AppModel : ObservableModel
{
    public partial AppModel(UserModel user); // Both Scoped = OK
}
```

### 4. Unused Model References (RXBG012)

```csharp
// WRONG - injected but never used
public partial class MyModel : ObservableModel
{
    public partial MyModel(OtherModel other); // Never access Other.*
}

// CORRECT - only inject what you use
public partial class MyModel : ObservableModel
{
    public partial MyModel(OtherModel other);

    private void DoSomething()
    {
        var value = Other.SomeProperty; // Actually use it
    }
}
```

### 5. Over-Engineering with Commands

```csharp
// WRONG - command for simple property update
[ObservableCommand(nameof(SetName))]
public partial IObservableCommand<string> SetNameCommand { get; }

private void SetName(string name) => Name = name;

// CORRECT - just use the property
public partial string Name { get; set; }
// In Razor: <input @bind="Model.Name" />
```

### 6. Dummy Property Access for Observer Detection

```csharp
// WRONG - external observer orchestrating workflow with callback
[ObservableModelObserver(nameof(InviteModel.LastInviteVerified))]
public async Task HandleInviteVerifiedAsync(InviteModel model, CancellationToken ct)
{
    var success = await _persistence.SaveAsync(...);
    model.NotifyContactsModified();  // Callback anti-pattern!
}

// WRONG - toggle property as notification signal
public void NotifyContactsModified()
{
    ContactsModifiedVersion = !ContactsModifiedVersion;  // Meaningless toggle!
}

// CORRECT - Model command owns workflow, sets Status
[ObservableCommand(nameof(ProcessVerificationAsync))]
[ObservableCommandTrigger(nameof(LastInviteVerified))]
public partial IObservableCommandAsync ProcessVerificationCommand { get; }

private async Task ProcessVerificationAsync(CancellationToken ct)
{
    var success = await _persistence.SaveAsync(LastInviteVerified, ct);
    Status = success
        ? new StatusMessage("Contact saved!", Severity.Success)
        : new StatusMessage("Save failed", Severity.Error);
}

// CORRECT - Other model observes Status (auto-detected)
private void OnInviteStatusChanged()
{
    if (InviteModel.Status?.Severity == Severity.Success)
    {
        _ = LoadContacts.ExecuteAsync();
    }
}
```

**Solution:** Use the Service-Model Interaction pattern (Section 9). Model commands own workflows, Status property signals completion, other models observe Status.

### 7. Manual StateHasChanged Calls

```csharp
// WRONG - bypassing reactive system
private void UpdateData()
{
    _data = LoadData();
    StateHasChanged(); // Anti-pattern!
}

// CORRECT - use partial property, UI updates automatically
public partial Data? CurrentData { get; set; }

private void UpdateData()
{
    CurrentData = LoadData(); // Triggers StateHasChanged via generator
}
```

**Manual `StateHasChanged()` almost always indicates:**
- Property isn't partial (not observable)
- Missing `[ObservableComponentTrigger]` for component hooks
- Working around broken reactive flow
- Field used instead of property

**Audit rule:** Any `StateHasChanged()` call in user code should be investigated. The reactive framework should handle all UI updates through property changes.

---

### 8. Missing Partial Modifier (RXBG072)

```csharp
// WRONG - missing partial
public class MyModel : ObservableModel // ERROR: not partial
{
    public string Name { get; set; } // ERROR: not partial
}

// CORRECT
public partial class MyModel : ObservableModel
{
    public partial string Name { get; set; }
}
```

---

## Diagnostic Reference

| ID | Severity | Description |
|----|----------|-------------|
| RXBG001 | Error | Observable model analysis error |
| RXBG002 | Error | Code generation error |
| RXBG010 | Error | Circular model reference detected |
| RXBG011 | Error | Invalid model reference target |
| RXBG012 | Error | Referenced model has no used properties |
| RXBG013 | Error | Cannot reference derived ObservableModel |
| RXBG014 | Error | Shared model must be Singleton |
| RXBG020 | Error | Generic type arity mismatch |
| RXBG021 | Error | Type constraint mismatch |
| RXBG030 | Error | Command trigger type mismatch |
| RXBG031 | Error | Circular trigger reference |
| RXBG032 | Error | Command method returns value unexpectedly |
| RXBG033 | Error | Command method missing return value |
| RXBG040 | Error | Invalid init accessor on property |
| RXBG041 | Warning | Unused component trigger |
| RXBG050 | Info | Unregistered service warning |
| RXBG051 | Error | DI scope violation |
| RXBG052 | Error | Referenced model in different assembly |
| RXBG060 | Error | Direct ObservableComponent inheritance |
| RXBG061 | Error | Same-assembly component composition |
| RXBG062 | Error | Non-reactive component |
| RXBG070 | Warning | Missing ObservableModelScope attribute |
| RXBG071 | Error | Non-public partial constructor |
| RXBG072 | Error | Missing partial modifier |
| RXBG080 | Error | Invalid external observer signature |
| RXBG081 | Error | Observer references non-existent property |
| RXBG082 | Warning | Invalid internal observer signature |
| RXBG090 | Warning | Direct Observable property access |

---

## Why Property Triggers Are Explicit (Not Auto-Detected)

You might wonder: if internal observers can be auto-detected for injected models, why can't we auto-detect triggers for same-model properties?

### The Fundamental Difference

| Aspect | Internal Observer (Auto-Detected) | Property Trigger (Explicit) |
|--------|----------------------------------|----------------------------|
| Property owner | Different model (`Settings.Theme`) | Same model (`this.Email`) |
| Intent when accessed | Always reactive - why else access another model's property in a method? | Often just reading - most methods read their own properties |
| False positive risk | Low - explicit model reference makes intent clear | High - nearly every method reads own properties |
| Circular trigger risk | Low - different models | High - method may write to property it reads |

### Why Auto-Detection Works for Injected Models

When a private method accesses `Settings.AutoRefresh`, the intent is clear:
```csharp
// Clear intent: react to Settings changes
private void UpdateTimer()
{
    if (Settings.AutoRefresh)  // ← Accesses injected model
    {
        StartTimer(Settings.RefreshInterval);
    }
}
```

### Why Auto-Detection Fails for Same-Model Properties

Most methods read their own properties without intending to react to changes:

```csharp
// Reading to use the value - NOT a trigger
private void SendEmail()
{
    _emailService.Send(Email, Subject, Body);  // Uses Email, doesn't react to it
}

// Actually a trigger - validates when Email changes
private void ValidateEmail()
{
    IsEmailValid = Email.Contains('@');  // Should react to Email changes
}
```

Auto-detection cannot distinguish these cases.

### Additional Reasons for Explicit Triggers

1. **Circular Prevention**: The generator analyzes `[ObservableTrigger]` to detect circular references (RXBG031)
   ```csharp
   // Generator catches this error
   [ObservableTrigger(nameof(UpdateA))]
   public partial int A { get; set; }

   private void UpdateA() { A++; }  // RXBG031: Circular trigger!
   ```

2. **Parametrized Triggers**: Cannot be expressed without attributes
   ```csharp
   [ObservableTrigger<string>(nameof(Log), "Value changed")]
   public partial int Value { get; set; }
   ```

3. **Conditional Guards**: Explicit `canTrigger` methods
   ```csharp
   [ObservableTriggerAsync(nameof(SaveAsync), nameof(CanSave))]
   public partial string Data { get; set; }
   ```

4. **Documentation**: The attribute documents the reactive relationship explicitly

---

## Summary: Pattern Selection Flowchart

Use this detailed flowchart to select the correct pattern:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        PATTERN SELECTION FLOWCHART                          │
└─────────────────────────────────────────────────────────────────────────────┘

START: What do you need?
        │
        ▼
┌───────────────────────────────────────┐
│ React to property changes?            │
└───────────────────────────────────────┘
        │
        ├─YES──▶ Whose property is changing?
        │               │
        │               ├─── MY OWN property (this.Name)
        │               │           │
        │               │           ▼
        │               │    ┌─────────────────────────────────┐
        │               │    │ Use [ObservableTrigger]         │
        │               │    │ (EXPLICIT - cannot auto-detect) │
        │               │    │                                 │
        │               │    │ • Validation logic              │
        │               │    │ • Computed property updates     │
        │               │    │ • Same-model side effects       │
        │               │    └─────────────────────────────────┘
        │               │
        │               ├─── INJECTED MODEL's property (Settings.Theme)
        │               │           │
        │               │           ▼
        │               │    ┌─────────────────────────────────┐
        │               │    │ Internal Observer               │
        │               │    │ (AUTO-DETECTED)                 │
        │               │    │                                 │
        │               │    │ Just write a private method     │
        │               │    │ that accesses the property:     │
        │               │    │                                 │
        │               │    │ private void OnThemeChanged()   │
        │               │    │ {                               │
        │               │    │     if (Settings.Theme == "Dark")│
        │               │    │         ApplyDarkMode();        │
        │               │    │ }                               │
        │               │    └─────────────────────────────────┘
        │               │
        │               ├─── Need UI/COMPONENT notification
        │               │           │
        │               │           ▼
        │               │    ┌─────────────────────────────────┐
        │               │    │ Use [ObservableComponentTrigger]│
        │               │    │                                 │
        │               │    │ • JS interop                    │
        │               │    │ • DOM manipulation              │
        │               │    │ • Component-level effects       │
        │               │    └─────────────────────────────────┘
        │               │
        │               └─── EXTERNAL SERVICE needs notification
        │                           │
        │                           ▼
        │                    ┌─────────────────────────────────┐
        │                    │ Use [ObservableModelObserver]   │
        │                    │ (on service method)             │
        │                    │                                 │
        │                    │ • Logging service               │
        │                    │ • Analytics                     │
        │                    │ • Cross-cutting concerns        │
        │                    └─────────────────────────────────┘
        │
        ├─NO───▶ Need user action binding (button click)?
        │               │
        │               ├─YES──▶ Should it auto-execute on property change?
        │               │               │
        │               │               ├─YES──▶ [ObservableCommand] + [ObservableCommandTrigger]
        │               │               │
        │               │               └─NO───▶ [ObservableCommand] only
        │               │
        │               └─NO───▶ Need to share state between models?
        │                               │
        │                               ├─YES──▶ Partial constructor injection
        │                               │
        │                               └─NO───▶ Just use partial properties
        │
        └─────────────────────────────────────────────────────────────────────

```

### Quick Decision Table: Property Change Reactions

| Property Location | Detection | Pattern | Example |
|-------------------|-----------|---------|---------|
| `this.Email` (own property) | **EXPLICIT** | `[ObservableTrigger]` | `[ObservableTrigger(nameof(Validate))]` |
| `Settings.Theme` (injected model) | **AUTO** | Internal Observer | Just access it in private method |
| Any model (from service) | **EXPLICIT** | `[ObservableModelObserver]` | `[ObservableModelObserver(nameof(Model.Prop))]` |

### Why This Design?

```
┌────────────────────────────────────────────────────────────────────────────┐
│ SAME MODEL (this.*)           │ INJECTED MODEL (Other.*)                   │
├───────────────────────────────┼────────────────────────────────────────────┤
│ Most methods READ properties  │ Methods only access injected model when    │
│ without intending to react    │ they need to REACT to its changes          │
│                               │                                            │
│ Example:                      │ Example:                                   │
│ SendEmail() uses this.Email   │ UpdateTimer() checks Settings.AutoRefresh  │
│ → NOT a trigger               │ → IS an observer (why else access it?)     │
│                               │                                            │
│ ValidateEmail() validates it  │                                            │
│ → IS a trigger                │                                            │
│                               │                                            │
│ ❌ Cannot auto-detect intent  │ ✅ Can auto-detect (clear intent)          │
│ ✅ Use [ObservableTrigger]    │ ✅ Auto-detected private methods           │
└───────────────────────────────┴────────────────────────────────────────────┘
```

---

## Version Information

- **RxBlazorV2**: 1.0.4+
- **.NET**: 9.0+
- **R3**: 1.3.0+
- **Document Version**: 1.0.0

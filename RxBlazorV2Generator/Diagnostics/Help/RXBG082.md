# RXBG082: Internal Model Observer Has Invalid Signature

## Description

This warning is reported when a method accesses properties from an injected ObservableModel reference but has an invalid signature for auto-detection as an internal observer. The generator can auto-detect methods that observe referenced model properties, but only if they follow specific signature patterns.

## Cause

This warning occurs when a method:
- Accesses properties from an injected ObservableModel reference (e.g., `Settings.Counter`)
- Has a naming pattern that suggests observer intent (e.g., `On*Changed`, `Handle*`, `*Observer`)
- Does NOT follow the required signature for auto-detection

Invalid signatures include:
- Public methods (must be private)
- Methods with return values other than `void`, `Task`, or `ValueTask`
- Sync methods with parameters
- Async methods with parameters other than `CancellationToken`

## Valid Method Signatures

### Sync Methods

Sync internal observer methods must:
- Be `private`
- Return `void`
- Have no parameters

```csharp
// Valid sync signature
private void OnCounterChanged()
{
    var count = Settings.Counter;
    // React to counter change
}
```

### Async Methods

Async internal observer methods must:
- Be `private`
- Return `Task` or `ValueTask`
- Have no parameters, or exactly one `CancellationToken` parameter

```csharp
// Valid async signature (without CancellationToken)
private async Task OnDataChangedAsync()
{
    await ProcessDataAsync(Settings.Data);
}

// Valid async signature (with CancellationToken)
private async Task OnDataChangedAsync(CancellationToken ct)
{
    await ProcessDataAsync(Settings.Data, ct);
}
```

## How to Fix

Change the method signature to match one of the valid patterns.

### Fix 1: Change Visibility to Private

```csharp
// WRONG - Public method
public void OnCounterChanged()
{
    var _ = Settings.Counter;
}

// CORRECT - Private method
private void OnCounterChanged()
{
    var _ = Settings.Counter;
}
```

### Fix 2: Change Return Type to Void (Sync)

```csharp
// WRONG - Returns value
private bool OnIsActiveChanged()
{
    return Settings.IsActive;
}

// CORRECT - Returns void
private void OnIsActiveChanged()
{
    if (Settings.IsActive)
    {
        // Handle active state
    }
}
```

### Fix 3: Remove Parameters (Sync)

```csharp
// WRONG - Has parameters
private void OnThemeChanged(string context)
{
    Console.WriteLine($"Theme: {Settings.Theme}, Context: {context}");
}

// CORRECT - No parameters
private void OnThemeChanged()
{
    Console.WriteLine($"Theme changed to: {Settings.Theme}");
}
```

### Fix 4: Fix CancellationToken Parameter (Async)

```csharp
// WRONG - Wrong parameter type
private async Task OnValueChangedAsync(string notACancellationToken)
{
    await Task.Delay(100);
    var _ = Settings.Value;
}

// CORRECT - Use CancellationToken
private async Task OnValueChangedAsync(CancellationToken ct)
{
    await Task.Delay(100, ct);
    var _ = Settings.Value;
}
```

### Fix 5: Ignore the Warning

If this method is not intended to be an internal observer (i.e., it accesses referenced model properties for other purposes), you can safely ignore this warning. The method will work normally but won't be auto-subscribed to property changes.

```csharp
// Not an observer - just a utility method
// Warning can be suppressed if needed
#pragma warning disable RXBG082
private bool CheckAndLogStatus(string prefix)
{
    var status = Timer.IsRunning ? "Running" : "Stopped";
    Console.WriteLine($"{prefix}: {status}");
    return Timer.IsRunning;
}
#pragma warning restore RXBG082
```

## Complete Example

```csharp
[ObservableModelScope(ModelScope.Scoped)]
public partial class TimerSettingsModel : ObservableModel
{
    public partial int TickCount { get; set; }
    public partial bool IsRunning { get; set; }
    public partial string Theme { get; set; }
}

[ObservableModelScope(ModelScope.Scoped)]
public partial class DashboardModel : ObservableModel
{
    public partial string StatusMessage { get; set; } = "";

    // Inject referenced model
    public partial DashboardModel(TimerSettingsModel timerSettings);

    // Valid sync observer - auto-detected
    private void OnTickCountChanged()
    {
        StatusMessage = $"Tick: {TimerSettings.TickCount}";
    }

    // Valid sync observer - auto-detected
    private void OnTimerStateChanged()
    {
        var state = TimerSettings.IsRunning ? "STARTED" : "STOPPED";
        StatusMessage = $"Timer {state}";
    }

    // Valid async observer with cancellation - auto-detected
    private async Task OnThemeChangedAsync(CancellationToken ct)
    {
        await Task.Delay(100, ct);
        StatusMessage = $"Theme is now: {TimerSettings.Theme}";
    }
}
```

## Generated Code

For valid internal observer methods, the generator creates subscriptions in the constructor:

```csharp
public partial class DashboardModel
{
    public DashboardModel(TimerSettingsModel timerSettings)
    {
        TimerSettings = timerSettings;

        // Sync observer subscription
        Subscriptions.Add(TimerSettings.Observable
            .Where(p => p.Intersect(["Model.TickCount"]).Any())
            .Subscribe(_ => OnTickCountChanged()));

        // Sync observer subscription
        Subscriptions.Add(TimerSettings.Observable
            .Where(p => p.Intersect(["Model.IsRunning"]).Any())
            .Subscribe(_ => OnTimerStateChanged()));

        // Async observer subscription with cancellation
        Subscriptions.Add(TimerSettings.Observable
            .Where(p => p.Intersect(["Model.Theme"]).Any())
            .SubscribeAwait(async (_, ct) =>
            {
                await OnThemeChangedAsync(ct);
            }, AwaitOperation.Switch));
    }
}
```

## Summary Table

| Pattern | Visibility | Return Type | Parameters | Valid |
|---------|------------|-------------|------------|-------|
| Sync | `private` | `void` | none | Yes |
| Sync | `public` | `void` | none | No |
| Sync | `private` | `bool` | none | No |
| Sync | `private` | `void` | `(string arg)` | No |
| Async | `private` | `Task` | none | Yes |
| Async | `private` | `Task` | `(CancellationToken ct)` | Yes |
| Async | `private` | `Task` | `(string arg)` | No |
| Async | `public` | `Task` | `(CancellationToken ct)` | No |

## Severity

**Warning** - This diagnostic does not block code generation. The method will not be auto-subscribed as an internal observer, but will otherwise work normally.

## Related Diagnostics

- RXBG080: ObservableModelObserver method has invalid signature (external observers)
- RXBG081: ObservableModelObserver references non-existent property
- RXBG031: Circular trigger reference detected

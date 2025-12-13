# RXBG090: Direct Access to Observable Property

## Description

This diagnostic is reported when the `Observable` property is accessed directly in user code. Direct access to `Observable` bypasses the framework's reactive patterns and should be replaced with appropriate attributes like `[ObservableTrigger]`, `[ObservableCommand]`, or `[ObservableComponentTrigger]`.

## Cause

This warning occurs when:
- Code directly accesses `this.Observable`, `Model.Observable`, or `someModel.Observable`
- The access is in user code, not generated code (`.g.cs` files)
- The code is not in a test project

## How to Fix

Replace direct Observable access with the appropriate attribute-based pattern:

### Option 1: Use [ObservableTrigger] for Property Reactions

```csharp
// Before (triggers warning)
Subscriptions.Add(Observable
    .Where(p => p.Intersect(["Model.Name"]).Any())
    .Subscribe(_ => ValidateName()));

// After (no warning)
[ObservableTrigger(nameof(ValidateName))]
public partial string Name { get; set; }

private void ValidateName() { /* validation logic */ }
```

### Option 2: Use [ObservableCommand] with Triggers

```csharp
// Before (triggers warning)
Subscriptions.Add(Observable
    .Where(p => p.Intersect(["Model.Count"]).Any())
    .Subscribe(_ => _refreshCommand.Execute()));

// After (no warning)
[ObservableCommand(nameof(DoRefresh))]
[ObservableCommandTrigger(nameof(Count))]
public partial IObservableCommand RefreshCommand { get; }

public partial int Count { get; set; }
private void DoRefresh() { /* refresh logic */ }
```

### Option 3: Use [ObservableComponentTrigger] for Component Hooks

```csharp
// Before (triggers warning in component)
Model.Observable
    .Where(p => p.Intersect(["Model.Theme"]).Any())
    .Subscribe(_ => ApplyTheme());

// After (no warning)
[ObservableComponent]
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial string Theme { get; set; }
}

// In component override:
protected override void OnThemeChanged()
{
    ApplyTheme();
}
```

### Option 4: Use Internal Model Observers (Auto-Detected)

```csharp
// Before (triggers warning)
Subscriptions.Add(Settings.Observable
    .Where(p => p.Intersect(["Model.AutoRefresh"]).Any())
    .Subscribe(_ => UpdateTimer()));

// After (no warning) - auto-detected observer
public partial class MyModel : ObservableModel
{
    public partial MyModel(SettingsModel settings);

    // This method is auto-detected as an observer because it accesses Settings.AutoRefresh
    private void UpdateTimer()
    {
        if (Settings.AutoRefresh)
        {
            // Timer logic...
        }
    }
}
```

## Examples

### Example 1: Direct Observable Access (Incorrect)

```csharp
// Warning: Direct access to 'Observable' property in 'method OnInitialized'
public partial class MyModel : ObservableModel
{
    protected override void OnInitialized()
    {
        Subscriptions.Add(Observable.Subscribe(props => HandleChange(props)));
    }
}
```

### Example 2: Using [ObservableTrigger] (Correct)

```csharp
// No warning - uses attribute-based pattern
public partial class MyModel : ObservableModel
{
    [ObservableTrigger(nameof(HandleNameChange))]
    public partial string Name { get; set; }

    private void HandleNameChange()
    {
        // Handle name change
    }
}
```

### Example 3: Async Trigger (Correct)

```csharp
// No warning - uses async trigger pattern
public partial class MyModel : ObservableModel
{
    [ObservableTriggerAsync(nameof(SaveAsync))]
    public partial string Data { get; set; }

    private async Task SaveAsync(CancellationToken ct)
    {
        await _repository.SaveAsync(Data, ct);
    }
}
```

### Example 4: External Model Observer (Correct)

```csharp
// No warning - uses [ObservableModelObserver] attribute
public class LoggingService
{
    [ObservableModelObserver(nameof(UserModel.Username))]
    public void OnUsernameChanged(UserModel model)
    {
        _logger.Log($"Username changed to: {model.Username}");
    }
}

public partial class UserModel : ObservableModel
{
    public partial UserModel(LoggingService logger);

    public partial string Username { get; set; }
}
```

## Why Direct Observable Access is Discouraged

1. **Bypasses Framework Patterns**: The RxBlazorV2 framework provides declarative attributes for reactive patterns. Direct Observable access circumvents these patterns and loses the benefits of code generation.

2. **Error-Prone**: Manual subscriptions are easy to get wrong:
   - Forgetting to add to `Subscriptions` causes memory leaks
   - Incorrect property name strings cause silent failures
   - Missing `.Where()` filters cause unnecessary updates

3. **AI Misuse Prevention**: AI-driven implementations tend to misunderstand reactive patterns and use Observable directly instead of the correct attribute-based approach.

4. **Maintainability**: Attribute-based patterns are declarative and easier to understand. Direct Observable subscriptions require understanding R3 reactive programming.

## When Observable Access is Legitimate

The analyzer skips:
- Generated code (`.g.cs` files)
- Test projects (paths containing "Tests" or "Test")
- Files in `obj/` directories

Generated code legitimately uses Observable for:
- Command trigger subscriptions
- Property trigger subscriptions
- Component change detection
- Model reference subscriptions

## Code Fix Available

No automatic code fix is available because the replacement pattern depends on the use case. Choose the appropriate attribute-based pattern based on your needs:

| Use Case | Attribute |
|----------|-----------|
| React to property changes in same model | `[ObservableTrigger]` / `[ObservableTriggerAsync]` |
| Trigger command on property changes | `[ObservableCommandTrigger]` |
| Component hook for property changes | `[ObservableComponentTrigger]` / `[ObservableComponentTriggerAsync]` |
| External service observer | `[ObservableModelObserver]` |
| React to referenced model properties | Internal observer (auto-detected) |

## Related Diagnostics

- RXBG041: ObservableComponentTrigger attribute has no effect
- RXBG080: ObservableModelObserver method has invalid signature
- RXBG082: Internal model observer has invalid signature

## Best Practices

- Always prefer attribute-based patterns over direct Observable access
- Use `[ObservableTrigger]` for simple property change reactions
- Use `[ObservableComponentTrigger]` for component-level hooks
- Use `[ObservableModelObserver]` for cross-service observations
- Let the generator handle subscription management
- Trust the framework to manage reactive subscriptions correctly

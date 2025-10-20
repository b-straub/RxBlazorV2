# RXBG012: Referenced Model Has No Used Properties

## Description

This diagnostic is reported when a model references another ObservableModel via a partial constructor parameter but never actually uses any properties from the referenced model. This indicates unnecessary coupling and should be cleaned up.

## Cause

This error occurs when:
- A partial constructor parameter of type `ObservableModel` is present
- No properties from the referenced model are accessed in any methods or properties
- The reference was added but is no longer needed
- **Exception**: The reference counts as USED if both conditions are met:
  - Parent model has `[ObservableComponent(includeReferencedTriggers: true)]` (default)
  - Referenced model has properties with `[ObservableComponentTrigger]` or `[ObservableComponentTriggerAsync]`

## How to Fix

Use the available code fix:
- **Remove constructor parameter** - Removes the unused parameter from the partial constructor

Alternatively, if the reference is needed:
- Add code that actually uses properties from the referenced model
- Add `[ObservableComponentTrigger]` attributes to properties in the referenced model (if using `[ObservableComponent]`)

## Examples

### Example 1: Unused Reference

```csharp
// ❌ WRONG - CounterModel is referenced but never used
[ObservableModelScope(ModelScope.Transient)]
public partial class CounterModel : ObservableModel
{
    public partial int Counter1 { get; set; }
    public partial int Counter2 { get; set; }
}

[ObservableModelScope(ModelScope.Transient)]
public partial class ParentModel : ObservableModel
{
    // Error: RXBG012 - counter parameter is never used
    public partial ParentModel(CounterModel counter);

    public partial int Value { get; set; }

    public void DoSomething()
    {
        Value = 42;  // Only uses own properties, never accesses Counter
    }
}
```

### Example 2: Fix by Removing Parameter

```csharp
// ✅ CORRECT - Removed unused reference
[ObservableModelScope(ModelScope.Transient)]
public partial class CounterModel : ObservableModel
{
    public partial int Counter1 { get; set; }
    public partial int Counter2 { get; set; }
}

[ObservableModelScope(ModelScope.Transient)]
public partial class ParentModel : ObservableModel
{
    // Constructor parameter removed

    public partial int Value { get; set; }

    public void DoSomething()
    {
        Value = 42;
    }
}
```

### Example 3: Fix by Using Properties

```csharp
// ✅ CORRECT - Now actually uses the referenced model
[ObservableModelScope(ModelScope.Transient)]
public partial class CounterModel : ObservableModel
{
    public partial int Counter1 { get; set; }
    public partial int Counter2 { get; set; }
}

[ObservableModelScope(ModelScope.Transient)]
public partial class ParentModel : ObservableModel
{
    public partial ParentModel(CounterModel counter);

    public partial int Value { get; set; }

    // Now uses Counter1 from Counter property (generated from constructor parameter)
    public int Total => Value + Counter.Counter1;
}
```

### Example 4: Using in Command Methods

```csharp
// ✅ CORRECT - Uses referenced model in command method
[ObservableModelScope(ModelScope.Transient)]
public partial class CounterModel : ObservableModel
{
    public partial int Counter1 { get; set; }
}

[ObservableModelScope(ModelScope.Transient)]
public partial class ParentModel : ObservableModel
{
    public partial ParentModel(CounterModel counter);

    public partial int Value { get; set; }

    [ObservableCommand(nameof(Execute))]
    public partial IObservableCommand TestCommand { get; }

    private void Execute()
    {
        Value = Counter.Counter1 * 2;  // Uses Counter1 via Counter property
    }
}
```

### Example 5: Trigger-Only Reference (No Error)

```csharp
// ✅ CORRECT - Reference counts as USED via component triggers
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial bool IsDay { get; set; }

    [ObservableComponentTriggerAsync]
    public partial string Theme { get; set; }
}

[ObservableComponent]  // includeReferencedTriggers: true by default
public partial class WeatherModel : ObservableModel
{
    // No RXBG012 error: Settings reference is USED because SettingsModel
    // has ComponentTrigger attributes and WeatherModel has [ObservableComponent]
    // This generates OnSettingsIsDayChanged() and OnSettingsThemeChangedAsync() hooks
    public partial WeatherModel(SettingsModel settings);

    public partial string Temperature { get; set; }
}
```

### Example 6: Trigger-Only Reference with Disabled Feature (Error)

```csharp
// ❌ WRONG - includeReferencedTriggers: false makes reference unused
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial bool IsDay { get; set; }
}

[ObservableComponent(includeReferencedTriggers: false)]  // Disabled
public partial class WeatherModel : ObservableModel
{
    // Error: RXBG012 - settings is unused because:
    // - No properties accessed in code
    // - includeReferencedTriggers is disabled, so triggers don't count
    public partial WeatherModel(SettingsModel settings);

    public partial string Temperature { get; set; }
}
```

## Code Fixes Available

- **Remove constructor parameter**: Removes the unused ObservableModel parameter from the partial constructor

## Related Diagnostics

- RXBG011: Invalid model reference target (referenced type doesn't inherit from ObservableModel)
- RXBG013: Derived model reference error
- RXBG052: Referenced model with triggers must be in same assembly

## See Also

- **includeReferencedTriggers Feature**: See `[ObservableComponent]` attribute documentation
- **Component Trigger Hooks**: See `[ObservableComponentTrigger]` and `[ObservableComponentTriggerAsync]` attribute documentation

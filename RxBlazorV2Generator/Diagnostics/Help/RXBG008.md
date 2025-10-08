# RXBG008: Referenced Model Has No Used Properties

## Description

This diagnostic is reported when a model declares an `ObservableModelReference` but never actually uses any properties from the referenced model. This indicates unnecessary coupling and should be cleaned up.

## Cause

This error occurs when:
- An `ObservableModelReference<T>` attribute is present
- No properties from the referenced model are accessed in any methods or properties
- The reference was added but is no longer needed

## How to Fix

Use the available code fix:
- **Remove unused ObservableModelReference attribute** - Removes the unused attribute

Alternatively, if the reference is needed:
- Add code that actually uses properties from the referenced model

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
[ObservableModelReference<CounterModel>]  // Error: Properties never used
public partial class ParentModel : ObservableModel
{
    public partial int Value { get; set; }

    public void DoSomething()
    {
        Value = 42;  // Only uses own properties
    }
}
```

### Example 2: Fix by Removing Attribute

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
[ObservableModelReference<CounterModel>]
public partial class ParentModel : ObservableModel
{
    public partial int Value { get; set; }

    // Now uses Counter1 from CounterModel
    public int Total => Value + CounterModel.Counter1;
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
[ObservableModelReference<CounterModel>]
public partial class ParentModel : ObservableModel
{
    public partial int Value { get; set; }

    [ObservableCommand(nameof(Execute))]
    public partial IObservableCommand TestCommand { get; }

    private void Execute()
    {
        Value = CounterModel.Counter1 * 2;  // Uses Counter1
    }
}
```

## Code Fixes Available

- **Remove unused ObservableModelReference attribute**: Removes the attribute

## Related Diagnostics

- RXBG006: Circular model reference
- RXBG007: Invalid model reference target

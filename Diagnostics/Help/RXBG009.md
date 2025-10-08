# RXBG009: Component Contains ObservableModel Fields But Does Not Inherit From ObservableComponent

## Description

This diagnostic is reported when a Blazor component contains `ObservableModel` fields but doesn't inherit from `ObservableComponent<T>` or `LayoutComponentBase`. Components using observable models need proper subscription management to enable reactive binding.

## Cause

This error occurs when:
- A component inherits from `ComponentBase` (or similar)
- The component has fields/properties of type `ObservableModel`
- The component doesn't inherit from `ObservableComponent<T>` or `LayoutComponentBase`

## How to Fix

Use one of the available code fixes:
1. **Change to ObservableComponent<T>** - Changes the base class to `ObservableComponent<T>` for reactive binding
2. **Remove ObservableModel attributes** - Removes all observable model-related attributes

## Examples

### Example 1: Invalid ComponentBase Inheritance

```csharp
// ❌ WRONG - Uses ComponentBase with ObservableModel
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public partial string Name { get; set; }
}

[ObservableModelScope(ModelScope.Singleton)]  // Error: Should use ObservableComponent
public partial class TestComponent : ComponentBase
{
    protected TestModel Model { get; set; }
}
```

### Example 2: Fix by Using ObservableComponent

```csharp
// ✅ CORRECT - Uses ObservableComponent<T>
using RxBlazorV2.Component;

[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public partial string Name { get; set; }
}

[ObservableModelScope(ModelScope.Singleton)]
public partial class TestComponent : ObservableComponent<TestModel>
{
    protected TestModel Model { get; set; }
}
```

### Example 3: Fix by Removing Attributes

```csharp
// ✅ CORRECT - Removed observable model attributes
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public partial string Name { get; set; }
}

public partial class TestComponent : ComponentBase
{
    protected TestModel Model { get; set; }
}
```

### Example 4: Valid LayoutComponentBase

```csharp
// ✅ CORRECT - LayoutComponentBase is allowed
public partial class MainLayout : LayoutComponentBase
{
    protected TestModel Model { get; set; }
}
```

## Why This Matters

`ObservableComponent<T>` provides:
- Automatic subscription management
- Reactive binding to observable model properties
- Proper disposal of subscriptions
- Automatic UI updates when model properties change

Without it, your component won't automatically update when the observable model changes.

## Code Fixes Available

- **Change to ObservableComponent<T>**: Updates inheritance and adds necessary using
- **Remove all ObservableModel attributes**: Removes observable model-related attributes

## Related Diagnostics

- RXBG010: Shared model not singleton

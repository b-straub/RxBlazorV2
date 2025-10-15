# RXBG014: ObservableModel Used By Multiple Components Must Have Singleton Scope

## Description

This diagnostic is reported when an `ObservableModel` is used by multiple `ObservableComponent` instances but is not registered with `Singleton` scope. This can cause data inconsistency issues as each component instance would get its own model instance instead of sharing the same state.

## Cause

This error occurs when:
- Multiple different `ObservableComponent` classes use the same model
- The model has `Scoped` or `Transient` scope
- The model needs to maintain shared state across components

## How to Fix

Use the available code fix:
- **Change scope to Singleton** - Updates the `ObservableModelScope` attribute to `Singleton`

Or manually:
- Change the scope to `ModelScope.Singleton`
- Remove the scope attribute entirely (Singleton is the default)

## Examples

### Example 1: Invalid Scoped Model

```csharp
// ❌ WRONG - Scoped model used by multiple components
[ObservableModelScope(ModelScope.Scoped)]  // Error: Should be Singleton
public partial class TestModel : ObservableModel
{
    public partial string Name { get; set; }
}

public partial class TestComponent1 : ObservableComponent<TestModel>
{
}

public partial class TestComponent2 : ObservableComponent<TestModel>
{
}
```

### Example 2: Fix by Using Singleton

```csharp
// ✅ CORRECT - Singleton model shared by multiple components
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public partial string Name { get; set; }
}

public partial class TestComponent1 : ObservableComponent<TestModel>
{
}

public partial class TestComponent2 : ObservableComponent<TestModel>
{
}
```

### Example 3: Default Scope (Singleton)

```csharp
// ✅ CORRECT - No scope attribute defaults to Singleton
public partial class TestModel : ObservableModel
{
    public partial string Name { get; set; }
}

public partial class TestComponent1 : ObservableComponent<TestModel>
{
}

public partial class TestComponent2 : ObservableComponent<TestModel>
{
}
```

### Example 4: Single Component with Scoped

```csharp
// ✅ CORRECT - Scoped is OK when used by only one component type
[ObservableModelScope(ModelScope.Scoped)]
public partial class TestModel : ObservableModel
{
    public partial string Name { get; set; }
}

public partial class TestComponent : ObservableComponent<TestModel>
{
}
```

## Why This Matters

When multiple components share a model:
- **Singleton**: All components see the same data and state (correct for shared state)
- **Scoped**: Each component gets its own instance within the same scope (can cause inconsistency)
- **Transient**: Each component gets a new instance every time (causes data duplication)

For shared application state (like user settings, navigation state, etc.), you need Singleton scope.

## Code Fixes Available

- **Change scope to Singleton**: Updates the `ObservableModelScope` attribute

## Related Diagnostics

- RXBG009: Component inheritance error

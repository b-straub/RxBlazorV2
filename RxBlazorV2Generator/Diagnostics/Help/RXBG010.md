# RXBG010: Circular Model Reference Detected

## Description

This diagnostic is reported when a circular reference is detected between `ObservableModel` classes. Circular references can cause infinite loops during initialization and are not allowed.

## Cause

Circular references occur when:
- Model A references Model B, and Model B references Model A
- A model references itself
- A chain of references forms a cycle (A → B → C → A)

## How to Fix

Use one of the available code fixes:
1. **Remove this circular model reference** - Removes only the attribute at the current location
2. **Remove all circular model references** - Removes all attributes involved in the circular reference

## Examples

### Example 1: Simple Circular Reference

```csharp
// ❌ WRONG - Circular reference
[ObservableModelScope(ModelScope.Singleton)]
[ObservableModelReference(typeof(ModelB))]  // Error: Circular reference
public partial class ModelA : ObservableModel
{
}

[ObservableModelScope(ModelScope.Singleton)]
[ObservableModelReference(typeof(ModelA))]  // Error: Circular reference
public partial class ModelB : ObservableModel
{
}

// ✅ CORRECT - Remove one reference to break the cycle
[ObservableModelScope(ModelScope.Singleton)]
public partial class ModelA : ObservableModel
{
}

[ObservableModelScope(ModelScope.Singleton)]
[ObservableModelReference<ModelA>]
public partial class ModelB : ObservableModel
{
    public int GetValue() => ModelA.SomeProperty;
}
```

### Example 2: Self-Reference

```csharp
// ❌ WRONG - Model references itself
[ObservableModelScope(ModelScope.Singleton)]
[ObservableModelReference(typeof(ModelA))]  // Error: Self-reference
public partial class ModelA : ObservableModel
{
}

// ✅ CORRECT - Remove self-reference
[ObservableModelScope(ModelScope.Singleton)]
public partial class ModelA : ObservableModel
{
}
```

### Example 3: Valid Chain (No Cycle)

```csharp
// ✅ CORRECT - No circular reference
[ObservableModelScope(ModelScope.Singleton)]
[ObservableModelReference<ModelB>]
public partial class ModelA : ObservableModel
{
    public int GetProp() => ModelB.Prop;
}

[ObservableModelScope(ModelScope.Singleton)]
[ObservableModelReference<ModelC>]
public partial class ModelB : ObservableModel
{
    public partial int Prop { get; set; }
    public int GetProp() => ModelC.Prop;
}

[ObservableModelScope(ModelScope.Singleton)]
public partial class ModelC : ObservableModel
{
    public partial int Prop { get; set; }
}
```

## Code Fixes Available

- **Remove this circular model reference**: Removes the current attribute
- **Remove all circular model references**: Removes all attributes in the circular chain

## Related Diagnostics

- RXBG030: Invalid model reference target
- RXBG031: Unused model reference

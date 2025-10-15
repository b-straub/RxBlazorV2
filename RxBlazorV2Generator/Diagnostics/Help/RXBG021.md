# RXBG021: Type Constraint Mismatch

## Description

This diagnostic is reported when type parameters in a referenced generic type have constraints that are incompatible with the corresponding type parameters in the referencing class. For proper type substitution and safety, constraints must be compatible.

## Cause

This error occurs when:
- A generic model references another generic model using open generic syntax
- The type parameters have different or incompatible constraints
- For example: referenced type requires `class` but referencing type has `struct`

## How to Fix

Use one of the available code fixes:
- **Adjust type parameters to match referenced type** - Updates constraints to match
- **Remove reference** - Removes the incompatible reference

Or manually:
- Ensure type parameter constraints match between referenced and referencing types
- Make constraints compatible (same or more restrictive)

## Examples

### Example 1: Class vs Struct Constraint

```csharp
// ❌ WRONG - GenericModel requires class, ConsumerModel has struct
[ObservableModelScope(ModelScope.Singleton)]
public partial class GenericModel<T> : ObservableModel where T : class
{
    public partial T Item { get; set; }
}

[ObservableModelReference(typeof(GenericModel<>))]  // Error: Constraint mismatch
[ObservableModelScope(ModelScope.Scoped)]
public partial class ConsumerModel<T> : ObservableModel where T : struct
{
    public partial int Value { get; set; }
}
```

### Example 2: Fix by Matching Constraints

```csharp
// ✅ CORRECT - Both use class constraint
[ObservableModelScope(ModelScope.Singleton)]
public partial class GenericModel<T> : ObservableModel where T : class
{
    public partial T Item { get; set; }
}

[ObservableModelReference(typeof(GenericModel<>))]
[ObservableModelScope(ModelScope.Scoped)]
public partial class ConsumerModel<T> : ObservableModel where T : class
{
    public partial int Value { get; set; }

    public T GetProp()
    {
        return GenericModel.Item;
    }
}
```

### Example 3: Interface Constraint Mismatch

```csharp
// ❌ WRONG - Different interface constraints
[ObservableModelScope(ModelScope.Singleton)]
public partial class GenericModel<T> : ObservableModel where T : IDisposable
{
    public partial T Item { get; set; }
}

[ObservableModelReference(typeof(GenericModel<>))]  // Error: Constraint mismatch
[ObservableModelScope(ModelScope.Scoped)]
public partial class ConsumerModel<T> : ObservableModel where T : class
{
    public partial int Value { get; set; }
}
```

### Example 4: Fix by Matching Interface Constraint

```csharp
// ✅ CORRECT - Both use IDisposable constraint
[ObservableModelScope(ModelScope.Singleton)]
public partial class GenericModel<T> : ObservableModel where T : IDisposable
{
    public partial T Item { get; set; }
}

[ObservableModelReference(typeof(GenericModel<>))]
[ObservableModelScope(ModelScope.Scoped)]
public partial class ConsumerModel<T> : ObservableModel where T : IDisposable
{
    public partial int Value { get; set; }

    public T GetItem() => GenericModel.Item;
}
```

### Example 5: More Restrictive Constraints

```csharp
// ❌ WRONG - ConsumerModel has more restrictive constraints
// Note: More restrictive constraints are not currently supported
[ObservableModelScope(ModelScope.Singleton)]
public partial class GenericModel<T> : ObservableModel where T : class
{
    public partial T Item { get; set; }
}

[ObservableModelReference(typeof(GenericModel<>))]  // Error: More restrictive
[ObservableModelScope(ModelScope.Scoped)]
public partial class ConsumerModel<T> : ObservableModel
    where T : class, IDisposable
{
    public partial int Value { get; set; }
}
```

### Example 6: Valid Same Constraints

```csharp
// ✅ CORRECT - Identical constraints
[ObservableModelScope(ModelScope.Singleton)]
public partial class GenericModel<T> : ObservableModel where T : class
{
    public partial T Item { get; set; }
}

[ObservableModelReference(typeof(GenericModel<>))]
[ObservableModelScope(ModelScope.Scoped)]
public partial class ConsumerModel<T> : ObservableModel where T : class
{
    public partial int Value { get; set; }

    public T GetProp()
    {
        return GenericModel.Item;
    }
}
```

## Common Constraint Types

- `where T : class` - Reference type constraint
- `where T : struct` - Value type constraint
- `where T : new()` - Constructor constraint
- `where T : BaseClass` - Base class constraint
- `where T : IInterface` - Interface constraint
- `where T : class, IInterface` - Multiple constraints

## Why This Matters

Matching type constraints ensures:
- Type safety during compilation
- Correct type parameter substitution
- Valid property access in generated code
- Predictable generic behavior

## Code Fixes Available

- **Adjust type parameters to match referenced type**: Updates constraints
- **Remove reference**: Removes the incompatible reference

## Related Diagnostics

- RXBG050: Generic arity mismatch
- RXBG022: Invalid open generic reference

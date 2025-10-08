# RXBG015: Invalid Open Generic Type Reference

## Description

This diagnostic is reported when an open generic type (like `typeof(Model<>)`) is referenced from a non-generic class. Open generic types can only be referenced from generic classes that can provide the required type parameters.

## Cause

This error occurs when:
- A non-generic class tries to reference an open generic type
- The class doesn't have type parameters to substitute into the generic type
- The reference uses `typeof(Model<>)` or `typeof(Model<,>)` syntax

## How to Fix

Use one of the available code fixes:
- **Adjust type parameters to match referenced type** - Makes the class generic with matching type parameters
- **Remove reference** - Removes the invalid reference

Or manually:
- Make the referencing class generic with compatible type parameters
- Use a closed generic type instead (`typeof(Model<string>)`)
- Remove the reference if not needed

## Examples

### Example 1: Non-Generic Referencing Open Generic

```csharp
// ❌ WRONG - ConsumerModel is not generic but references open generic
[ObservableModelScope(ModelScope.Singleton)]
public partial class GenericModel<T> : ObservableModel where T : class
{
    public partial T Item { get; set; }
}

[ObservableModelReference(typeof(GenericModel<>))]  // Error: Invalid reference
[ObservableModelScope(ModelScope.Scoped)]
public partial class ConsumerModel : ObservableModel
{
    public partial int Value { get; set; }
}
```

### Example 2: Fix by Making Class Generic

```csharp
// ✅ CORRECT - ConsumerModel is now generic
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

### Example 3: Fix by Using Closed Generic

```csharp
// ✅ CORRECT - Use closed generic type with specific type argument
[ObservableModelScope(ModelScope.Singleton)]
public partial class GenericModel<T> : ObservableModel where T : class
{
    public partial T Item { get; set; }
}

[ObservableModelReference(typeof(GenericModel<string>))]  // Closed generic
[ObservableModelScope(ModelScope.Scoped)]
public partial class ConsumerModel : ObservableModel
{
    public partial int Value { get; set; }
}
```

### Example 4: Multiple Type Parameters

```csharp
// ❌ WRONG - Non-generic class referencing open generic with 2 type parameters
[ObservableModelScope(ModelScope.Singleton)]
public partial class GenericModel<T, P> : ObservableModel
    where T : class
    where P : struct
{
    public partial T Item1 { get; set; }
    public partial P Item2 { get; set; }
}

[ObservableModelReference(typeof(GenericModel<,>))]  // Error: Invalid reference
[ObservableModelScope(ModelScope.Scoped)]
public partial class ConsumerModel : ObservableModel
{
    public partial int Value { get; set; }
}
```

### Example 5: Fix with Multiple Type Parameters

```csharp
// ✅ CORRECT - ConsumerModel now has matching type parameters
[ObservableModelScope(ModelScope.Singleton)]
public partial class GenericModel<T, P> : ObservableModel
    where T : class
    where P : struct
{
    public partial T Item1 { get; set; }
    public partial P Item2 { get; set; }
}

[ObservableModelReference(typeof(GenericModel<,>))]
[ObservableModelScope(ModelScope.Scoped)]
public partial class ConsumerModel<T, P> : ObservableModel
    where T : class
    where P : struct
{
    public partial int Value { get; set; }

    public T GetItem1() => GenericModel.Item1;
    public P GetItem2() => GenericModel.Item2;
}
```

### Example 6: Fix by Removing Reference

```csharp
// ✅ CORRECT - Removed the invalid reference
[ObservableModelScope(ModelScope.Singleton)]
public partial class GenericModel<T> : ObservableModel where T : class
{
    public partial T Item { get; set; }
}

[ObservableModelScope(ModelScope.Scoped)]
public partial class ConsumerModel : ObservableModel
{
    public partial int Value { get; set; }
}
```

## Open vs Closed Generic Types

- **Open Generic**: `typeof(Model<>)` or `typeof(Model<,>)` - No specific type arguments
- **Closed Generic**: `typeof(Model<string>)` or `typeof(Model<int, bool>)` - Specific type arguments

## When to Use Each

- **Open Generic Reference**: When you want type parameter substitution from the referencing class
- **Closed Generic Reference**: When you want a specific instantiation of the generic type

## Why This Matters

Open generic references require:
- The referencing class to be generic
- Matching number of type parameters (arity)
- Compatible type constraints

Without these, the generator cannot properly substitute type parameters during code generation.

## Code Fixes Available

- **Adjust type parameters to match referenced type**: Makes the class generic
- **Remove reference**: Removes the invalid reference

## Related Diagnostics

- RXBG013: Generic arity mismatch
- RXBG014: Type constraint mismatch

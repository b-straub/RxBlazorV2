# RXBG013: Generic Type Arity Mismatch

## Description

This diagnostic is reported when an open generic type is referenced with a different number of type parameters than the referencing class. For proper type parameter substitution, the number of type parameters (arity) must match.

## Cause

This error occurs when:
- A generic model references another generic model using open generic syntax (`typeof(Model<>)`)
- The referenced type has a different number of type parameters
- For example: A model with 1 type parameter references a model with 2 type parameters

## How to Fix

Use one of the available code fixes:
- **Adjust type parameters to match referenced type** - Adjusts the class's type parameters
- **Remove reference** - Removes the incompatible reference

Or manually:
- Ensure both classes have the same number of type parameters
- Use closed generic syntax if you don't need type parameter substitution

## Examples

### Example 1: Single to Double Arity Mismatch

```csharp
// ❌ WRONG - GenericModel has 1 type parameter, ConsumerModel has 2
[ObservableModelScope(ModelScope.Singleton)]
public partial class GenericModel<T> : ObservableModel where T : class
{
    public partial T Item { get; set; }
}

[ObservableModelReference(typeof(GenericModel<>))]  // Error: Arity mismatch
[ObservableModelScope(ModelScope.Scoped)]
public partial class ConsumerModel<T, P> : ObservableModel
    where T : class
    where P : struct
{
    public partial int Value { get; set; }
}
```

### Example 2: Fix by Matching Arity

```csharp
// ✅ CORRECT - Both have 1 type parameter
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

### Example 3: Double to Single Arity Mismatch

```csharp
// ❌ WRONG - GenericModel has 2 type parameters, ConsumerModel has 1
[ObservableModelScope(ModelScope.Singleton)]
public partial class GenericModel<T, P> : ObservableModel
    where T : class
    where P : struct
{
    public partial T Item1 { get; set; }
    public partial P Item2 { get; set; }
}

[ObservableModelReference(typeof(GenericModel<,>))]  // Error: Arity mismatch
[ObservableModelScope(ModelScope.Scoped)]
public partial class ConsumerModel<T> : ObservableModel where T : class
{
    public partial int Value { get; set; }
}
```

### Example 4: Fix with Same Arity

```csharp
// ✅ CORRECT - Both have 2 type parameters
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

### Example 5: Using Closed Generic (Alternative)

```csharp
// ✅ CORRECT - Using closed generic type (specific type arguments)
[ObservableModelScope(ModelScope.Singleton)]
public partial class GenericModel<T> : ObservableModel where T : class
{
    public partial T Item { get; set; }
}

[ObservableModelReference(typeof(GenericModel<string>))]  // Closed generic
[ObservableModelScope(ModelScope.Scoped)]
public partial class ConsumerModel<T, P> : ObservableModel
    where T : class
    where P : struct
{
    public partial int Value { get; set; }
}
```

## Why This Matters

Type parameter arity matching ensures:
- Proper type parameter substitution during code generation
- Type safety throughout the application
- Correct property access in generated code
- Predictable generic type behavior

## Code Fixes Available

- **Adjust type parameters to match referenced type**: Updates the class's type parameters
- **Remove reference**: Removes the incompatible reference

## Related Diagnostics

- RXBG014: Type constraint mismatch
- RXBG015: Invalid open generic reference

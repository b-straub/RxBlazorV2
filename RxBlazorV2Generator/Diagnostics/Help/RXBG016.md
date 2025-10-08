# RXBG016: Invalid Init Accessor on Partial Property

## Description

This diagnostic is reported when a partial property uses the `init` accessor but the property type does not implement `IObservableCollection`. The `init` accessor is only valid for IObservableCollection properties where reactivity comes from observing the collection rather than property changes.

## Cause

This error occurs when:
- A partial property uses `{ get; init; }` accessor pattern
- The property type does not implement `IObservableCollection`
- The property needs reactive state change notifications

## How to Fix

Use the available code fix:
- **Convert 'init' to 'set'** - Converts the `init` accessor to `set` while preserving the `required` modifier

Or manually:
- Change `{ get; init; }` to `{ get; set; }`
- Keep the `required` modifier if present

## Examples

### Example 1: Invalid Init Accessor for String

```csharp
// ❌ WRONG - String doesn't implement IObservableCollection
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public required partial string Name { get; init; }  // Error: Invalid init
}
```

### Example 2: Fix with Set Accessor

```csharp
// ✅ CORRECT - Use set accessor for reactive properties
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public required partial string Name { get; set; }  // Fixed: Uses set
}
```

### Example 3: Valid Init for IObservableCollection

```csharp
// ✅ CORRECT - IObservableCollection can use init
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public required partial ObservableList<string> Items { get; init; }  // Valid
}
```

### Example 4: Invalid Init for Value Types

```csharp
// ❌ WRONG - Value types don't implement IObservableCollection
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public partial int Count { get; init; }  // Error: Invalid init
}
```

### Example 5: Fix Preserves Required

```csharp
// ✅ CORRECT - Code fix preserves required modifier
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public required partial string Name { get; set; } = "Default";
}
```

### Example 6: Invalid Init for Generic Type

```csharp
// ❌ WRONG - Generic type T doesn't implement IObservableCollection
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel<T> : ObservableModel where T : new()
{
    public required partial T Value { get; init; } = new();  // Error
}
```

### Example 7: Fix Generic Type

```csharp
// ✅ CORRECT - Use set accessor for generic properties
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel<T> : ObservableModel where T : new()
{
    public required partial T Value { get; set; } = new();  // Fixed
}
```

## Why Init is Invalid

For non-IObservableCollection properties:
- The `set` accessor triggers `StateHasChanged()` for reactive updates
- The `init` accessor is for object initialization only
- Using `init` prevents reactive property change notifications
- The generator cannot properly implement reactive patterns with `init`

For IObservableCollection properties:
- Reactivity comes from observing collection changes
- No property-level `StateHasChanged()` needed
- The `init` accessor is valid for constructor initialization
- Collection modifications trigger notifications automatically

## When Init is Valid

The `init` accessor is only valid when:
- Property type implements `IObservableCollection`
- Examples: `ObservableList<T>`, `ObservableDictionary<K,V>`, etc.
- The collection itself provides reactive change notifications

## Generated Code Patterns

### With Set Accessor (Standard)
```csharp
[UsedImplicitly]
public partial string Name
{
    get => field;
    set
    {
        field = value;
        StateHasChanged(nameof(Name));
    }
}
```

### With Init Accessor (IObservableCollection Only)
```csharp
public required partial ObservableList<string> Items
{
    get => field;
    init => field = value;
}
```

## Code Fix Available

- **Convert 'init' to 'set'**: Converts the `init` accessor to `set` and preserves the `required` modifier

## Related Diagnostics

- RXBG001: Observable model analysis error

## Best Practices

- Use `set` accessor for all reactive partial properties
- Use `init` accessor only for IObservableCollection properties
- Keep `required` modifier when properties must be initialized
- Let the generator handle reactive state notifications

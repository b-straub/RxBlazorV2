# RXBG072: Observable Entity Must Be Declared as Partial

## Description

This error is reported when an observable entity (a class inheriting from `ObservableModel` or a property implementing `IObservableCommand`) is not declared with the `partial` modifier. The source generator cannot generate code for non-partial members.

## Cause

This error occurs when:
- A class inherits from `ObservableModel` but is not declared as `partial`
- A property with `[ObservableCommand]` attribute implementing `IObservableCommand` (or related interfaces) is not declared as `partial`

## How to Fix

Add the `partial` modifier to the class or property declaration:

```csharp
// ❌ WRONG - Class missing partial modifier
public class MyModel : ObservableModel
{
    public partial string Name { get; set; }
}

// ✅ CORRECT - Class with partial modifier
public partial class MyModel : ObservableModel
{
    public partial string Name { get; set; }
}

// ❌ WRONG - Command property missing partial modifier
public partial class MyModel : ObservableModel
{
    [ObservableCommand(nameof(Execute))]
    public IObservableCommand MyCommand { get; }

    private void Execute() { }
}

// ✅ CORRECT - Command property with partial modifier
public partial class MyModel : ObservableModel
{
    [ObservableCommand(nameof(Execute))]
    public partial IObservableCommand MyCommand { get; }

    private void Execute() { }
}
```

## Why This Matters

The `partial` modifier is required for source generation:
- **Code Generation**: The source generator creates the implementation part of partial classes and properties
- **Compilation Requirement**: Without `partial`, the compiler cannot merge the generated code with your declaration
- **Framework Design**: RxBlazorV2 uses C# partial members feature to provide clean, declarative syntax

## Examples

### Example 1: Non-Partial ObservableModel Class (Error)

```csharp
// ❌ WRONG - Class inherits from ObservableModel but lacks partial modifier
[ObservableModelScope(ModelScope.Scoped)]
public class CounterModel : ObservableModel
{
    public partial int Count { get; set; }

    [ObservableCommand(nameof(Increment))]
    public partial IObservableCommand IncrementCommand { get; }

    private void Increment()
    {
        Count++;
    }
}
```

**Error:**
```
RXBG072: Class 'CounterModel' inherits from ObservableModel but is not declared as 'partial'.
The source generator cannot generate code for non-partial members.
```

**Fix:**

```csharp
// ✅ CORRECT - Add partial modifier to class
[ObservableModelScope(ModelScope.Scoped)]
public partial class CounterModel : ObservableModel
{
    public partial int Count { get; set; }

    [ObservableCommand(nameof(Increment))]
    public partial IObservableCommand IncrementCommand { get; }

    private void Increment()
    {
        Count++;
    }
}
```

### Example 2: Non-Partial Command Property (Error)

```csharp
// ❌ WRONG - Command property missing partial modifier
[ObservableModelScope(ModelScope.Scoped)]
public partial class WeatherModel : ObservableModel
{
    public partial string Temperature { get; set; }

    // ERROR: Command property must be partial
    [ObservableCommand(nameof(LoadWeather))]
    public IObservableCommandAsync LoadCommand { get; }

    private async Task LoadWeather()
    {
        // Load weather data
    }
}
```

**Error:**
```
RXBG072: Property 'LoadCommand' implements IObservableCommand but is not declared as 'partial'.
The source generator cannot generate command implementation for non-partial properties.
```

**Fix:**

```csharp
// ✅ CORRECT - Add partial modifier to command property
[ObservableModelScope(ModelScope.Scoped)]
public partial class WeatherModel : ObservableModel
{
    public partial string Temperature { get; set; }

    [ObservableCommand(nameof(LoadWeather))]
    public partial IObservableCommandAsync LoadCommand { get; }

    private async Task LoadWeather()
    {
        // Load weather data
    }
}
```

### Example 3: Multiple Observable Entities

```csharp
// ❌ WRONG - Multiple issues
public class UserModel : ObservableModel  // Missing partial on class
{
    public partial string Name { get; set; }
    public partial string Email { get; set; }

    [ObservableCommand(nameof(Save))]
    public IObservableCommandAsync SaveCommand { get; }  // Missing partial on property

    [ObservableCommand(nameof(Delete))]
    public IObservableCommandAsync DeleteCommand { get; }  // Missing partial on property

    private async Task Save() { }
    private async Task Delete() { }
}
```

**Errors:**
```
RXBG072: Class 'UserModel' inherits from ObservableModel but is not declared as 'partial'.
RXBG072: Property 'SaveCommand' implements IObservableCommand but is not declared as 'partial'.
RXBG072: Property 'DeleteCommand' implements IObservableCommand but is not declared as 'partial'.
```

**Fix:**

```csharp
// ✅ CORRECT - All entities have partial modifier
public partial class UserModel : ObservableModel
{
    public partial string Name { get; set; }
    public partial string Email { get; set; }

    [ObservableCommand(nameof(Save))]
    public partial IObservableCommandAsync SaveCommand { get; }

    [ObservableCommand(nameof(Delete))]
    public partial IObservableCommandAsync DeleteCommand { get; }

    private async Task Save() { }
    private async Task Delete() { }
}
```

### Example 4: Generic ObservableModel

```csharp
// ❌ WRONG - Generic class missing partial
[ObservableModelScope(ModelScope.Scoped)]
public class GenericModel<T> : ObservableModel where T : class
{
    public partial T? Item { get; set; }
}
```

**Fix:**

```csharp
// ✅ CORRECT - Generic class with partial modifier
[ObservableModelScope(ModelScope.Scoped)]
public partial class GenericModel<T> : ObservableModel where T : class
{
    public partial T? Item { get; set; }
}
```

### Example 5: Nested ObservableModel

```csharp
// ❌ WRONG - Nested class missing partial
public static partial class Models
{
    public class NestedModel : ObservableModel  // Missing partial
    {
        public partial string Data { get; set; }
    }
}
```

**Fix:**

```csharp
// ✅ CORRECT - Nested class with partial modifier
public static partial class Models
{
    public partial class NestedModel : ObservableModel
    {
        public partial string Data { get; set; }
    }
}
```

## What Happens Without Partial

If you don't use `partial`, you'll encounter compilation errors:

1. **For Classes**: The generator creates a separate partial class implementation, but the compiler cannot merge it with your non-partial declaration
2. **For Properties**: The generator creates property implementations, but without `partial`, there's no declaration to complete

## Code Fix Available

The RxBlazorV2 code fix provider can automatically add the `partial` modifier to classes and properties:

1. Place cursor on the error
2. Press `Ctrl+.` (or `Cmd+.` on Mac)
3. Select "Add 'partial' modifier to class" or "Add 'partial' modifier to property"

## Important Notes

1. **Both Required**: For command properties, both the class AND the property must be partial
2. **All Command Types**: This applies to all command interfaces:
   - `IObservableCommand`
   - `IObservableCommand<T>`
   - `IObservableCommandAsync`
   - `IObservableCommandAsync<T>`
   - `IObservableCommandR<TResult>`
   - `IObservableCommandRAsync<TResult>`
   - And their generic variants

3. **Partial Properties**: RxBlazorV2 uses C# 13's partial properties feature for clean syntax

## Severity

**Error** - This diagnostic will block compilation and must be fixed before the code can build successfully.

## Related Diagnostics

- RXBG001: Observable model analysis error
- RXBG070: ObservableModel is missing ObservableModelScope attribute
- RXBG071: Partial constructor with DI parameters must be public

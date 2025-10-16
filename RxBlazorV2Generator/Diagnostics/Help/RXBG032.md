# RXBG032: Command Execute Method Should Not Return a Value

## Description

This diagnostic is reported when a command property is declared as `IObservableCommand` or `IObservableCommandAsync` (without return value support), but its execute method returns a value. Commands without the "R" suffix do not support return values.

## Cause

This error occurs when:
- A command property is declared as `IObservableCommand` or `IObservableCommandAsync`
- The execute method returns a value (not `void` or `Task`)
- The command type does not include "R" which indicates return value support

## How to Fix

You have two options:

### Option 1: Change Command Type to Support Return Values

Change the command property type to `IObservableCommandR<T>` or `IObservableCommandRAsync<T>`:

```csharp
// Change from:
[ObservableCommand(nameof(Calculate))]
public partial IObservableCommand CalculateCommand { get; }

private int Calculate()  // Returns int
{
    return Counter * 10;
}

// To:
[ObservableCommand(nameof(Calculate))]
public partial IObservableCommandR<int> CalculateCommand { get; }  // Use IObservableCommandR<int>

private int Calculate()
{
    return Counter * 10;
}
```

### Option 2: Remove Return Value from Execute Method

Change the execute method to return `void` or `Task`:

```csharp
[ObservableCommand(nameof(Execute))]
public partial IObservableCommand ExecuteCommand { get; }

// Change from:
private int Execute()
{
    Counter++;
    return Counter;
}

// To:
private void Execute()
{
    Counter++;
    // No return statement
}
```

## Examples

### Example 1: Sync Command with Unwanted Return Value

```csharp
// ❌ WRONG - IObservableCommand doesn't support return values
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public partial int Counter { get; set; }

    [ObservableCommand(nameof(Increment))]
    public partial IObservableCommand IncrementCommand { get; }

    private int Increment()  // Error: Returns int but command doesn't support it
    {
        Counter++;
        return Counter;
    }
}
```

**Fix:**

```csharp
// ✅ CORRECT - Changed to IObservableCommandR<int>
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public partial int Counter { get; set; }

    [ObservableCommand(nameof(Increment))]
    public partial IObservableCommandR<int> IncrementCommand { get; }  // Now supports return

    private int Increment()
    {
        Counter++;
        return Counter;
    }
}
```

### Example 2: Async Command with Unwanted Return Value

```csharp
// ❌ WRONG - IObservableCommandAsync doesn't support return values
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    [ObservableCommand(nameof(LoadDataAsync))]
    public partial IObservableCommandAsync LoadCommand { get; }

    private async Task<string> LoadDataAsync()  // Error: Returns Task<string>
    {
        await Task.Delay(1000);
        return "Data loaded";
    }
}
```

**Fix:**

```csharp
// ✅ CORRECT - Changed to IObservableCommandRAsync<string>
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    [ObservableCommand(nameof(LoadDataAsync))]
    public partial IObservableCommandRAsync<string> LoadCommand { get; }  // Now supports return

    private async Task<string> LoadDataAsync()
    {
        await Task.Delay(1000);
        return "Data loaded";
    }
}
```

### Example 3: Parameterized Command with Unwanted Return Value

```csharp
// ❌ WRONG - IObservableCommand<T> doesn't support return values
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    [ObservableCommand(nameof(Calculate))]
    public partial IObservableCommand<int> CalculateCommand { get; }

    private double Calculate(int value)  // Error: Returns double
    {
        return value * 1.5;
    }
}
```

**Fix:**

```csharp
// ✅ CORRECT - Changed to IObservableCommandR<int, double>
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    [ObservableCommand(nameof(Calculate))]
    public partial IObservableCommandR<int, double> CalculateCommand { get; }  // Now supports parameter and return

    private double Calculate(int value)
    {
        return value * 1.5;
    }
}
```

## Why This Matters

Using the wrong command type:
- Prevents proper code generation
- Causes compilation errors in generated code
- Makes it unclear what the command returns
- Violates the framework's type safety guarantees

## Command Type Reference

| Command Type | Parameter | Return Value | Async |
|-------------|-----------|--------------|-------|
| `IObservableCommand` | No | No | No |
| `IObservableCommand<T>` | Yes | No | No |
| `IObservableCommandR<T>` | No | Yes (T) | No |
| `IObservableCommandR<T1, T2>` | Yes (T1) | Yes (T2) | No |
| `IObservableCommandAsync` | No | No | Yes |
| `IObservableCommandAsync<T>` | Yes | No | Yes |
| `IObservableCommandRAsync<T>` | No | Yes (T) | Yes |
| `IObservableCommandRAsync<T1, T2>` | Yes (T1) | Yes (T2) | Yes |

## Related Diagnostics

- RXBG033: Command execute method must return a value

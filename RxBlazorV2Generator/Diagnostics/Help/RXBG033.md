# RXBG033: Command Execute Method Must Return a Value

## Description

This diagnostic is reported when a command property is declared as `IObservableCommandR` or `IObservableCommandRAsync` (with return value support), but its execute method does not return the expected value. Commands with the "R" suffix require a return value.

## Cause

This error occurs when:
- A command property is declared as `IObservableCommandR<T>` or `IObservableCommandRAsync<T>`
- The execute method returns `void` or `Task` (without a value)
- The command type expects a return value of type `T`

## How to Fix

You have two options:

### Option 1: Add Return Value to Execute Method

Change the execute method to return the expected type:

```csharp
[ObservableCommand(nameof(Calculate))]
public partial IObservableCommandR<int> CalculateCommand { get; }

// Change from:
private void Calculate()
{
    Counter++;
}

// To:
private int Calculate()
{
    Counter++;
    return Counter * 10;
}
```

### Option 2: Change Command Type to Not Require Return Value

Change the command property type to `IObservableCommand` or `IObservableCommandAsync`:

```csharp
// Change from:
[ObservableCommand(nameof(Execute))]
public partial IObservableCommandR<int> ExecuteCommand { get; }

private void Execute()  // Doesn't return anything
{
    Counter++;
}

// To:
[ObservableCommand(nameof(Execute))]
public partial IObservableCommand ExecuteCommand { get; }  // Remove R and type parameter

private void Execute()
{
    Counter++;
}
```

## Examples

### Example 1: Sync Command Missing Return Value

```csharp
// ❌ WRONG - IObservableCommandR<int> requires int return value
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public partial int Counter { get; set; }

    [ObservableCommand(nameof(Increment))]
    public partial IObservableCommandR<int> IncrementCommand { get; }  // Expects int return

    private void Increment()  // Error: Doesn't return int
    {
        Counter++;
    }
}
```

**Fix:**

```csharp
// ✅ CORRECT - Now returns int
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public partial int Counter { get; set; }

    [ObservableCommand(nameof(Increment))]
    public partial IObservableCommandR<int> IncrementCommand { get; }

    private int Increment()  // Now returns int
    {
        Counter++;
        return Counter * 10;
    }
}
```

### Example 2: Async Command Missing Return Value

```csharp
// ❌ WRONG - IObservableCommandRAsync<string> requires Task<string> return
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    [ObservableCommand(nameof(LoadDataAsync))]
    public partial IObservableCommandRAsync<string> LoadCommand { get; }  // Expects string return

    private async Task LoadDataAsync()  // Error: Returns Task instead of Task<string>
    {
        await Task.Delay(1000);
        // No return statement
    }
}
```

**Fix:**

```csharp
// ✅ CORRECT - Now returns Task<string>
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    [ObservableCommand(nameof(LoadDataAsync))]
    public partial IObservableCommandRAsync<string> LoadCommand { get; }

    private async Task<string> LoadDataAsync()  // Now returns Task<string>
    {
        await Task.Delay(1000);
        return "Data loaded";
    }
}
```

### Example 3: Parameterized Command Missing Return Value

```csharp
// ❌ WRONG - IObservableCommandR<int, double> requires double return value
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    [ObservableCommand(nameof(Calculate))]
    public partial IObservableCommandR<int, double> CalculateCommand { get; }  // Expects double return

    private void Calculate(int value)  // Error: Doesn't return double
    {
        // Calculation but no return
    }
}
```

**Fix:**

```csharp
// ✅ CORRECT - Now returns double
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    [ObservableCommand(nameof(Calculate))]
    public partial IObservableCommandR<int, double> CalculateCommand { get; }

    private double Calculate(int value)  // Now returns double
    {
        return value * 1.5;
    }
}
```

### Example 4: Nullable Return Type

```csharp
// ✅ CORRECT - Returns nullable int
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    [ObservableCommand(nameof(FindValue))]
    public partial IObservableCommandR<int?> FindCommand { get; }  // Expects int? return

    private int? FindValue()  // Returns int? correctly
    {
        if (SomeCondition)
        {
            return 42;
        }
        return null;
    }
}
```

## Why This Matters

Missing return values:
- Prevents proper code generation
- Causes compilation errors in generated code
- Makes the command unusable for retrieving results
- Violates the framework's type contracts

## Command Type Reference

| Command Type | Parameter | Return Value | Async |
|-------------|-----------|--------------|-------|
| `IObservableCommand` | No | No | No |
| `IObservableCommand<T>` | Yes | No | No |
| `IObservableCommandR<T>` | No | **Yes (T)** | No |
| `IObservableCommandR<T1, T2>` | Yes (T1) | **Yes (T2)** | No |
| `IObservableCommandAsync` | No | No | Yes |
| `IObservableCommandAsync<T>` | Yes | No | Yes |
| `IObservableCommandRAsync<T>` | No | **Yes (T)** | Yes |
| `IObservableCommandRAsync<T1, T2>` | Yes (T1) | **Yes (T2)** | Yes |

## Related Diagnostics

- RXBG032: Command execute method should not return a value

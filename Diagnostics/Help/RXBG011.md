# RXBG011: Command Trigger Type Arguments Mismatch

## Description

This diagnostic is reported when an `ObservableCommandTrigger` attribute has generic type arguments that don't match the command's generic type arguments. Type arguments must match exactly for proper type safety.

## Cause

This error occurs when:
- A parametrized command uses `IObservableCommand<T>` or `IObservableCommandAsync<T>`
- The trigger attribute uses `ObservableCommandTrigger<TParam>` with a different type parameter
- The type arguments don't match between command and trigger

## How to Fix

Use the available code fix:
- **Fix trigger type arguments** - Updates the trigger's type parameters to match the command

Or manually:
- Ensure the trigger's generic type matches the command's type parameter exactly
- For non-generic commands, don't use generic trigger syntax

## Examples

### Example 1: Type Mismatch

```csharp
// ❌ WRONG - Command uses string but trigger uses int
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public partial string Name { get; set; } = "";

    [ObservableCommand(nameof(ExecuteMethodWithParam))]
    [ObservableCommandTrigger<int>(nameof(Name), 3)]  // Error: Should be <string>
    public partial IObservableCommand<string> TestCommand { get; }

    private void ExecuteMethodWithParam(string parameter)
    {
        Console.WriteLine($"Executed with Name: {Name}, Parameter: {parameter}");
    }
}
```

### Example 2: Fix by Matching Types

```csharp
// ✅ CORRECT - Trigger type matches command type
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public partial string Name { get; set; } = "";

    [ObservableCommand(nameof(ExecuteMethodWithParam))]
    [ObservableCommandTrigger<string>(nameof(Name), "NewTest")]
    public partial IObservableCommand<string> TestCommand { get; }

    private void ExecuteMethodWithParam(string parameter)
    {
        Console.WriteLine($"Executed with Name: {Name}, Parameter: {parameter}");
    }
}
```

### Example 3: Non-Generic Command

```csharp
// ✅ CORRECT - Non-generic command with non-generic trigger
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public partial string Name { get; set; } = "";

    [ObservableCommand(nameof(ExecuteMethod))]
    [ObservableCommandTrigger(nameof(Name))]  // No type parameter
    public partial IObservableCommand TestCommand { get; }

    private void ExecuteMethod()
    {
        Console.WriteLine($"Executed with Name: {Name}");
    }
}
```

## Code Fixes Available

- **Fix trigger type arguments to match command**: Updates the trigger's generic type parameter

## Related Diagnostics

- RXBG012: Circular trigger reference

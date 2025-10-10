# RXBG012: Circular Trigger Reference Detected

## Description

This diagnostic is reported when a command is triggered by a property that the command's execution method modifies. This creates an infinite loop where the command executes, modifies the property, which triggers the command again, and so on.

## Cause

This error occurs when:
- A command has an `ObservableCommandTrigger` for a property
- The command's execution method **modifies (writes to)** that same property
- This creates an infinite execution loop

## Important: Reading vs Writing

**Reading a trigger property is safe and allowed:**
- ✅ Your command can READ the trigger property's value
- ✅ The generator automatically handles read-only trigger properties
- ✅ No circular trigger occurs if the property is only read

**Writing to a trigger property causes an error:**
- ❌ Modifying (assigning, incrementing) the trigger property creates infinite loop
- ❌ This diagnostic fires when the trigger property is written

Example of safe usage:
```csharp
[ObservableCommand(nameof(SearchAsync))]
[ObservableCommandTrigger(nameof(SearchText))]  // ✅ Safe: SearchText triggers command
public partial IObservableCommandAsync SearchCommand { get; }

private async Task SearchAsync()
{
    Results = $"Searching for '{SearchText}'...";  // ✅ Reading SearchText is OK
    await Task.Delay(500);
    Results = $"Found results for: {SearchText}";  // ✅ Still OK - only reading
    // SearchText is never modified, so no circular trigger
}
```

## How to Fix

Use the available code fix:
- **Remove circular trigger** - Removes the trigger that creates the circular reference

Or manually:
- Remove the trigger attribute for the property being modified
- Modify a different property in the execution method
- Trigger the command based on a different property

## Examples

### Example 1: Simple Circular Reference

```csharp
// ❌ WRONG - Counter triggers command, command modifies Counter (infinite loop)
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public partial int Counter { get; set; }

    [ObservableCommand(nameof(IncrementCounter))]
    [ObservableCommandTrigger(nameof(Counter))]  // Error: Creates infinite loop
    public partial IObservableCommand IncrementCommand { get; }

    private void IncrementCounter()
    {
        Counter++;  // This modifies Counter, which triggers the command again
    }
}
```

### Example 2: Fix by Removing Trigger

```csharp
// ✅ CORRECT - Removed the circular trigger
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public partial int Counter { get; set; }

    [ObservableCommand(nameof(IncrementCounter))]
    public partial IObservableCommand IncrementCommand { get; }

    private void IncrementCounter()
    {
        Counter++;
    }
}
```

### Example 3: Fix by Triggering on Different Property

```csharp
// ✅ CORRECT - Triggers on different property than it modifies
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public partial int Counter { get; set; }
    public partial string Message { get; set; } = "";

    [ObservableCommand(nameof(UpdateMessage))]
    [ObservableCommandTrigger(nameof(Counter))]  // OK: Triggers on Counter
    public partial IObservableCommand UpdateMessageCommand { get; }

    private void UpdateMessage()
    {
        Message = $"Counter is now: {Counter}";  // Modifies Message, not Counter
    }
}
```

### Example 4: Assignment Creates Circular Reference

```csharp
// ❌ WRONG - Assignment also creates circular reference
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public partial int Counter { get; set; }

    [ObservableCommand(nameof(ResetCounter))]
    [ObservableCommandTrigger(nameof(Counter))]  // Error: Creates infinite loop
    public partial IObservableCommand ResetCommand { get; }

    private void ResetCounter()
    {
        Counter = 0;  // This also modifies Counter
    }
}
```

### Example 5: Multiple Triggers - Only One Circular

```csharp
// ❌ WRONG - Counter2 trigger creates circular reference
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public partial int Counter1 { get; set; }
    public partial int Counter2 { get; set; }

    [ObservableCommand(nameof(IncrementCounter2))]
    [ObservableCommandTrigger(nameof(Counter1))]  // OK: Different property
    [ObservableCommandTrigger(nameof(Counter2))]  // Error: Circular reference
    public partial IObservableCommand IncrementCommand { get; }

    private void IncrementCounter2()
    {
        Counter2++;  // Modifies Counter2, which has a trigger
    }
}
```

### Example 6: Async Command with Circular Reference

```csharp
// ❌ WRONG - Async commands can also have circular triggers
[ObservableModelScope(ModelScope.Singleton)]
public partial class TestModel : ObservableModel
{
    public partial string Status { get; set; } = "";

    [ObservableCommand(nameof(UpdateStatusAsync))]
    [ObservableCommandTrigger(nameof(Status))]  // Error: Creates infinite loop
    public partial IObservableCommandAsync UpdateCommand { get; }

    private async Task UpdateStatusAsync()
    {
        await Task.Delay(100);
        Status = "Updated";  // Modifies Status
    }
}
```

## Why This Matters

Circular trigger references cause:
- Infinite loops that can freeze your application
- Stack overflow exceptions
- Poor performance and excessive memory usage
- Unpredictable application behavior

## Code Fixes Available

- **Remove circular trigger attribute**: Removes the trigger that creates the circular reference

## Related Diagnostics

- RXBG011: Trigger type arguments mismatch

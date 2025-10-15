# RXBG002: Method Analysis Warning

## Description

This warning is reported when the source generator encounters an issue while analyzing a method for property usage. This typically occurs during analysis of command methods or other methods that may use observable properties.

## Cause

This warning occurs when:
- The generator cannot fully analyze property usage in a method
- Method body contains patterns that are difficult to analyze
- Property access detection encounters edge cases

## How to Fix

This is a warning and usually does not prevent code generation. However, you can:
1. Simplify complex method logic if possible
2. Ensure property access patterns are straightforward
3. Review the specific warning message for details

## Example

```csharp
[ObservableModelScope(ModelScope.Singleton)]
public partial class MyModel : ObservableModel
{
    public partial string Name { get; set; }

    [ObservableCommand(nameof(Execute))]
    public partial IObservableCommand MyCommand { get; }

    // If this method has complex property access patterns,
    // it might generate a warning
    private void Execute()
    {
        // Complex logic here
        var x = Name;
    }
}
```

## Related Diagnostics

- RXBG031: Circular trigger reference error

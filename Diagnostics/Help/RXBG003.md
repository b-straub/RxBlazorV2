# RXBG003: Code Generation Error

## Description

This diagnostic is reported when the source generator encounters an error while generating source code. This is an internal error that indicates a problem during the code generation phase.

## Cause

This error occurs when:
- The generator cannot create the expected source code
- There are template rendering issues
- Code generation logic encounters an exception

## How to Fix

1. Review your model structure for unsupported patterns
2. Check that all types referenced in your model are valid
3. Ensure partial properties and commands follow expected patterns
4. If the error persists, it may indicate a bug in the generator - please report it with the error message details

## Example

```csharp
// If you see this error during code generation
[ObservableModelScope(ModelScope.Singleton)]
public partial class MyModel : ObservableModel
{
    public partial string Name { get; set; }

    [ObservableCommand(nameof(Execute))]
    public partial IObservableCommand MyCommand { get; }

    private void Execute() { }
}
```

## Related Diagnostics

- RXBG001: Observable model analysis error
- RXBG002: Razor component analysis error

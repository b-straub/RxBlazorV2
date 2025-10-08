# RXBG001: Observable Model Analysis Error

## Description

This diagnostic is reported when the source generator encounters an error while analyzing an `ObservableModel` class. This is typically an internal error that indicates a problem during the semantic analysis phase.

## Cause

This error occurs when:
- The generator cannot properly analyze the structure of an `ObservableModel` class
- There are unexpected semantic model issues
- Internal analysis logic encounters an exception

## How to Fix

1. Check that your `ObservableModel` class follows the expected structure
2. Ensure all referenced types are properly defined and accessible
3. Verify that partial properties and commands use correct syntax
4. If the error persists, it may indicate a bug in the generator - please report it with the error message details

## Example

```csharp
// If you see this error, check your ObservableModel structure
[ObservableModelScope(ModelScope.Singleton)]
public partial class MyModel : ObservableModel
{
    // Ensure properties and commands follow correct patterns
    public partial string Name { get; set; }
}
```

## Related Diagnostics

- RXBG002: Razor component analysis error
- RXBG003: Code generation error

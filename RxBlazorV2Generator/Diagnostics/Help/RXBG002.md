# RXBG002: Razor Component Analysis Error

## Description

This diagnostic is reported when the source generator encounters an error while analyzing a Razor component that uses observable models. This is typically an internal error during the Razor file analysis phase.

## Cause

This error occurs when:
- The generator cannot properly analyze a Razor component's structure
- There are issues reading or parsing Razor component files
- Observable model references in components cannot be resolved

## How to Fix

1. Verify that your Razor component files are properly structured
2. Ensure `ObservableComponent<T>` inheritance is correct
3. Check that all model references are valid and accessible
4. If the error persists, it may indicate a bug in the generator - please report it with the error message details

## Example

```csharp
// Razor component code-behind
public partial class MyComponent : ObservableComponent<MyModel>
{
    // Ensure proper structure and model references
}
```

## Related Diagnostics

- RXBG001: Observable model analysis error
- RXBG009: Component inheritance error

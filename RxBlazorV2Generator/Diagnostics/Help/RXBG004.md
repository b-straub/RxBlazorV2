# RXBG004: Source Generator Encountered Diagnostic Errors

## Description

This diagnostic is reported when the source generator encounters diagnostic errors during code generation. It wraps analyzer diagnostics for compilation output, ensuring the build fails with a clear message while preventing duplicate code fixes in the IDE.

## Cause

This error occurs when:
- The analyzer has detected issues with your `ObservableModel` that prevent code generation
- There are structural problems with properties, commands, or model references
- DI scope violations or other configuration errors exist

## How to Fix

1. Check the IDE/analyzer output for detailed error information and the original diagnostic ID
2. The message includes the original diagnostic title and ID (e.g., "Missing partial modifier (RXBG072)")
3. Look up the referenced diagnostic ID for specific fix instructions
4. Apply any available code fixes shown in the IDE

## Example

```csharp
// If you see: "Source generator failed: Observable entity must be declared as partial (RXBG072)"
// The fix is to add the 'partial' modifier:

// Before (causes RXBG004 wrapping RXBG072):
public class MyModel : ObservableModel { }

// After:
public partial class MyModel : ObservableModel { }
```

## Related Diagnostics

This diagnostic wraps other diagnostics. Check the message for the original diagnostic ID:

- RXBG070: Missing ObservableModelScope attribute
- RXBG071: Non-public partial constructor
- RXBG072: Missing partial modifier
- And other RXBG0xx diagnostics

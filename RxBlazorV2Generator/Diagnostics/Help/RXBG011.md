# RXBG011: Invalid Model Reference Target

## Description

This diagnostic is reported when an `ObservableModelReference` attribute references a class that does not inherit from `ObservableModel`.

## Cause

This error occurs when:
- The referenced type is a regular class that doesn't inherit from `ObservableModel`
- The referenced type is missing the `: ObservableModel` inheritance

## How to Fix

Use one of the available code fixes:
1. **Remove the invalid ObservableModelReference attribute** - Removes the attribute referencing the invalid type
2. **Make class inherit from ObservableModel** - Adds `ObservableModel` inheritance and makes the class `partial`

## Examples

### Example 1: Invalid Reference

```csharp
// ❌ WRONG - InvalidModel doesn't inherit from ObservableModel
public class InvalidModel
{
    public string Name { get; set; }
}

[ObservableModelReference<InvalidModel>]  // Error: InvalidModel is not an ObservableModel
[ObservableModelScope(ModelScope.Scoped)]
public partial class TestClass : ObservableModel
{
    public partial int Value { get; set; }
}
```

### Example 2: Fix by Removing Attribute

```csharp
// ✅ CORRECT - Removed invalid reference
public class InvalidModel
{
    public string Name { get; set; }
}

[ObservableModelScope(ModelScope.Scoped)]
public partial class TestClass : ObservableModel
{
    public partial int Value { get; set; }
}
```

### Example 3: Fix by Making Class Observable

```csharp
// ✅ CORRECT - Made InvalidModel inherit from ObservableModel
public partial class ValidModel : ObservableModel
{
    public string Name { get; set; }
}

[ObservableModelReference<ValidModel>]
[ObservableModelScope(ModelScope.Scoped)]
public partial class TestClass : ObservableModel
{
    public partial int Value { get; set; }

    public string GetName() => ValidModel.Name;
}
```

## Code Fixes Available

- **Remove ObservableModelReference attribute**: Removes the invalid attribute
- **Make class inherit from ObservableModel**: Adds inheritance and makes the class partial

## Related Diagnostics

- RXBG051: Circular model reference
- RXBG031: Unused model reference

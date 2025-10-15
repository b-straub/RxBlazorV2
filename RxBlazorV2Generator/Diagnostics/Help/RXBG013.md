# RXBG013: Cannot Reference Derived ObservableModel

## Description

This diagnostic is reported when an `ObservableModelReference` attribute references a derived ObservableModel (a model that inherits from another ObservableModel class).

## Cause

This error occurs when:
- The referenced model inherits from another ObservableModel class (not directly from `ObservableModel`)
- Derived models are excluded from dependency injection registration and cannot be resolved
- The generator filters out derived models from DI because they inherit observability from their base class

## Why This Is Restricted

Derived ObservableModels are not registered in DI because:
1. **Inheritance provides observability**: The derived class IS the base class through inheritance, so all property changes flow through the base model's Observable infrastructure
2. **No DI registration**: Only concrete, non-derived models are registered in the service collection
3. **Use base or composition**: Reference the base model instead, or refactor to use composition over inheritance

## How to Fix

Use one of the available solutions:
1. **Remove the ObservableModelReference attribute** (available code fix) - Removes the invalid attribute
2. **Reference the base model instead** - Change the reference to point to the base ObservableModel
3. **Refactor to composition** - Instead of using inheritance, create a separate model and use composition

## Examples

### Example 1: Invalid Reference to Derived Model

```csharp
// ❌ WRONG - BasicCommandsModel is a derived ObservableModel
public abstract partial class SampleBaseModel : ObservableModel
{
    public abstract string Usage { get; }
    public partial ObservableList<LogEntry> LogEntries { get; init; } = new();
}

public partial class BasicCommandsModel : SampleBaseModel
{
    public override string Usage => "Basic commands example";
    public partial int Counter { get; set; }
}

[ObservableModelReference<BasicCommandsModel>]  // Error: BasicCommandsModel is derived
[ObservableModelScope(ModelScope.Scoped)]
public partial class ParentModel : ObservableModel
{
    public partial int Value { get; set; }

    public int GetCounter() => BasicCommandsModel.Counter;
}
```

### Example 2: Fix by Removing Attribute

```csharp
// ✅ CORRECT - Removed invalid derived model reference
public abstract partial class SampleBaseModel : ObservableModel
{
    public abstract string Usage { get; }
    public partial ObservableList<LogEntry> LogEntries { get; init; } = new();
}

public partial class BasicCommandsModel : SampleBaseModel
{
    public override string Usage => "Basic commands example";
    public partial int Counter { get; set; }
}

[ObservableModelScope(ModelScope.Scoped)]
public partial class ParentModel : ObservableModel
{
    public partial int Value { get; set; }
    // Don't reference derived model
}
```

### Example 3: Fix by Referencing Base Model

```csharp
// ✅ CORRECT - Reference the base model instead
public abstract partial class SampleBaseModel : ObservableModel
{
    public abstract string Usage { get; }
    public partial ObservableList<LogEntry> LogEntries { get; init; } = new();
}

public partial class BasicCommandsModel : SampleBaseModel
{
    public override string Usage => "Basic commands example";
    public partial int Counter { get; set; }
}

[ObservableModelReference<SampleBaseModel>]  // Reference base model instead
[ObservableModelScope(ModelScope.Scoped)]
public partial class ParentModel : ObservableModel
{
    public partial int Value { get; set; }

    public ObservableList<LogEntry> GetLogs() => SampleBaseModel.LogEntries;
}
```

### Example 4: Fix by Using Composition

```csharp
// ✅ CORRECT - Use composition instead of inheritance
public partial class LoggingModel : ObservableModel
{
    public partial ObservableList<LogEntry> LogEntries { get; init; } = new();
}

public partial class BasicCommandsModel : ObservableModel
{
    public partial string Usage { get; set; } = "Basic commands example";
    public partial int Counter { get; set; }
}

[ObservableModelReference<BasicCommandsModel>]  // Now valid - not a derived model
[ObservableModelScope(ModelScope.Scoped)]
public partial class ParentModel : ObservableModel
{
    public partial int Value { get; set; }

    public int GetCounter() => BasicCommandsModel.Counter;
}
```

## Code Fixes Available

- **Remove ObservableModelReference attribute**: Removes the invalid attribute referencing the derived model

## Design Considerations

When designing your ObservableModel hierarchy:

1. **Prefer composition over inheritance** for models that need to be referenced
2. **Use inheritance for shared base functionality** but don't reference derived classes
3. **Reference only the base model** if you need properties from a derived model's hierarchy
4. **Consider extracting shared properties** into a separate composable model

## Related Diagnostics

- RXBG030: Invalid model reference target
- RXBG031: Unused model reference
- RXBG051: Circular model reference

# RXBG052: Abstract Class Cannot Be Used in Partial Constructor

## Description

This diagnostic is reported when a partial constructor parameter uses an abstract class type. Abstract classes cannot be instantiated directly, so the dependency injection container cannot resolve them.

## Cause

Abstract classes are designed to be base classes that must be derived from. They cannot be instantiated directly, which means:

1. The DI container cannot create an instance of the abstract class
2. There is no default implementation to inject
3. The runtime will fail when trying to resolve the dependency

## How to Fix

### Option 1: Use a Concrete Implementation

Create a class that derives from the abstract class and use that instead:

```csharp
// Abstract base class
public abstract class StatusBaseModel : ObservableModel
{
    public abstract void AddError(string message);
}

// Concrete implementation
[ObservableComponent]
[ObservableModelScope(ModelScope.Singleton)]
public partial class AppStatusModel : StatusBaseModel
{
    public override void AddError(string message) { /* implementation */ }
}

// Use the concrete class
public partial class MyModel : ObservableModel
{
    public partial MyModel(AppStatusModel statusModel);  // Use concrete type
}
```

### Option 2: Use an Interface

If you need abstraction for testing or multiple implementations, use an interface instead:

```csharp
// Interface
public interface IStatusModel
{
    void AddError(string message);
}

// Concrete implementation
[ObservableComponent]
[ObservableModelScope(ModelScope.Singleton)]
public partial class AppStatusModel : StatusBaseModel, IStatusModel
{
    public override void AddError(string message) { /* implementation */ }
}

// Register the interface
builder.Services.AddSingleton<IStatusModel, AppStatusModel>();

// Use the interface
public partial class MyModel : ObservableModel
{
    public partial MyModel(IStatusModel statusModel);
}
```

### Option 3: Remove the Parameter

If the abstract class was added by mistake, simply remove it:

```csharp
// Before (error)
public partial class MyModel : ObservableModel
{
    public partial MyModel(StatusBaseModel statusModel);  // Error: abstract class
}

// After (fixed)
public partial class MyModel : ObservableModel
{
    // No constructor needed, or use concrete types only
}
```

## Examples

### Example 1: Using StatusBaseModel Incorrectly

```csharp
// StatusBaseModel is an abstract base class
public abstract class StatusBaseModel : ObservableModel
{
    public ObservableList<StatusMessage> Messages { get; }
    public abstract void HandleError(Exception error, string commandName, string methodName);
}

// Error: Cannot inject abstract class
public partial class OrderModel : ObservableModel
{
    public partial OrderModel(StatusBaseModel statusModel);  // RXBG052
}
```

**Fix: Use a concrete StatusModel**

```csharp
// Concrete implementation in your app
[ObservableComponent]
[ObservableModelScope(ModelScope.Singleton)]
public partial class AppStatusModel : StatusBaseModel
{
    public override void HandleError(Exception error, string commandName, string methodName)
    {
        AddError($"{commandName}.{methodName}: {error.Message}");
    }
}

// Use the concrete class
public partial class OrderModel : ObservableModel
{
    public partial OrderModel(AppStatusModel statusModel);  // OK
}
```

### Example 2: MudBlazor StatusModel

The RxBlazorV2.MudBlazor package provides a ready-to-use `StatusModel`:

```csharp
using RxBlazorV2.MudBlazor.Components;

public partial class MyModel : ObservableModel
{
    // Use the MudBlazor StatusModel (concrete implementation)
    public partial MyModel(StatusModel statusModel);  // OK
}
```

## Code Fix

The code fix removes the abstract class parameter from the partial constructor.

## Severity

**Error** - This will cause a runtime exception when the DI container tries to resolve the abstract class.

## Related Diagnostics

- RXBG050: Partial constructor parameter type may not be registered in DI
- RXBG051: DI service scope violation
- RXBG013: Cannot reference derived ObservableModel

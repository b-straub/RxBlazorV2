# RXBG071: Partial Constructor with DI Parameters Must Be Public

## Description

This error is reported when a partial constructor in an `ObservableModel` has dependency injection parameters but is not declared as `public`. The dependency injection container can only resolve constructors that are publicly accessible.

## Cause

This error occurs when:
- A partial constructor is declared on an `ObservableModel`
- The constructor has one or more parameters (for dependency injection)
- The constructor is declared as `protected`, `private`, `internal`, or `protected internal`

## How to Fix

Change the constructor accessibility to `public`:

```csharp
// ❌ WRONG - Non-public constructor with DI parameters
protected partial MyModel(HttpClient httpClient);

// ✅ CORRECT - Public constructor with DI parameters
public partial MyModel(HttpClient httpClient);
```

## Why This Matters

The .NET dependency injection container requires constructors to be public:
- **Runtime Error Prevention**: Non-public constructors will cause `InvalidOperationException` at runtime when DI tries to instantiate your model
- **DI Container Limitation**: The DI container uses reflection to create instances and can only access public constructors
- **Best Practice**: Public constructors with DI parameters follow standard .NET dependency injection patterns

## Examples

### Example 1: Protected Constructor (Error)

```csharp
// ❌ WRONG - Protected partial constructor with DI parameters
[ObservableModelScope(ModelScope.Scoped)]
public partial class WeatherModel : ObservableModel
{
    public partial string Temperature { get; set; }

    // ERROR: Cannot inject HttpClient into protected constructor
    protected partial WeatherModel(HttpClient httpClient);
}
```

**Fix:**

```csharp
// ✅ CORRECT - Public partial constructor
[ObservableModelScope(ModelScope.Scoped)]
public partial class WeatherModel : ObservableModel
{
    public partial string Temperature { get; set; }

    // Public constructor allows DI to work
    public partial WeatherModel(HttpClient httpClient);
}
```

### Example 2: Private Constructor (Error)

```csharp
// ❌ WRONG - Private partial constructor
[ObservableModelScope(ModelScope.Scoped)]
public partial class UserModel : ObservableModel
{
    // ERROR: DI cannot access private constructor
    private partial UserModel(IUserService userService);
}
```

**Fix:**

```csharp
// ✅ CORRECT - Public partial constructor
[ObservableModelScope(ModelScope.Scoped)]
public partial class UserModel : ObservableModel
{
    public partial UserModel(IUserService userService);
}
```

### Example 3: Internal Constructor (Error)

```csharp
// ❌ WRONG - Internal partial constructor
[ObservableModelScope(ModelScope.Scoped)]
public partial class DataModel : ObservableModel
{
    // ERROR: DI cannot access internal constructor
    internal partial DataModel(IDataService dataService);
}
```

**Fix:**

```csharp
// ✅ CORRECT - Public partial constructor
[ObservableModelScope(ModelScope.Scoped)]
public partial class DataModel : ObservableModel
{
    public partial DataModel(IDataService dataService);
}
```

### Example 4: Protected Internal Constructor (Error)

```csharp
// ❌ WRONG - Protected internal partial constructor
[ObservableModelScope(ModelScope.Scoped)]
public partial class BaseModel : ObservableModel
{
    // ERROR: DI cannot access protected internal constructor
    protected internal partial BaseModel(IServiceProvider serviceProvider);
}
```

**Fix:**

```csharp
// ✅ CORRECT - Public partial constructor
[ObservableModelScope(ModelScope.Scoped)]
public partial class BaseModel : ObservableModel
{
    public partial BaseModel(IServiceProvider serviceProvider);
}
```

### Example 5: Parameterless Protected Constructor (No Error)

```csharp
// ✅ CORRECT - Parameterless constructors can be non-public
[ObservableModelScope(ModelScope.Scoped)]
public partial class SimpleModel : ObservableModel
{
    // No error - parameterless constructor doesn't need DI
    protected partial SimpleModel();
}
```

### Example 6: Multiple Dependencies

```csharp
// ✅ CORRECT - Public constructor with multiple DI parameters
[ObservableModelScope(ModelScope.Scoped)]
public partial class ComplexModel : ObservableModel
{
    public partial string Status { get; set; }

    // Public constructor allows DI to inject all dependencies
    public partial ComplexModel(
        HttpClient httpClient,
        ILogger<ComplexModel> logger,
        ISnackbar snackbar);
}
```

## Runtime Error Example

Without this fix, you would get a runtime error like:

```
System.InvalidOperationException: Unable to resolve service for type 'MyModel'
while attempting to activate 'MyComponent'.
```

This happens because the DI container cannot access the non-public constructor.

## Important Notes

1. **Parameterless Constructors**: If your partial constructor has no parameters, it can be any visibility (public, protected, private, internal). This rule only applies to constructors with DI parameters.

2. **Code Fix Available**: The RxBlazorV2 code fix provider can automatically change the constructor to public for you.

3. **Generator Behavior**: The generator will still create the implementation for non-public constructors, but the code will fail at runtime when DI attempts to instantiate your model.

## Severity

**Error** - This diagnostic will block compilation and must be fixed before the code can build successfully.

## Related Diagnostics

- RXBG050: Partial constructor parameter type may not be registered in DI
- RXBG051: DI service scope violation

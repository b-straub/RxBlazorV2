# RXBG020: Partial Constructor Parameter Type May Not Be Registered in DI

## Description

This informational diagnostic is reported when a partial constructor parameter type cannot be detected as registered in the dependency injection container. This is a soft warning that helps identify potential DI configuration issues, but it may also appear for services that are correctly registered through means not detectable by static analysis.

## Cause

This warning occurs when:
- A partial constructor parameter is declared on an `ObservableModel`
- The parameter type is not detected in the DI registration analysis
- Static analysis cannot find an `AddSingleton`, `AddScoped`, or `AddTransient` call for this type

Note: This does NOT necessarily mean the service is actually unregistered. It may be registered through:
- Interface-based registrations where the implementation type differs
- Factory methods
- Extension methods from third-party libraries
- Runtime or conditional registrations

## How to Fix

### If the service is actually unregistered:

Register the service in your DI configuration:

```csharp
// In Program.cs or Startup.cs
builder.Services.AddSingleton<IValidationService, ValidationService>();
// or
builder.Services.AddScoped<MyService>();
// or
builder.Services.AddTransient<TemporaryService>();
```

### If the service is already registered:

This warning can be safely ignored if:
- The service is registered via an interface (`AddSingleton<IService, Implementation>()`)
- The service is registered by a third-party extension method
- The service is conditionally registered at runtime

## Examples

### Example 1: Unregistered Service (Actual Issue)

```csharp
// ❌ WRONG - IValidationService not registered in DI
public partial class ValidationModel : ObservableModel
{
    public bool IsValid => ValidationService.IsValid();

    // Warning: IValidationService may not be registered
    public partial ValidationModel(IValidationService validationService);
}

// No DI registration for IValidationService found in Program.cs
```

**Fix:**

```csharp
// ✅ CORRECT - Register the service
// In Program.cs:
builder.Services.AddSingleton<IValidationService, ValidationService>();

// Now the warning should disappear
public partial class ValidationModel : ObservableModel
{
    public bool IsValid => ValidationService.IsValid();

    public partial ValidationModel(IValidationService validationService);
}
```

### Example 2: Interface Registration (False Positive - Safe to Ignore)

```csharp
// In Program.cs:
builder.Services.AddSingleton<IValidationService, ValidationService>();
//                              ^interface        ^implementation

// Warning appears because static analysis searches for
// "ValidationService" but finds "IValidationService" registration instead
public partial class ValidationModel : ObservableModel
{
    // Warning: ValidationService may not be registered
    // This is safe to ignore - it's registered via interface
    public partial ValidationModel(ValidationService validationService);
}
```

**Better approach:**

```csharp
// ✅ BEST PRACTICE - Use interface in constructor
public partial class ValidationModel : ObservableModel
{
    // No warning - IValidationService is detected
    public partial ValidationModel(IValidationService validationService);
}
```

### Example 3: Third-Party Extension Method (Safe to Ignore)

```csharp
// In Program.cs:
builder.Services.AddMudServices();  // Registers MudBlazor services

// This warning can be safely ignored - MudBlazor services
// are registered by AddMudServices() extension method
public partial class MyModel : ObservableModel
{
    // Warning: IDialogService may not be registered
    // Safe to ignore - registered by MudBlazor
    public partial MyModel(IDialogService dialogService);
}
```

### Example 4: Multiple Dependencies

```csharp
// In Program.cs:
builder.Services.AddSingleton<IUserService, UserService>();
// Note: ILogger is registered by default ASP.NET Core infrastructure

public partial class UserModel : ObservableModel
{
    // No warning for IUserService - detected
    // Warning for ILogger - registered by framework, safe to ignore
    public partial UserModel(IUserService userService, ILogger<UserModel> logger);
}
```

## Why This Matters

This diagnostic helps catch configuration issues early:
- **Prevents runtime errors**: Missing DI registrations cause exceptions when the model is instantiated
- **Improves maintainability**: Helps ensure all dependencies are properly configured
- **Documentation**: Serves as a reminder of what needs to be registered

However, being informational level means:
- **Build succeeds**: Your code will still compile
- **No blocking**: You can deploy code with this warning
- **Trust your testing**: If your app runs correctly, the service is likely registered

## Severity

**Info** - This is an informational warning that won't block compilation. It's a hint that you should verify your DI configuration.

## Related Diagnostics

- RXBG021: DI service scope violation

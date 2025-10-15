# RXBG051: DI Service Scope Violation

## Description

This diagnostic is reported when an `ObservableModel` injects a service with an incompatible dependency injection scope. This violation can lead to runtime issues such as captive dependencies, premature disposal, or unexpected behavior.

## Cause

This error occurs when DI scoping rules are violated:

### DI Scoping Rules

1. **Singleton services** can only inject **Singleton** services
2. **Scoped services** can inject **Singleton** or **Scoped** services
3. **Transient services** can inject services of **any scope**

### Common Violations

- **Captive Dependency**: A Singleton service injecting a Scoped or Transient service
  - The short-lived service gets "captured" by the long-lived singleton
  - The service lives longer than intended, causing stale data or memory leaks

- **Premature Disposal**: A Scoped service injecting a Transient service
  - May cause disposal ordering issues
  - Generally discouraged but not as severe as captive dependencies

## How to Fix

### Option 1: Change Model Scope

Match your model's scope to the required service scope:

```csharp
// Change from Singleton to Scoped
[ObservableModelScope(ModelScope.Scoped)]  // Was: Singleton
public partial class MyModel : ObservableModel
{
    public partial MyModel(IScopedService service);
}
```

### Option 2: Change Service Scope

Register the service with a longer lifetime:

```csharp
// Change from Scoped to Singleton
builder.Services.AddSingleton<IMyService, MyService>();  // Was: AddScoped
```

### Option 3: Use Service Locator Pattern

For legitimate cases where you need a shorter-lived service:

```csharp
[ObservableModelScope(ModelScope.Singleton)]
public partial class MyModel : ObservableModel
{
    public partial MyModel(IServiceProvider serviceProvider);

    public void DoWork()
    {
        // Create a scope and resolve the service when needed
        using var scope = ServiceProvider.CreateScope();
        var scopedService = scope.ServiceProvider.GetRequiredService<IScopedService>();
        scopedService.DoSomething();
    }
}
```

## Examples

### Example 1: Captive Dependency (Critical Issue)

```csharp
// ❌ WRONG - Singleton capturing a Scoped service
// In Program.cs:
builder.Services.AddScoped<IUserService, UserService>();

[ObservableModelScope(ModelScope.Singleton)]
public partial class DashboardModel : ObservableModel
{
    // Error: Singleton model cannot inject Scoped service
    // This creates a captive dependency!
    public partial DashboardModel(IUserService userService);

    public string GetCurrentUser() => UserService.GetCurrentUserName();
}

// Problem: UserService is meant to be per-request, but the singleton
// DashboardModel will hold the same instance for the app's lifetime.
// Different users will see wrong data!
```

**Fix Option 1: Change model to Scoped**

```csharp
// ✅ CORRECT - Scoped model can inject Scoped service
[ObservableModelScope(ModelScope.Scoped)]  // Changed from Singleton
public partial class DashboardModel : ObservableModel
{
    public partial DashboardModel(IUserService userService);

    public string GetCurrentUser() => UserService.GetCurrentUserName();
}
```

**Fix Option 2: Change service to Singleton (if appropriate)**

```csharp
// ✅ CORRECT - Singleton model injects Singleton service
// In Program.cs:
builder.Services.AddSingleton<IUserService, UserService>();  // Changed from Scoped

[ObservableModelScope(ModelScope.Singleton)]
public partial class DashboardModel : ObservableModel
{
    public partial DashboardModel(IUserService userService);

    public string GetCurrentUser() => UserService.GetCurrentUserName();
}

// Note: Only do this if UserService truly has no per-request state
```

**Fix Option 3: Use IServiceProvider**

```csharp
// ✅ CORRECT - Use service locator pattern
[ObservableModelScope(ModelScope.Singleton)]
public partial class DashboardModel : ObservableModel
{
    public partial DashboardModel(IServiceProvider serviceProvider);

    public string GetCurrentUser()
    {
        // Resolve the scoped service when needed
        using var scope = ServiceProvider.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        return userService.GetCurrentUserName();
    }
}
```

### Example 2: Scoped with Transient (Less Critical)

```csharp
// ⚠️ WARNING - Scoped service injecting Transient
// In Program.cs:
builder.Services.AddTransient<IEmailService, EmailService>();

[ObservableModelScope(ModelScope.Scoped)]
public partial class NotificationModel : ObservableModel
{
    // Warning: Scoped services should not depend on Transient services
    // May cause disposal ordering issues
    public partial NotificationModel(IEmailService emailService);
}
```

**Fix: Change Transient to Scoped**

```csharp
// ✅ CORRECT - Scoped model injects Scoped service
// In Program.cs:
builder.Services.AddScoped<IEmailService, EmailService>();  // Changed from Transient

[ObservableModelScope(ModelScope.Scoped)]
public partial class NotificationModel : ObservableModel
{
    public partial NotificationModel(IEmailService emailService);
}
```

### Example 3: Valid Transient Usage

```csharp
// ✅ CORRECT - Transient can inject anything
// In Program.cs:
builder.Services.AddSingleton<ILogger, Logger>();
builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddTransient<IValidator, Validator>();

[ObservableModelScope(ModelScope.Transient)]
public partial class FormModel : ObservableModel
{
    // No warnings - Transient can inject any scope
    public partial FormModel(
        ILogger logger,           // Singleton - OK
        IUserContext userContext, // Scoped - OK
        IValidator validator);    // Transient - OK
}
```

### Example 4: Multiple Dependencies

```csharp
// ❌ WRONG - Mixed scope violations
// In Program.cs:
builder.Services.AddSingleton<ILogger, Logger>();
builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddTransient<IValidator, Validator>();

[ObservableModelScope(ModelScope.Singleton)]
public partial class AppModel : ObservableModel
{
    // Multiple violations:
    // - IUserContext: Error (Singleton cannot inject Scoped)
    // - IValidator: Error (Singleton cannot inject Transient)
    // - ILogger: OK (Singleton can inject Singleton)
    public partial AppModel(
        ILogger logger,           // OK
        IUserContext userContext, // ❌ Captive dependency!
        IValidator validator);    // ❌ Captive dependency!
}
```

**Fix:**

```csharp
// ✅ CORRECT - All Singleton dependencies
// In Program.cs:
builder.Services.AddSingleton<ILogger, Logger>();
builder.Services.AddSingleton<IUserContext, UserContext>();      // Changed from Scoped
builder.Services.AddSingleton<IValidator, Validator>();          // Changed from Transient

[ObservableModelScope(ModelScope.Singleton)]
public partial class AppModel : ObservableModel
{
    // No warnings - all Singleton dependencies
    public partial AppModel(
        ILogger logger,
        IUserContext userContext,
        IValidator validator);
}
```

## Why This Matters

### Captive Dependencies

Captive dependencies are serious issues:
- **Stale data**: A singleton holding a scoped service will have outdated per-request data
- **Memory leaks**: Services that should be disposed aren't disposed properly
- **Concurrency issues**: Scoped services aren't designed for multi-user concurrent access
- **Unpredictable behavior**: Violates the service's intended lifetime contract

### Best Practices

1. **Singleton models should only inject Singleton services**
   - Singleton services are shared across the entire application lifetime
   - Use IServiceProvider if you truly need shorter-lived dependencies

2. **Scoped models for per-request state**
   - Use Scoped for services that need per-user or per-request isolation
   - Common in Blazor Server (per-circuit scope)

3. **Transient for stateless operations**
   - Use Transient for lightweight, stateless services
   - Each consumer gets a new instance

## Severity

**Warning** - This indicates a design issue that will likely cause runtime problems. Fix it before deploying to production.

## Related Diagnostics

- RXBG050: Partial constructor parameter type may not be registered in DI
- RXBG051: Shared model not singleton

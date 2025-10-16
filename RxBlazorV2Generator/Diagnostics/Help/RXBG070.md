# RXBG070: ObservableModel is Missing ObservableModelScope Attribute

## Description

This warning is reported when an `ObservableModel` class does not have an explicit `[ObservableModelScope]` attribute. While a default scope of `Scoped` is used, it is recommended to explicitly specify the scope for clarity and maintainability.

## Cause

This warning occurs when:
- A class inherits from `ObservableModel`
- The class does not have an `[ObservableModelScope]` attribute
- The default scope (Scoped) is being applied implicitly

## How to Fix

Add an `[ObservableModelScope]` attribute to your ObservableModel class, specifying the appropriate scope:

```csharp
// ✅ CORRECT - Explicitly specify scope
[ObservableModelScope(ModelScope.Scoped)]
public partial class MyModel : ObservableModel
{
    // Model implementation
}
```

### Choosing the Right Scope

**Scoped** (Default - Most Common):
- New instance per HTTP request in server-side Blazor
- New instance per circuit in Blazor Server
- New instance per scope in Blazor WebAssembly
- Best for most UI models

**Singleton**:
- Single instance shared across the entire application
- Use for application-wide state or shared services
- Must be thread-safe

**Transient**:
- New instance every time it's injected
- Rarely used for ObservableModels
- Use for lightweight, stateless models

## Examples

### Example 1: Missing Attribute (Warning)

```csharp
// ⚠️ WARNING - No explicit scope attribute
public partial class UserModel : ObservableModel
{
    public partial string Name { get; set; }
}
// Default Scoped scope is applied, but not obvious to developers
```

**Fix:**

```csharp
// ✅ CORRECT - Explicitly specify scope
[ObservableModelScope(ModelScope.Scoped)]
public partial class UserModel : ObservableModel
{
    public partial string Name { get; set; }
}
```

### Example 2: Application-Wide State

```csharp
// ✅ CORRECT - Singleton for shared application state
[ObservableModelScope(ModelScope.Singleton)]
public partial class AppStateModel : ObservableModel
{
    public partial bool IsDarkMode { get; set; }
    public partial string CurrentUser { get; set; }
}
```

### Example 3: Per-Request Model

```csharp
// ✅ CORRECT - Scoped for per-request data
[ObservableModelScope(ModelScope.Scoped)]
public partial class OrderFormModel : ObservableModel
{
    public partial decimal TotalAmount { get; set; }
    public partial List<OrderItem> Items { get; set; }
}
```

### Example 4: Transient for Lightweight Operations

```csharp
// ✅ CORRECT - Transient for lightweight, stateless operations
[ObservableModelScope(ModelScope.Transient)]
public partial class ValidationHelperModel : ObservableModel
{
    public partial bool IsValid { get; set; }
}
```

## Why This Matters

Explicitly declaring the scope improves code quality:
- **Clarity**: Other developers immediately understand the lifetime
- **Intentionality**: Shows the scope was chosen deliberately, not by accident
- **Documentation**: Self-documents the model's lifetime behavior
- **Maintainability**: Prevents confusion when the default changes

## Default Behavior

Without the attribute, the model defaults to `ModelScope.Scoped`. This is equivalent to:

```csharp
// These are equivalent:
public partial class MyModel : ObservableModel { }

[ObservableModelScope(ModelScope.Scoped)]
public partial class MyModel : ObservableModel { }
```

## Severity

**Warning** - This diagnostic won't block compilation, but it's recommended to fix it for code clarity.

## Related Diagnostics

- RXBG014: Shared model (used in multiple components) must be Singleton
- RXBG051: DI service scope violation

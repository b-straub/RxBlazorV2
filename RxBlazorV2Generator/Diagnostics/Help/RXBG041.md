# RXBG041: ObservableComponentTrigger Attribute Has No Effect

## Description

This diagnostic is reported when a property has `[ObservableComponentTrigger]` or `[ObservableComponentTriggerAsync]` attributes, but the model neither has `[ObservableComponent]` attribute nor is referenced by another model with `[ObservableComponent(includeReferencedTriggers: true)]`. In this case, the trigger attributes will be ignored and no hook methods will be generated.

## Cause

This warning occurs when:

1. A property in an `ObservableModel` has `[ObservableComponentTrigger]` or `[ObservableComponentTriggerAsync]` attributes
2. The model does NOT have `[ObservableComponent]` attribute
3. The model is NOT referenced by another model via partial constructor parameter where that other model has `[ObservableComponent(includeReferencedTriggers: true)]`

### Why This Is a Problem

Component triggers only generate hook methods in two scenarios:
1. **Direct component generation**: Model has `[ObservableComponent]` → generates hooks in its own component
2. **Referenced model triggers**: Model is referenced by another model with `[ObservableComponent(includeReferencedTriggers: true)]` → generates hooks in the referencing model's component

Without either scenario, the trigger attributes are just decoration with no functional effect.

## How to Fix

### Option 1: Add [ObservableComponent] to the Model (Recommended)

If this model should have its own component with trigger hooks, add the `[ObservableComponent]` attribute:

```csharp
// Before: ❌ Triggers have no effect
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial bool IsDarkMode { get; set; }
}

// After: ✅ Triggers generate hooks in SettingsModelComponent
[ObservableComponent]
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial bool IsDarkMode { get; set; }
}

// Generated component:
// SettingsModelComponent.g.cs
public abstract partial class SettingsModelComponent : ObservableComponent<SettingsModel>
{
    protected virtual void OnIsDarkModeChanged() { }
}

// Usage in razor file:
// Settings.razor
@inherits SettingsModelComponent

@code {
    protected override void OnIsDarkModeChanged()
    {
        // Called when Model.IsDarkMode changes
        Console.WriteLine($"Dark mode: {Model.IsDarkMode}");
    }
}
```

### Option 2: Reference from Another Model with [ObservableComponent]

If this model should be used via composition, have another model reference it:

```csharp
// ✅ SettingsModel doesn't need [ObservableComponent]
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial bool IsDarkMode { get; set; }
}

// ✅ ThemeModel references SettingsModel and has [ObservableComponent]
[ObservableComponent]  // includeReferencedTriggers: true by default
public partial class ThemeModel : ObservableModel
{
    public partial ThemeModel(SettingsModel settings);

    // Can use Settings.IsDarkMode
    public partial string CurrentTheme { get; set; }
}

// Generated component:
// ThemeModelComponent.g.cs
public abstract partial class ThemeModelComponent : ObservableComponent<ThemeModel>
{
    // Hook generated for referenced model's trigger
    protected virtual void OnSettingsIsDarkModeChanged() { }
}

// Usage:
// Theme.razor
@inherits ThemeModelComponent

@code {
    protected override void OnSettingsIsDarkModeChanged()
    {
        // Called when Model.Settings.IsDarkMode changes
        Model.CurrentTheme = Model.Settings.IsDarkMode ? "Dark" : "Light";
    }
}
```

### Option 3: Remove the Trigger Attributes

If you don't need reactive hooks for these properties, remove the attributes:

```csharp
// ✅ No trigger attributes → no warning
public partial class SettingsModel : ObservableModel
{
    public partial bool IsDarkMode { get; set; }
    public partial string Theme { get; set; }
}
```

You can still observe changes manually using the `Observable` property:

```csharp
[ObservableComponent]
public partial class ThemeModel : ObservableModel
{
    public partial ThemeModel(SettingsModel settings);

    protected override void InitializeGeneratedCode()
    {
        base.InitializeGeneratedCode();

        // Manual subscription to Settings.IsDarkMode
        Subscriptions.Add(Settings.Observable
            .Where(p => p.Intersect(["Model.IsDarkMode"]).Any())
            .Subscribe(_ => OnSettingsChanged()));
    }

    private void OnSettingsChanged()
    {
        // Handle settings changes manually
    }
}
```

## Examples

### Example 1: Unused Trigger (Warning)

```csharp
// ❌ Warning RXBG041: Property 'IsDarkMode' has trigger but model has no [ObservableComponent]
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial bool IsDarkMode { get; set; }

    [ObservableComponentTriggerAsync]
    public partial string Theme { get; set; }
}

// No component is generated
// No other model references this model
// → Trigger attributes have no effect
```

**Warning Messages:**
```
RXBG041: Property 'IsDarkMode' in model 'SettingsModel' has [ObservableComponentTrigger] or
[ObservableComponentTriggerAsync] attribute, but this model neither has [ObservableComponent]
attribute nor is referenced by a model with [ObservableComponent(includeReferencedTriggers: true)].
The trigger attribute will be ignored and no hook methods will be generated.

RXBG041: Property 'Theme' in model 'SettingsModel' has [ObservableComponentTrigger] or
[ObservableComponentTriggerAsync] attribute, but this model neither has [ObservableComponent]
attribute nor is referenced by a model with [ObservableComponent(includeReferencedTriggers: true)].
The trigger attribute will be ignored and no hook methods will be generated.
```

### Example 2: Fix with [ObservableComponent]

```csharp
// ✅ No warning - triggers generate hooks
[ObservableComponent]
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial bool IsDarkMode { get; set; }

    [ObservableComponentTriggerAsync]
    public partial string Theme { get; set; }
}

// Generated: SettingsModelComponent.g.cs
public abstract partial class SettingsModelComponent : ObservableComponent<SettingsModel>
{
    protected virtual void OnIsDarkModeChanged() { }
    protected virtual Task OnThemeChangedAsync(CancellationToken ct) => Task.CompletedTask;
}
```

### Example 3: Fix with Model Reference

```csharp
// ✅ No warning - SettingsModel is referenced by ThemeModel
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial bool IsDarkMode { get; set; }
}

[ObservableComponent]  // includeReferencedTriggers: true (default)
public partial class ThemeModel : ObservableModel
{
    public partial ThemeModel(SettingsModel settings);
}

// Generated: ThemeModelComponent.g.cs
public abstract partial class ThemeModelComponent : ObservableComponent<ThemeModel>
{
    // Hook for referenced model trigger
    protected virtual void OnSettingsIsDarkModeChanged() { }
}
```

### Example 4: Multiple Models with Mixed Scenarios

```csharp
// ❌ Warning RXBG041 - no [ObservableComponent], not referenced
public partial class UnusedModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial int Value { get; set; }
}

// ✅ No warning - has [ObservableComponent]
[ObservableComponent]
public partial class DirectComponentModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial string Name { get; set; }
}

// ✅ No warning - referenced by AppModel
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial bool IsEnabled { get; set; }
}

// ✅ Uses SettingsModel triggers
[ObservableComponent]
public partial class AppModel : ObservableModel
{
    public partial AppModel(SettingsModel settings);
    // Generates: OnSettingsIsEnabledChanged() hook
}
```

### Example 5: Reference with includeReferencedTriggers: false

```csharp
// ❌ Warning RXBG041 - referenced but includeReferencedTriggers: false
public partial class ConfigModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial string ApiUrl { get; set; }
}

// References ConfigModel but disables trigger propagation
[ObservableComponent(includeReferencedTriggers: false)]
public partial class ApiModel : ObservableModel
{
    public partial ApiModel(ConfigModel config);
    // No OnConfigApiUrlChanged() hook is generated
}

// Fix: Either enable includeReferencedTriggers or remove trigger from ConfigModel
```

## Common Patterns

### Pattern 1: Standalone Component Model

```csharp
// Model with its own component
[ObservableComponent]
public partial class CounterModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial int Count { get; set; }
}
```

**Use when:**
- Model has a dedicated UI component
- Model is not composed with other models
- Direct user interaction with this model

### Pattern 2: Shared/Referenced Model

```csharp
// Shared model referenced by multiple components
public partial class UserProfileModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial string UserName { get; set; }
}

[ObservableComponent]
public partial class HeaderModel : ObservableModel
{
    public partial HeaderModel(UserProfileModel profile);
}

[ObservableComponent]
public partial class DashboardModel : ObservableModel
{
    public partial DashboardModel(UserProfileModel profile);
}
```

**Use when:**
- Model is shared across multiple components
- No dedicated UI component for the model itself
- Used via composition pattern

### Pattern 3: Hybrid Approach

```csharp
// Model has both its own component AND is referenced by others
[ObservableComponent]
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial bool IsDarkMode { get; set; }
}

// Other models can also reference it
[ObservableComponent]
public partial class ThemeModel : ObservableModel
{
    public partial ThemeModel(SettingsModel settings);
    // Gets OnSettingsIsDarkModeChanged() hook
}

// Can use SettingsModelComponent directly
// Settings.razor
@inherits SettingsModelComponent
```

**Use when:**
- Model needs both standalone component and composition usage
- Complex application with flexible component architecture

## Understanding Hook Generation

### Direct Component Hooks

```csharp
[ObservableComponent]
public partial class Model : ObservableModel
{
    [ObservableComponentTrigger]
    public partial string Name { get; set; }
}

// Generates in ModelComponent:
protected virtual void OnNameChanged() { }
```

### Referenced Model Hooks

```csharp
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial bool IsDay { get; set; }
}

[ObservableComponent]
public partial class WeatherModel : ObservableModel
{
    public partial WeatherModel(SettingsModel settings);
}

// Generates in WeatherModelComponent:
protected virtual void OnSettingsIsDayChanged() { }
```

**Hook naming pattern:** `On{ReferencedProperty}{TriggerProperty}Changed[Async]`

## Severity

**Warning** - The code will compile but trigger attributes will be silently ignored, which may lead to confusion about why hooks are not being called.

## Related Diagnostics

- RXBG052: Referenced model with triggers must be in same assembly
- RXBG012: Unused model reference (when referenced model has no used properties)

## See Also

- `[ObservableComponent]` attribute documentation
- `[ObservableComponentTrigger]` attribute documentation
- `[ObservableComponentTriggerAsync]` attribute documentation
- Component trigger hooks guide
- Model composition patterns

## Notes

- This diagnostic currently only checks if the model has `[ObservableComponent]` attribute
- It cannot easily detect if the model is referenced by another model at compile time
- If you're certain the model is referenced by another model with `includeReferencedTriggers: true`, you can ignore this warning
- However, it's good practice to verify that the expected hooks are actually being generated in the referencing component
- Consider adding `[ObservableComponent]` for better clarity even if the model is referenced

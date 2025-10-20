# RXBG052: Referenced Model with Triggers Must Be in Same Assembly

## Description

This diagnostic is reported when an `ObservableModel` with `[ObservableComponent(includeReferencedTriggers: true)]` references another `ObservableModel` from a different assembly that has `[ObservableComponentTrigger]` attributes. Due to source generator limitations, cross-assembly trigger generation is not supported.

## Cause

This error occurs when:

1. Your model has `[ObservableComponent]` attribute with `includeReferencedTriggers: true` (default)
2. The model references another `ObservableModel` via partial constructor parameter
3. The referenced model is in a **different assembly**
4. The referenced model has properties marked with `[ObservableComponentTrigger]` or `[ObservableComponentTriggerAsync]`

### Why This Limitation Exists

Source generators run during compilation and can only analyze source code in the current assembly. When a referenced model is in a different assembly:
- It's already compiled into a DLL
- Source code and attribute information are not available to the generator
- The generator cannot detect which properties have trigger attributes
- Hook methods cannot be generated for referenced model triggers

## How to Fix

### Option 1: Move Referenced Model to Same Assembly (Recommended)

Move the referenced `ObservableModel` to the same assembly as your component model:

```csharp
// Before: Models in different assemblies
// Assembly: MyApp.Core
[ObservableComponent]
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial bool IsDarkMode { get; set; }
}

// Assembly: MyApp.UI  ❌ Different assembly!
[ObservableComponent]  // includeReferencedTriggers defaults to true
public partial class ThemeModel : ObservableModel
{
    // Error RXBG052: SettingsModel is in different assembly
    public partial ThemeModel(SettingsModel settings);
}

// ✅ Solution: Move both models to same assembly
// Assembly: MyApp.UI
[ObservableComponent]
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial bool IsDarkMode { get; set; }
}

[ObservableComponent]
public partial class ThemeModel : ObservableModel
{
    public partial ThemeModel(SettingsModel settings);
    // Now generates: OnThemeSettingsIsDarkModeChanged() hook
}
```

### Option 2: Disable Cross-Model Triggers

Set `includeReferencedTriggers: false` to disable automatic trigger propagation:

```csharp
// ✅ Solution: Disable the feature
[ObservableComponent(includeReferencedTriggers: false)]
public partial class ThemeModel : ObservableModel
{
    // No cross-assembly error - feature is disabled
    public partial ThemeModel(SettingsModel settings);

    // You can still observe Settings manually:
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

### Option 3: Create Shared Library

Organize your code with a shared model library:

```csharp
// ✅ Solution: Create SharedModels assembly
// Assembly: MyApp.SharedModels
[ObservableComponent]
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial bool IsDarkMode { get; set; }
}

[ObservableComponent]
public partial class ThemeModel : ObservableModel
{
    public partial ThemeModel(SettingsModel settings);
}

// Assembly: MyApp.UI
// Reference SharedModels assembly
// All generated code is available in SharedModels.dll
```

## Examples

### Example 1: Cross-Assembly Reference (Error)

```csharp
// Assembly: RxBlazorVSSampleComponents
[ObservableComponent]
[ObservableModelScope(ModelScope.Singleton)]
public partial class ErrorModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial string Message { get; set; }
}

// Assembly: MyApp  ❌ Different assembly!
[ObservableComponent]  // includeReferencedTriggers = true (default)
public partial class NotificationModel : ObservableModel
{
    // ❌ Error RXBG052: ErrorModel is in RxBlazorVSSampleComponents,
    // but NotificationModel is in MyApp
    public partial NotificationModel(ErrorModel errorModel);
}
```

**Error Message:**
```
RXBG052: ObservableModel 'NotificationModel' with [ObservableComponent(includeReferencedTriggers: true)]
references model 'ErrorModel' from assembly 'RxBlazorVSSampleComponents', but the referenced model is not
in the same assembly ('MyApp'). Referenced models with [ObservableComponentTrigger] attributes must be in
the same assembly to generate trigger hooks. Either move 'ErrorModel' to assembly 'MyApp', or set
[ObservableComponent(includeReferencedTriggers: false)] to disable this feature.
```

**Fix 1: Disable Feature**
```csharp
// ✅ Disable cross-model triggers
[ObservableComponent(includeReferencedTriggers: false)]
public partial class NotificationModel : ObservableModel
{
    public partial NotificationModel(ErrorModel errorModel);
}
```

**Fix 2: Move to Same Assembly**
```csharp
// ✅ Move both models to MyApp assembly
// Assembly: MyApp
[ObservableComponent]
public partial class ErrorModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial string Message { get; set; }
}

[ObservableComponent]
public partial class NotificationModel : ObservableModel
{
    public partial NotificationModel(ErrorModel errorModel);
    // Generates: OnNotificationErrorModelMessageChanged() hook
}
```

### Example 2: Same Assembly (Valid)

```csharp
// ✅ Both models in same assembly - works correctly
// Assembly: MyApp
[ObservableComponent]
public partial class CounterModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial int Count { get; set; }
}

[ObservableComponent]
public partial class DashboardModel : ObservableModel
{
    public partial DashboardModel(CounterModel counter);
    // ✅ Generates: OnDashboardCounterCountChanged() hook
}

// In DashboardModelComponent.razor
@inherits DashboardModelComponent

@code {
    protected override void OnDashboardCounterCountChanged()
    {
        // Called when Counter.Count changes
        Console.WriteLine($"Counter changed: {Model.Counter.Count}");
    }
}
```

### Example 3: Multiple Referenced Models

```csharp
// ✅ All in same assembly
// Assembly: MyApp.Features
[ObservableComponent]
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial bool IsDarkMode { get; set; }
}

[ObservableComponent]
public partial class UserModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial string UserName { get; set; }
}

[ObservableComponent]
public partial class AppModel : ObservableModel
{
    public partial AppModel(SettingsModel settings, UserModel user);
    // ✅ Generates hooks for both:
    // - OnAppSettingsIsDarkModeChanged()
    // - OnAppUserUserNameChanged()
}
```

### Example 4: Mixed Scenarios

```csharp
// Assembly: CoreLibrary
[ObservableComponent]
public partial class CoreModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial int CoreValue { get; set; }
}

// Assembly: MyApp
[ObservableComponent]
public partial class LocalModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial string LocalValue { get; set; }
}

// ❌ Error: CoreModel is from different assembly
[ObservableComponent]
public partial class MixedModel : ObservableModel
{
    public partial MixedModel(
        CoreModel core,    // ❌ Error RXBG052
        LocalModel local); // ✅ OK - same assembly
}

// ✅ Fix: Disable cross-assembly triggers
[ObservableComponent(includeReferencedTriggers: false)]
public partial class MixedModel : ObservableModel
{
    public partial MixedModel(CoreModel core, LocalModel local);

    // No automatic hooks, but you can observe manually
    protected override void InitializeGeneratedCode()
    {
        base.InitializeGeneratedCode();

        // Manual subscription to CoreModel
        Subscriptions.Add(Core.Observable
            .Where(p => p.Intersect(["Model.CoreValue"]).Any())
            .Subscribe(_ => OnCoreChanged()));
    }
}
```

## Hook Method Naming Convention

When `includeReferencedTriggers: true` and models are in the same assembly, hook methods follow this pattern:

```
On{ReferencedProperty}{TriggerProperty}Changed[Async]
```

**Example:**
```csharp
// WeatherModel references SettingsModel
[ObservableComponent]
public partial class WeatherModel : ObservableModel
{
    public partial WeatherModel(SettingsModel settings);
    //                          ^               ^
    //                          Property name   Model name
}

[ObservableComponent]
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial bool IsDay { get; set; }
    //                      ^
    //                      Trigger property
}

// Generated hook in WeatherModelComponent:
// On + Settings + IsDay + Changed
protected virtual void OnSettingsIsDayChanged() { }
protected virtual Task OnSettingsIsDayChangedAsync(CancellationToken ct)
    => Task.CompletedTask;
```

## When to Use Each Option

### Use Option 1 (Move to Same Assembly)
- ✅ You control both model codebases
- ✅ Models are logically related
- ✅ You want automatic trigger propagation
- ✅ Easier maintenance with automatic hook generation

### Use Option 2 (Disable Feature)
- ✅ Referenced model is in a third-party library
- ✅ You don't need trigger propagation
- ✅ You prefer manual observable subscriptions
- ✅ You want more control over subscription logic

### Use Option 3 (Shared Library)
- ✅ Large application with multiple UI assemblies
- ✅ Models are shared across multiple apps
- ✅ Clean architecture with separate concerns
- ✅ You want reusable model components

## Severity

**Error** - This prevents code generation for cross-assembly trigger hooks and must be resolved.

## Related Diagnostics

- RXBG014: Shared model not singleton
- RXBG013: Cannot reference derived ObservableModel

## See Also

- `[ObservableComponent]` attribute documentation
- `[ObservableComponentTrigger]` attribute documentation
- Model composition patterns
- Component trigger hooks

## Testing Notes

This diagnostic is difficult to test in unit tests because it requires a multi-assembly compilation scenario where:
1. Assembly A contains a model with `[ObservableComponentTrigger]` attributes
2. Assembly B references Assembly A and tries to use `includeReferencedTriggers: true`

**Current Test Coverage:**
- ✅ Same-assembly scenarios are fully tested (see `ReferencedModelTriggerTests.cs`)
- ✅ Feature flag behavior (`includeReferencedTriggers: true/false`) is tested
- ✅ Hook generation for same-assembly references is verified
- ❌ Actual cross-assembly error (RXBG052) requires integration tests

**To Verify This Diagnostic:**
Create a manual integration test with two projects:
```csharp
// Project: MyApp.Core
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial bool IsDay { get; set; }
}

// Project: MyApp.UI (references MyApp.Core)
[ObservableComponent]  // Error RXBG052!
public partial class WeatherModel : ObservableModel
{
    public partial WeatherModel(SettingsModel settings);
}
```

**Why Unit Testing Is Not Feasible:**
- Source generator test infrastructure compiles all code in a single assembly
- Cannot simulate inter-assembly references with attribute metadata
- Would require complex MSBuild integration tests with actual project references

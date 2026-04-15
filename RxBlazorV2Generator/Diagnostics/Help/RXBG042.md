# RXBG042: Redundant ObservableComponentTrigger on Razor-Observed Property

## Description

This diagnostic is reported when a property has `[ObservableComponentTrigger]` or `[ObservableComponentTriggerAsync]` with `RenderOnly` or `RenderAndHook` behavior, but the property is already referenced in the corresponding razor file. The component already re-renders automatically when this property changes via the generated `Filter()` method, making the trigger attribute redundant.

## Cause

This error occurs when:

1. A property has `[ObservableComponentTrigger]` with default (`RenderAndHook`) or `RenderOnly` behavior
2. The same property is already used in the razor file (e.g., `@Model.PropertyName`)
3. The component's `Filter()` method already includes this property for automatic re-rendering

### Why This Is an Error

When a property is referenced in a razor file, the source generator automatically includes it in the `Filter()` method. This means the component already re-renders when the property changes. Adding a trigger attribute with rendering behavior is redundant and misleading:

- **RenderOnly**: Completely redundant — the razor reference already triggers re-renders
- **RenderAndHook**: The rendering part is redundant — use `HookOnly` if you need the hook method

## How to Fix

### Option 1: Remove the Trigger Attribute (Recommended)

If you only need the component to re-render when the property changes, the razor reference is sufficient:

```csharp
// Before: Redundant trigger
[ObservableComponent]
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]  // RXBG042: Redundant
    public partial bool IsDay { get; set; }
}

// Razor file already references the property:
// @if (Model.IsDay) { <span>Day</span> }

// After: Remove the trigger — razor reference handles re-rendering
[ObservableComponent]
public partial class SettingsModel : ObservableModel
{
    public partial bool IsDay { get; set; }
}
```

### Option 2: Use HookOnly Behavior

If you need a code-behind hook method in addition to the razor rendering, use `HookOnly`:

```csharp
// Before: Redundant RenderAndHook
[ObservableComponent]
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]  // RXBG042: RenderAndHook is redundant
    public partial bool IsDay { get; set; }
}

// After: HookOnly — rendering comes from razor, hook from trigger
[ObservableComponent]
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger(triggerBehavior: TriggerBehavior.HookOnly)]
    public partial bool IsDay { get; set; }
}

// In component code-behind:
protected override void OnIsDayChanged()
{
    // Custom logic when IsDay changes
}
```

### Option 3: Use Razor Conditional Logic

Instead of a code-behind hook, handle the logic directly in the razor file:

```razor
@inherits SettingsModelComponent

@if (Model.IsDay)
{
    <MudIcon Icon="@Icons.Material.Filled.WbSunny" />
}
else
{
    <MudIcon Icon="@Icons.Material.Filled.DarkMode" />
}
```

## Severity

**Error** — The trigger attribute is provably redundant and should be removed or changed to `HookOnly`.

## Related Diagnostics

- RXBG041: ObservableComponentTrigger attribute has no effect (model lacks component)
- RXBG062: Component has no reactive properties or triggers

## See Also

- `TriggerBehavior` enum: `RenderAndHook`, `RenderOnly`, `HookOnly`
- `[ObservableComponentTrigger]` attribute documentation
- Component trigger hooks guide

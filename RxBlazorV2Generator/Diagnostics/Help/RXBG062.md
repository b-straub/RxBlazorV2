# RXBG062: Component has No Reactive Properties or Triggers

## Description

This diagnostic is reported when a Blazor component that inherits from an ObservableComponent does not use any model properties in its razor file AND has no ObservableComponentTrigger hooks defined. Such a component serves no reactive purpose and will never respond to model changes.

## Cause

This error occurs when:
- A `.razor` file inherits from a generated ObservableComponent class
- The razor file doesn't reference any properties from the model (e.g., `@Model.PropertyName`)
- The model has no properties marked with `[ObservableComponentTrigger]` or `[ObservableComponentTriggerAsync]`
- The component will never re-render in response to model changes

## How to Fix

Choose one of these solutions based on your needs:

### Solution 1: Use Model Properties in the Razor File

If you want automatic re-rendering when model properties change, reference them in your razor file:

```razor
@* Add property references *@
<p>Count: @Model.Count</p>
<p>Name: @Model.Name</p>
```

### Solution 2: Add Component Triggers

If you want manual control over rendering through trigger hooks, add trigger attributes to properties:

```csharp
[ObservableComponent]
public partial class MyModel : ObservableModel
{
    [ObservableComponentTrigger]  // ✅ Generates OnCountChanged() hook
    public partial int Count { get; set; }
}
```

Then implement the hook in your razor file:

```razor
@code {
    protected override void OnCountChanged()
    {
        // Custom logic when Count changes
        // Optionally call StateHasChanged() if you want re-render
    }
}
```

### Solution 3: Don't Use ObservableComponent

If the component doesn't need reactive behavior, don't inherit from ObservableComponent:

```razor
@* Remove @inherits directive or change to ComponentBase *@
@inherits ComponentBase

<h3>Static Content</h3>
```

## Examples

### Example 1: Non-Reactive Component (Wrong)

```razor
@* Counter.razor *@
@inherits CounterModelComponent

<h3>Counter</h3>
<p>This is a static page</p>
@* No model properties used! *@
```

```csharp
// CounterModel.cs
[ObservableComponent]
public partial class CounterModel : ObservableModel
{
    public partial int Count { get; set; }
    // No triggers defined
}
```

**Result**: RXBG062 error - component will never update

### Example 2: Fixed with Property Usage

```razor
@* Counter.razor *@
@inherits CounterModelComponent

<h3>Counter</h3>
<p>Count: @Model.Count</p>  @* ✅ Now uses model property *@
<button @onclick="() => Model.Count++">Increment</button>
```

```csharp
// CounterModel.cs (unchanged)
[ObservableComponent]
public partial class CounterModel : ObservableModel
{
    public partial int Count { get; set; }
}
```

**Result**: Component automatically re-renders when Count changes

### Example 3: Fixed with Triggers

```razor
@* Logger.razor *@
@inherits LoggerModelComponent

<h3>Logger</h3>
<p>Logs are being captured...</p>

@code {
    protected override void OnLogMessageChanged()
    {
        // ✅ Custom logic - log to console instead of re-rendering
        Console.WriteLine($"New log: {Model.LogMessage}");
        // Note: Not calling StateHasChanged() - no re-render
    }
}
```

```csharp
// LoggerModel.cs
[ObservableComponent]
public partial class LoggerModel : ObservableModel
{
    [ObservableComponentTrigger]  // ✅ Generates hook
    public partial string LogMessage { get; set; }
}
```

**Result**: Hook fires when LogMessage changes, no automatic re-render

### Example 4: Trigger with Selective Re-render

```razor
@* Dashboard.razor *@
@inherits DashboardModelComponent

<h3>Dashboard</h3>
<p>Status: @_status</p>

@code {
    private string _status = "Idle";

    protected override void OnDataRefreshedChanged()
    {
        // ✅ Update local state and trigger re-render
        _status = Model.DataRefreshed ? "Updated" : "Stale";
        StateHasChanged();  // Manual re-render
    }
}
```

```csharp
// DashboardModel.cs
[ObservableComponent]
public partial class DashboardModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial bool DataRefreshed { get; set; }
}
```

**Result**: Component only re-renders when you explicitly call StateHasChanged()

## Understanding Filter Behavior

The generated component code uses a `Filter()` method to determine reactivity:

### Empty Filter (No Properties, No Triggers) - ERROR
```csharp
protected override string[] Filter()
{
    return [];  // ❌ RXBG062: Nothing to observe
}
```
**Behavior**: Error - component is completely non-reactive

### Empty Filter (No Properties, Has Triggers) - OK
```csharp
protected override string[] Filter()
{
    return [];  // ✅ Trigger hooks will fire, no automatic StateHasChanged
}
```
**Behavior**: Only trigger hooks execute, no automatic re-rendering

### Non-Empty Filter (Has Properties) - OK
```csharp
protected override string[] Filter()
{
    return ["Model.Count", "Model.Name"];  // ✅ Observes these properties
}
```
**Behavior**: Automatic re-render when Count or Name changes

## Common Scenarios

### Scenario 1: Background Processing Component

If you need a component that performs background work without UI updates:

```csharp
[ObservableComponent]
public partial class BackgroundWorkerModel : ObservableModel
{
    [ObservableComponentTrigger]  // ✅ Use trigger for side effects
    public partial string JobStatus { get; set; }
}
```

```razor
@inherits BackgroundWorkerModelComponent

@code {
    protected override void OnJobStatusChanged()
    {
        // Perform background work without re-rendering UI
        ProcessJob(Model.JobStatus);
    }
}
```

### Scenario 2: Event Logging Component

```csharp
[ObservableComponent]
public partial class EventLoggerModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial string EventMessage { get; set; }
}
```

```razor
@inherits EventLoggerModelComponent

<h3>Event Logger Active</h3>

@code {
    protected override void OnEventMessageChanged()
    {
        LogToFile(Model.EventMessage);
        // No StateHasChanged() - just logging
    }
}
```

### Scenario 3: Multi-Property Display

```razor
@inherits DashboardModelComponent

<div>
    <p>Users: @Model.UserCount</p>
    <p>Orders: @Model.OrderCount</p>
    <p>Revenue: @Model.Revenue</p>
</div>
```

```csharp
[ObservableComponent]
public partial class DashboardModel : ObservableModel
{
    public partial int UserCount { get; set; }
    public partial int OrderCount { get; set; }
    public partial decimal Revenue { get; set; }
    // ✅ Properties used in razor - automatic re-render
}
```

## Why This Diagnostic Exists

This diagnostic prevents:

1. **Wasted Resources**: Non-reactive components shouldn't inherit from ObservableComponent
2. **Confusion**: Makes developer intent explicit
3. **Performance**: Avoids unnecessary DI setup and subscription overhead
4. **Code Clarity**: Component reactivity is immediately apparent

## Related Diagnostics

- RXBG041: Unused ObservableComponentTrigger
- RXBG060: Direct ObservableComponent inheritance
- RXBG012: Unused model reference

## Migration Tips

If you see this error in existing code:

1. **Review Component Purpose**: Does it need to react to model changes?
2. **Check Razor File**: Are there commented-out property references?
3. **Consider Architecture**: Should this be a regular ComponentBase instead?
4. **Add Triggers**: If you need lifecycle hooks but no automatic re-rendering

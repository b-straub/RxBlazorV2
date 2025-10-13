# RXBG019: Razor File @inherits Directive Doesn't Match Code-Behind

## Description

This diagnostic is reported when a Razor code-behind file (.razor.cs) inherits from `ObservableComponent` or `ObservableComponent<T>`, but the corresponding Razor file (.razor) is missing the matching `@inherits` directive. This mismatch can cause partial class merging issues and lifecycle hook failures.

## Cause

This warning occurs when:
- A .razor.cs file explicitly declares inheritance from `ObservableComponent` or `ObservableComponent<T>`
- The corresponding .razor file does not have a matching `@inherits` directive
- The Blazor compiler might use a different base class for the .razor file

## How to Fix

Add the matching `@inherits` directive to your .razor file to match the code-behind inheritance.

## Examples

### Example 1: Missing @inherits Directive (Warning)

**Code-Behind: MyComponent.razor.cs**
```csharp
using RxBlazorV2.Component;

namespace MyApp.Components
{
    // Code-behind inherits from ObservableComponent<MyModel>
    public partial class MyComponent : ObservableComponent<MyModel>
    {
        protected override void OnInitialized()
        {
            // Component initialization
        }
    }
}
```

**Razor File: MyComponent.razor** (❌ WRONG - Missing @inherits)
```razor
<h3>My Component</h3>

<p>Count: @Model.Count</p>

<button @onclick="Model.IncrementCommand.Execute">Increment</button>
```

### Example 2: Fix by Adding @inherits Directive

**Code-Behind: MyComponent.razor.cs**
```csharp
using RxBlazorV2.Component;

namespace MyApp.Components
{
    public partial class MyComponent : ObservableComponent<MyModel>
    {
        protected override void OnInitialized()
        {
            // Component initialization
        }
    }
}
```

**Razor File: MyComponent.razor** (✅ CORRECT - Matching @inherits)
```razor
@using RxBlazorV2.Component
@inherits ObservableComponent<MyModel>

<h3>My Component</h3>

<p>Count: @Model.Count</p>

<button @onclick="Model.IncrementCommand.Execute">Increment</button>
```

### Example 3: Non-Generic ObservableComponent

**Code-Behind: LayoutComponent.razor.cs**
```csharp
using RxBlazorV2.Component;

namespace MyApp.Components
{
    // Non-generic ObservableComponent
    public partial class LayoutComponent : ObservableComponent
    {
        [Inject]
        public SettingsModel Settings { get; set; } = null!;
    }
}
```

**Razor File: LayoutComponent.razor** (✅ CORRECT)
```razor
@using RxBlazorV2.Component
@inherits ObservableComponent

<div class="layout">
    <NavMenu />
    <div class="content">
        @Body
    </div>
</div>
```

### Example 4: Complex Inheritance Hierarchy

**Code-Behind: CustomDashboard.razor.cs**
```csharp
using RxBlazorV2.Component;

namespace MyApp.Components
{
    // Inherits from a custom base component that inherits from ObservableComponent
    public partial class CustomDashboard : MyBaseComponent<DashboardModel>
    {
    }

    public abstract class MyBaseComponent<T> : ObservableComponent<T>
        where T : ObservableModel
    {
        // Common functionality for all dashboard components
    }
}
```

**Razor File: CustomDashboard.razor** (✅ CORRECT)
```razor
@using RxBlazorV2.Component
@inherits MyBaseComponent<DashboardModel>

<div class="dashboard">
    <h2>Dashboard</h2>
    <p>Data: @Model.Data</p>
</div>
```

## Why This Matters

The `@inherits` directive is crucial for proper Blazor component functionality:

1. **Partial Class Merging**: Without the matching `@inherits` directive, the Blazor compiler might generate a different base class for the .razor file, causing the partial class definitions to be incompatible.

2. **Lifecycle Hooks**: ObservableComponent provides lifecycle hooks like `InitializeGeneratedCode()` and `InitializeGeneratedCodeAsync()`. Without the correct inheritance chain, these hooks won't be called.

3. **Reactive Binding**: ObservableComponent sets up automatic subscriptions to model changes. Mismatched inheritance breaks this reactive binding system.

4. **Model Access**: When using `ObservableComponent<T>`, the Model property is only accessible when the .razor file inherits from the same base class.

5. **Generated Code Integration**: The source generator creates code that expects the .razor and .razor.cs files to share the same base class hierarchy.

## Common Scenarios

### Scenario 1: New Component Creation

When creating a new component with a code-behind:

```csharp
// 1. Create .razor.cs first
public partial class MyComponent : ObservableComponent<MyModel>
{
    // Implementation
}

// 2. Add matching @inherits to .razor
@inherits ObservableComponent<MyModel>
```

### Scenario 2: Converting Existing Component

When converting an existing ComponentBase component to ObservableComponent:

```csharp
// Before
public partial class MyComponent : ComponentBase { }

// After - update both files
// .razor.cs:
public partial class MyComponent : ObservableComponent<MyModel> { }

// .razor:
@inherits ObservableComponent<MyModel>  // Add this line
```

### Scenario 3: Multiple Inheritance Levels

When using a custom base component:

```csharp
// Base component
public abstract class MyBase<T> : ObservableComponent<T>
    where T : ObservableModel
{ }

// Derived component .razor.cs
public partial class MyComponent : MyBase<MyModel> { }

// Derived component .razor
@inherits MyBase<MyModel>  // Match the immediate parent, not ObservableComponent
```

## Severity

**Warning** - This indicates a configuration issue that should be fixed to ensure proper component behavior, but it won't prevent compilation.

## Related Diagnostics

- RXBG009: Component contains ObservableModel without reactive binding

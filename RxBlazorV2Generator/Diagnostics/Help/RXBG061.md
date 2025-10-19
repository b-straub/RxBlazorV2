# RXBG061: Generated Component Used for Composition in Same Assembly Without @page Directive

## Description

This diagnostic is reported when a Razor file inherits from a source-generated component without using the `@page` directive AND the component is rendered/used as a child component (e.g., `<MyComponent />`) elsewhere in the same assembly. Due to Roslyn compilation order, this creates a compilation error.

**Key Point**: If the component is defined in assembly A but only rendered in assembly B, this diagnostic will NOT trigger because the generated code already exists when assembly B compiles.

This limitation is a fundamental characteristic of Roslyn source generators and the Razor compiler interaction (also produces Razor warning RZ10012).

## Cause

This error occurs when ALL of these conditions are met:
- A `.razor` file uses `@inherits GeneratedComponent` (e.g., `@inherits MyModelComponent`)
- The razor file does NOT have a `@page` directive
- The component class is source-generated in the same assembly
- **The component IS rendered/used in the same assembly** (e.g., another razor file contains `<ComponentName />`)
- The component is NOT the default layout (specified in `<RouteView ... DefaultLayout="@typeof(ComponentName)" />`)

**Note:** If the component is defined but NOT used in the same assembly, this diagnostic will not trigger because you can safely render it from another assembly.

## Why This Limitation Exists

The Razor compiler processes `.razor` files BEFORE source generators run during compilation:

1. **Razor Compilation**: Razor files are processed first and need all referenced types to exist
2. **Source Generation**: Source generators run after initial compilation to create new types
3. **Timing Conflict**: Razor can't reference types that don't exist yet in the same assembly

**This means:**
- ✅ **Direct inheritance with `@page` works**: The Razor file becomes a page and compilation succeeds
- ❌ **Child component usage fails**: Attempting to use `<ComponentName />` in another component fails because the type isn't available yet

## How to Fix

### Solution 1: Add @page Directive (Quick Fix)

If this component should be a routable page, add the `@page` directive:

```razor
@page "/my-page"
@inherits MyModelComponent

<h3>My Page</h3>
```

This works because pages use direct inheritance and don't require the type to be available for composition.

### Solution 2: Don't Render in Same Assembly (No Code Change Needed!)

**If your component is only rendered from other assemblies, you don't need to do anything!** The diagnostic only triggers when the component is rendered in the same assembly where it's defined.

**Example: ErrorManager Pattern**
```csharp
// Assembly A: MyApp.SharedComponents/Models/ErrorModel.cs
[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class ErrorModel : ObservableModel
{
    public partial string Message { get; set; } = string.Empty;
}
```

```razor
@* Assembly A: MyApp.SharedComponents/Components/ErrorDisplay.razor *@
@inherits ErrorModelComponent
@* ✅ NO RXBG061 - Not used anywhere in Assembly A *@

<div class="error">Error: @Model.Message</div>
```

```razor
@* Assembly B: MyApp/Pages/SomePage.razor *@
@page "/some-page"
@* ✅ Works perfectly! ErrorDisplay is from pre-compiled Assembly A *@

<ErrorDisplay />
```

This pattern is safe because when Assembly B compiles, Assembly A is already built and `ErrorModelComponent` exists.

### Solution 3: Move to Separate Assembly (Component Composition)

For components that need to be used as child components in composition:

1. **Create a new class library project** for shared components
2. **Move the model class** to the new assembly
3. **Reference the new assembly** from your main Blazor project
4. **Generated component is now available** for composition

## Examples

### Example 1: Default Layout - Allowed Without @page

```csharp
// MyProject/Models/SettingsModel.cs
[ObservableModelScope(ModelScope.Singleton)]
[ObservableComponent("SettingsModelComponent")]
public partial class SettingsModel : ObservableModel
{
    public partial bool IsDarkMode { get; set; }
}

// Generated: MyProject/Models/SettingsModelComponent.cs
```

```razor
@* MyProject/App.razor *@
<Router AppAssembly="@typeof(App).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
    </Found>
</Router>
```

```razor
@* MyProject/Layout/MainLayout.razor *@
@inherits SettingsModelComponent  @* ✅ No RXBG061 - MainLayout is the default layout *@

<MudThemeProvider IsDarkMode="@Model.IsDarkMode" />
<MudDrawer>...</MudDrawer>
<main>@Body</main>
```

The default layout component is excluded from RXBG061 because it's a top-level component by definition.

### Example 2: Problem - Child Component in Same Assembly (Won't Work)

```csharp
// MyProject/Models/WidgetModel.cs
[ObservableModelScope(ModelScope.Singleton)]
[ObservableComponent]
public partial class WidgetModel : ObservableModel
{
    public partial string Title { get; set; } = "Widget";
}

// Generated in same assembly: MyProject/Models/WidgetModelComponent.cs
```

```razor
@* MyProject/Pages/Dashboard.razor *@
@page "/dashboard"

<h1>Dashboard</h1>
<WidgetModelComponent />  @* ❌ Won't work - RZ10012 warning *@
```

```razor
@* MyProject/Components/Widget.razor *@
@inherits WidgetModelComponent  @* ❌ RXBG061 error - no @page directive, not default layout *@

<div class="widget">
    <h3>@Model.Title</h3>
</div>
```

### Example 3: Solution 1 - Use as Page

```razor
@* MyProject/Pages/Widget.razor *@
@page "/widget"  @* ✅ Added @page directive *@
@inherits WidgetModelComponent

<div class="widget">
    <h3>@Model.Title</h3>
</div>
```

Now the component works as a routable page.

### Example 4: Solution 2 - Separate Assembly for Composition

**Step 1: Create separate assembly**
```bash
dotnet new classlib -n MyProject.Components
dotnet add MyProject.Components reference RxBlazorV2
```

**Step 2: Move model to separate assembly**
```csharp
// MyProject.Components/Models/WidgetModel.cs
namespace MyProject.Components.Models;

[ObservableModelScope(ModelScope.Singleton)]
[ObservableComponent]
public partial class WidgetModel : ObservableModel
{
    public partial string Title { get; set; } = "Widget";
}

// Generated: MyProject.Components/Models/WidgetModelComponent.cs
```

**Step 3: Add using directive to main project**
```razor
@* MyProject/_Imports.razor *@
@using MyProject.Components.Models
```

**Step 4: Create Razor file in separate assembly**
```razor
@* MyProject.Components/Components/Widget.razor *@
@inherits WidgetModelComponent

<div class="widget">
    <h3>@Model.Title</h3>
</div>
```

**Step 5: Use in main project**
```razor
@* MyProject/Pages/Dashboard.razor *@
@page "/dashboard"

<h1>Dashboard</h1>
<Widget />  @* ✅ Works! Component from separate assembly *@
```

### Example 5: Architectural Best Practice

**Project Structure:**
```
Solution/
├── MyApp/                          # Main Blazor project
│   ├── Pages/
│   │   └── Dashboard.razor         # Uses components from shared library
│   └── Program.cs
│
├── MyApp.SharedComponents/         # Component library
│   ├── Models/
│   │   ├── HeaderModel.cs          # [ObservableComponent]
│   │   ├── FooterModel.cs          # [ObservableComponent]
│   │   └── SidebarModel.cs         # [ObservableComponent]
│   └── Components/
│       ├── Header.razor            # @inherits HeaderModelComponent
│       ├── Footer.razor            # @inherits FooterModelComponent
│       └── Sidebar.razor           # @inherits SidebarModelComponent
```

This architecture:
- ✅ Enables component composition
- ✅ Promotes reusability
- ✅ Separates concerns
- ✅ Avoids RXBG061 errors

## Direct Inheritance vs Component Composition

### Direct Inheritance (Works in Same Assembly)
```razor
@page "/my-page"
@inherits MyModelComponent  @* Works with @page *@
```

### Component Composition (Requires Separate Assembly)
```razor
@page "/parent-page"
<MyModelComponent />  @* Needs MyModelComponent from different assembly *@
```

## Architecture Considerations

### When to Use Same Assembly (Pages Only)
- Single-page components that don't need composition
- Quick prototyping
- Page-specific functionality

### When to Use Separate Assembly (Shared Components)
- Reusable UI components
- Component libraries
- Composition patterns
- Multiple projects sharing components

## Comparison with External Components

Components from external assemblies (like MudBlazor or your own component libraries) work perfectly because they're compiled BEFORE your main project:

```razor
@* Works fine - MudBlazor is pre-compiled *@
<MudButton>Click Me</MudButton>

@* Also works - from your pre-compiled component library *@
<MySharedComponent />

@* Doesn't work - same assembly, source-generated *@
<MyGeneratedComponent />  @* ❌ RXBG061 *@
```

## Related Information

- **Razor Warning RZ10012**: You may also see this warning from the Razor compiler indicating it can't find the generated type
- **Source Generator Timing**: This is a fundamental limitation of how Roslyn source generators interact with the Razor compiler
- **Compilation Order**: Razor → Source Generators → Final Compilation

## Migration Path

If you have existing same-assembly child components that trigger this error:

1. **Identify affected components** (those without `@page` inheriting from generated components)
2. **Decide**: Add `@page` to make them pages, OR move to separate assembly for composition
3. **If moving**: Create component library project, move models, add reference
4. **Update imports**: Add `@using` statements in `_Imports.razor`
5. **Build and verify**: Ensure all components resolve correctly

## Related Diagnostics

- **RXBG060**: Direct inheritance from ObservableComponent
- **RXBG014**: Shared model scope violations
- **RZ10012**: Razor compiler warning for missing types

# RxBlazorV2

A reactive programming framework for Blazor applications built on top of [R3 (Reactive Extensions)](https://github.com/Cysharp/R3). RxBlazorV2 uses Roslyn source generators to automatically create observable models with reactive property bindings, command patterns, and dependency injection support.

## Features

- **Reactive State Management**: Automatic change notifications using R3 observables
- **Source Generation**: Zero runtime reflection - all code generated at compile time
- **Command Pattern**: Declarative observable commands with automatic CanExecute support
- **Return Commands**: Commands that return values (sync and async)
- **Model References**: Automatic cross-model reactive subscriptions via partial constructors
- **DI Integration**: Automatic service registration with configurable lifetimes
- **Observable Collections**: Built-in support for reactive collections with batch updates
- **Generic Models**: Full support for generic observable models with type constraints
- **Component Generation**: Automatic Blazor component generation with `[ObservableComponent]`
- **Trigger System**: Multiple trigger types for different reactive scenarios
- **Notification Batching**: Suspend/resume change notifications for bulk updates

## Installation

```bash
dotnet add package RxBlazorV2
```

The package includes the source generator and code fixes automatically.

## Quick Start

```csharp
[ObservableModelScope(ModelScope.Scoped)]
[ObservableComponent]
public partial class CounterModel : ObservableModel
{
    public partial int Count { get; set; }

    [ObservableCommand(nameof(Increment))]
    public partial IObservableCommand IncrementCommand { get; }

    private void Increment() => Count++;
}
```

```razor
@page "/counter"
@inherits CounterModelComponent

<p>Count: @Model.Count</p>
<button @onclick="() => Model.IncrementCommand.Execute()">Increment</button>
```

## Core Concepts

### Observable Properties

Properties marked as `partial` with both getter and setter are automatically enhanced with change notifications, value equality checks, and integration with the reactive observable stream.

```csharp
public partial class MyModel : ObservableModel
{
    public partial string Name { get; set; } = "Default";
    public partial int Count { get; set; }
}
```

### Observable Commands

Commands link UI actions to model methods with automatic `CanExecute` tracking.

| Interface | Description |
|-----------|-------------|
| `IObservableCommand` | Sync, no parameters |
| `IObservableCommand<T>` | Sync with parameter |
| `IObservableCommandAsync` | Async, no parameters |
| `IObservableCommandAsync<T>` | Async with parameter and cancellation |
| `IObservableCommandR<T>` | Sync with return value |
| `IObservableCommandR<T1, T2>` | Sync with parameter and return value |
| `IObservableCommandRAsync<T>` | Async with return value |
| `IObservableCommandRAsync<T1, T2>` | Async with parameter and return value |

```csharp
[ObservableCommand(nameof(Save), nameof(CanSave))]
public partial IObservableCommandAsync SaveCommand { get; }

[ObservableCommand(nameof(Calculate))]
public partial IObservableCommandR<int> CalculateCommand { get; }

private async Task Save() { /* ... */ }
private bool CanSave() => !string.IsNullOrEmpty(Name);
private int Calculate() => Count * 2;
```

### Model References (Partial Constructor Pattern)

Models can reference and react to changes in other models through the partial constructor pattern:

```csharp
[ObservableModelScope(ModelScope.Scoped)]
public partial class ShoppingCartModel : ObservableModel
{
    // Declare dependencies via partial constructor
    public partial ShoppingCartModel(ProductCatalogModel catalog, ILogger logger);

    // Generator creates:
    // - protected ProductCatalogModel Catalog { get; } with auto-subscription
    // - private readonly ILogger _logger field

    public partial decimal Total { get; set; }

    private void RecalculateTotal()
    {
        Total = Quantity * Catalog.Price; // Access referenced model
    }
}
```

**Key Points:**
- ObservableModel parameters become `protected` properties with auto-subscription
- Other service parameters become `private readonly` fields with underscore prefix
- Observable streams are automatically merged from referenced models

### Component Generation

Use `[ObservableComponent]` to generate a Blazor component base class:

```csharp
[ObservableModelScope(ModelScope.Scoped)]
[ObservableComponent] // Generates MyModelComponent
public partial class MyModel : ObservableModel
{
    public partial string Title { get; set; }
}
```

```razor
@page "/mypage"
@inherits MyModelComponent

<h1>@Model.Title</h1>
```

Options:
- `componentName`: Custom component class name (default: `{ModelName}Component`)
- `includeReferencedTriggers`: Include triggers from referenced models (default: `true`)

## Trigger System

RxBlazorV2 provides multiple trigger types for different reactive scenarios:

### Component Triggers

Generate hook methods in components for property changes:

```csharp
[ObservableComponent]
public partial class SettingsModel : ObservableModel
{
    // Generates OnThemeChanged() hook in component
    [ObservableComponentTrigger]
    public partial string Theme { get; set; }

    // Async hook with custom name
    [ObservableComponentTriggerAsync(hookMethodName: "HandleDarkModeToggle")]
    public partial bool IsDarkMode { get; set; }

    // Render only - no hook generated
    [ObservableComponentTrigger(ComponentTriggerType.RenderOnly)]
    public partial int Counter { get; set; }

    // Hook only - no re-render
    [ObservableComponentTrigger(ComponentTriggerType.HookOnly)]
    public partial string BackgroundTask { get; set; }
}
```

**ComponentTriggerType Options:**
- `RenderAndHook` (default): Calls hook AND re-renders component
- `RenderOnly`: Re-renders but no hook method
- `HookOnly`: Calls hook but no automatic re-render

### Property Triggers

Execute internal methods automatically when properties change:

```csharp
public partial class ValidationModel : ObservableModel
{
    [ObservableTrigger(nameof(ValidateEmail))]
    public partial string Email { get; set; }

    [ObservableTriggerAsync(nameof(SaveAsync), nameof(CanSave))]
    public partial string Data { get; set; }

    private void ValidateEmail() { /* validation */ }
    private async Task SaveAsync() { /* save */ }
    private bool CanSave() => !string.IsNullOrEmpty(Data);
}
```

### Callback Triggers

Allow external services to subscribe to property changes:

```csharp
public partial class AuthModel : ObservableModel
{
    [ObservableCallbackTrigger]
    public partial ClaimsPrincipal? CurrentUser { get; set; }
}

// In external service:
public class AuthService
{
    public AuthService(AuthModel authModel)
    {
        authModel.OnCurrentUserChanged(() =>
        {
            // React to user changes
        });
    }
}
```

### Command Triggers

Auto-execute commands when properties change:

```csharp
public partial class SearchModel : ObservableModel
{
    public partial string Query { get; set; }

    [ObservableCommand(nameof(Search))]
    [ObservableCommandTrigger(nameof(Query))]
    public partial IObservableCommandAsync SearchCommand { get; }

    private async Task Search() { /* search logic */ }
}
```

## Notification Batching

Group property changes for single notifications:

```csharp
public partial class FormModel : ObservableModel
{
    [ObservableBatch("userInfo")]
    public partial string FirstName { get; set; }

    [ObservableBatch("userInfo")]
    public partial string LastName { get; set; }

    public void UpdateUser(string first, string last)
    {
        using (SuspendNotifications("userInfo"))
        {
            FirstName = first;
            LastName = last;
        } // Single notification fired here
    }
}
```

## Key Attributes

| Attribute | Target | Description |
|-----------|--------|-------------|
| `[ObservableModelScope]` | Class | DI lifetime (Singleton, Scoped, Transient) |
| `[ObservableComponent]` | Class | Generate component base class |
| `[ObservableCommand]` | Property | Link command to implementation method |
| `[ObservableComponentTrigger]` | Property | Generate component hook (sync) |
| `[ObservableComponentTriggerAsync]` | Property | Generate component hook (async) |
| `[ObservableTrigger]` | Property | Execute method on change (sync) |
| `[ObservableTriggerAsync]` | Property | Execute method on change (async) |
| `[ObservableCallbackTrigger]` | Property | Generate callback registration (sync) |
| `[ObservableCallbackTriggerAsync]` | Property | Generate callback registration (async) |
| `[ObservableCommandTrigger]` | Property | Auto-execute command on property change |
| `[ObservableBatch]` | Property | Group for batched notifications |

## Architecture

**Key Design Principles:**
- **Model Level**: Immediate, synchronous notifications for data integrity
- **Consumer Level**: Batched, chunked updates for UI performance
- **No Runtime Reflection**: All code generated at compile time
- **Type Safety**: Full IntelliSense and compile-time checking

## Requirements

- .NET 10.0 or later
- C# 14 (for partial constructors and `field` keyword)
- R3 v1.3.0 or later

## Sample Application

See the **RxBlazorV2Sample** project for comprehensive, interactive examples:

| Sample | Description |
|--------|-------------|
| **BasicCommands** | Sync/async commands and observable properties |
| **BasicCommandWithReturn** | Commands that return values |
| **ParameterizedCommands** | Commands with type-safe parameters |
| **CommandsWithCanExecute** | Conditional command execution |
| **CommandsWithCancellation** | Long-running async with cancellation |
| **CommandTriggers** | Auto-execute commands on property changes |
| **ComponentTriggers** | Component hooks for property changes |
| **PropertyTriggers** | Internal method execution on changes |
| **CallbackTriggers** | External service subscriptions |
| **ModelReferences** | Cross-model reactive subscriptions |
| **ModelPatterns** | Partial constructor pattern examples |
| **GenericModels** | Generic observable models with DI |
| **ObservableBatches** | Batched property notifications |
| **ValueEquality** | Automatic value equality |
| **CrossComponentCommunication** | Share models across components |

Run the sample application:
```bash
dotnet run --project RxBlazorV2Sample
```

## Project Structure

- **RxBlazorV2** - Core runtime library with base classes and interfaces
- **RxBlazorV2Generator** - Roslyn source generator for code generation
- **RxBlazorV2CodeFix** - Code analyzers and fixes for common issues
- **RxBlazorV2Sample** - Sample Blazor WebAssembly application

## Diagnostics

The generator provides comprehensive diagnostics (RXBG001-RXBG072) with code fixes. See the [Diagnostics Help](RxBlazorV2Generator/Diagnostics/Help/) folder for detailed documentation.

## Contributing

Contributions are welcome! Please ensure:
- All tests pass
- Code follows existing patterns
- Source generator changes include corresponding tests

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Credits

Built with:
- [R3](https://github.com/Cysharp/R3) - High-performance Reactive Extensions
- [ObservableCollections](https://github.com/Cysharp/ObservableCollections) - Reactive collection implementations
- [Microsoft.CodeAnalysis](https://github.com/dotnet/roslyn) - Roslyn compiler platform

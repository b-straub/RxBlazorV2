# RxBlazorV2

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/RxBlazorV2)](https://www.nuget.org/packages/RxBlazorV2)
[![Build and Test](https://github.com/b-straub/RxBlazorV2/actions/workflows/build.yml/badge.svg)](https://github.com/b-straub/RxBlazorV2/actions/workflows/build.yml)
[![GitHub Repo stars](https://img.shields.io/github/stars/b-straub/RxBlazorV2)](https://github.com/b-straub/RxBlazorV2/stargazers)

A reactive programming framework for Blazor applications built on top of [R3 (Reactive Extensions)](https://github.com/Cysharp/R3). RxBlazorV2 uses Roslyn source generators to automatically create observable models with reactive property bindings, command patterns, and dependency injection support.

**[Live Demo](https://b-straub.github.io/RxBlazorV2/)**

## Features

- **Reactive State Management**: Automatic change notifications using R3 observables
- **Source Generation**: Zero runtime reflection - all code generated at compile time
- **Command Pattern**: Declarative observable commands with automatic CanExecute support
- **Return Commands**: Commands that return values (sync and async)
- **Automatic Error Handling**: Commands automatically capture exceptions via `IErrorModel`
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

### Companion Packages

| Package | Description |
|---------|-------------|
| [RxBlazorV2.MudBlazor](https://www.nuget.org/packages/RxBlazorV2.MudBlazor) | Reactive MudBlazor button components with progress indicators, cancellation, and confirmation dialogs |

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

### Automatic Error Handling (`IErrorModel`)

Commands automatically capture exceptions and route them to an `IErrorModel` implementation for centralized error handling:

```csharp
// Implement IErrorModel in your error handling model
[ObservableModelScope(ModelScope.Singleton)]
public partial class ErrorModel : ObservableModel, IErrorModel
{
    [ObservableComponentTrigger]
    public ObservableList<string> Errors { get; } = [];

    public void HandleError(Exception error)
    {
        Errors.Add(error.Message);
    }
}

// Any model that injects IErrorModel gets automatic error capture
[ObservableModelScope(ModelScope.Scoped)]
public partial class MyModel : ObservableModel
{
    public partial MyModel(IErrorModel errorModel);

    [ObservableCommand(nameof(DoRiskyOperation))]
    public partial IObservableCommandAsync RiskyCommand { get; }

    private async Task DoRiskyOperation()
    {
        // If this throws, the exception is automatically
        // captured and sent to ErrorModel.HandleError()
        await _service.DoSomethingAsync();
    }
}
```

**Key Points:**
- Inject `IErrorModel` via partial constructor to enable automatic error capture
- All command exceptions are routed to `HandleError(Exception)` method
- No try/catch needed in command methods - errors are handled centrally
- Use with `RxBlazorV2.MudBlazor.StatusDisplay` for automatic UI feedback

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

### Abstract Base Class Contracts

Define reactive contracts in abstract base classes with attribute transfer to concrete implementations:

```csharp
// Abstract base class defines the contract with reactive attributes
public abstract class StatusBaseModel : ObservableModel
{
    [ObservableComponentTrigger]
    public abstract ObservableList<StatusMessage> Messages { get; }

    [ObservableComponentTrigger]
    [ObservableTrigger(nameof(CanAddMessageTrigger))]
    public abstract bool CanAddMessage { get; set; }

    [ObservableCommandTrigger(nameof(CanAddMessage))]
    public abstract IObservableCommand AddMessageCommand { get; }

    protected abstract void CanAddMessageTrigger();
}

// Concrete class - attributes are automatically transferred from base
[ObservableComponent]
[ObservableModelScope(ModelScope.Singleton)]
public partial class AppStatusModel : StatusBaseModel
{
    public override ObservableList<StatusMessage> Messages { get; } = [];
    public override partial bool CanAddMessage { get; set; }

    [ObservableCommand(nameof(AddMessage))]
    public override partial IObservableCommand AddMessageCommand { get; }

    protected override void CanAddMessageTrigger() { /* ... */ }
    private void AddMessage() { /* ... */ }
}
```

**Key Points:**
- Abstract base class defines the **contract** with reactive attributes
- Concrete class uses `override partial` - attributes are **automatically transferred**
- `[ObservableComponentTrigger]`, `[ObservableTrigger]`, `[ObservableCommandTrigger]` all transfer
- Generator produces `override` modifier in generated code
- Enables reusable reactive base classes across your application

### Internal Model Observers (Auto-Detection)

Private methods that **read** referenced model properties are automatically detected and subscribed. No special naming convention required - the generator analyzes which properties are actually accessed:

```csharp
[ObservableModelScope(ModelScope.Scoped)]
public partial class ShoppingCartModel : ObservableModel
{
    public partial ShoppingCartModel(ProductCatalogModel catalog);

    [ObservableTrigger(nameof(RecalculateTotal))]
    public partial int Quantity { get; set; }

    public partial decimal Total { get; set; }

    // This method is BOTH a property trigger AND an internal observer:
    // - Called when Quantity changes (via [ObservableTrigger])
    // - Called when Catalog.Price changes (auto-detected read)
    private void RecalculateTotal()
    {
        Total = Quantity * Catalog.Price;
    }
}
```

**Valid Signatures:**
- Sync: `private void MethodName()`
- Async: `private Task MethodName()` or `private Task MethodName(CancellationToken ct)`

**Key Points:**
- Methods are detected by analyzing property **reads** (not writes)
- A method can be both a local trigger AND a referenced model observer
- No naming convention required - any private method reading referenced properties works

### External Model Observers

Allow external services to observe model changes using `[ObservableModelObserver]`:

```csharp
public class NotificationService
{
    public NotificationService(UserModel userModel)
    {
        // Service is injected into model constructor
    }

    [ObservableModelObserver(nameof(UserModel.UnreadCount))]
    private void OnUnreadCountChanged(UserModel model)
    {
        UpdateBadge(model.UnreadCount);
    }

    [ObservableModelObserver(nameof(UserModel.Status))]
    private async Task OnStatusChangedAsync(UserModel model, CancellationToken ct)
    {
        await SyncStatusAsync(model.Status, ct);
    }
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

| Attribute                        | Target   | Description                                       |
|----------------------------------|----------|---------------------------------------------------|
| `[ObservableModelScope]`         | Class    | DI lifetime (Singleton, Scoped, Transient)        |
| `[ObservableComponent]`          | Class    | Generate component base class                     |
| `[ObservableCommand]`            | Property | Link command to implementation method             |
| `[ObservableComponentTrigger]`   | Property | Generate component hook (sync)                    |
| `[ObservableComponentTriggerAsync]` | Property | Generate component hook (async)                |
| `[ObservableTrigger]`            | Property | Execute method on change (sync)                   |
| `[ObservableTriggerAsync]`       | Property | Execute method on change (async)                  |
| `[ObservableCommandTrigger]`     | Property | Auto-execute command on property change           |
| `[ObservableModelObserver]`      | Method   | Subscribe service method to model property changes |
| `[ObservableBatch]`              | Property | Group for batched notifications                   |

## Architecture

**Key Design Principles:**
- **Model Level**: Immediate, synchronous notifications for data integrity
- **Consumer Level**: Batched, chunked updates for UI performance
- **No Runtime Reflection**: All code generated at compile time
- **Type Safety**: Full IntelliSense and compile-time checking

## Breaking Changes

### Version 1.0.4

**Removed: Generic `[ObservableTrigger<T>]` and `[ObservableTriggerAsync<T>]` attributes**

The generic trigger attributes have been removed. Use these alternatives instead:

| Removed                                  | Replacement                                                   |
|------------------------------------------|---------------------------------------------------------------|
| `[ObservableTrigger<T>(method, param)]`  | `[ObservableModelObserver]` on service methods                |
| `[ObservableTriggerAsync<T>(method, param)]` | `[ObservableModelObserver]` with async signature          |

> **Note**: The non-generic `[ObservableTrigger]` and `[ObservableTriggerAsync]` attributes for internal method execution are still available.

For internal model observers that react to referenced model changes, use the new **auto-detection pattern**:

```csharp
[ObservableModelScope(ModelScope.Scoped)]
public partial class MyModel : ObservableModel
{
    public partial MyModel(SettingsModel settings);

    // Auto-detected: private void method accessing Settings properties
    private void OnThemeChanged()
    {
        // Automatically subscribed to Settings.Theme changes
        ApplyTheme(Settings.Theme);
    }

    // Async version with CancellationToken
    private async Task OnLanguageChangedAsync(CancellationToken ct)
    {
        await LoadLocalizationAsync(Settings.Language, ct);
    }
}
```

For external services, use `[ObservableModelObserver]`:

```csharp
public class ThemeService
{
    [ObservableModelObserver(nameof(SettingsModel.Theme))]
    private void OnThemeChanged(SettingsModel model)
    {
        ApplyGlobalTheme(model.Theme);
    }
}
```

## Requirements

- .NET 10.0 or later
- C# 14 (for partial constructors and `field` keyword)
- R3 v1.3.0 or later

## Sample Applications

### RxBlazorV2Sample

See the **RxBlazorV2Sample** project for comprehensive, interactive examples:

| Sample                          | Description                                                        |
|---------------------------------|--------------------------------------------------------------------|
| **BasicCommands**               | Sync/async commands and observable properties                      |
| **BasicCommandWithReturn**      | Commands that return values                                        |
| **ParameterizedCommands**       | Commands with type-safe parameters                                 |
| **CommandsWithCanExecute**      | Conditional command execution                                      |
| **CommandsWithCancellation**    | Long-running async with cancellation                               |
| **ErrorHandling**               | Automatic error capture via IErrorModel                            |
| **CommandTriggers**             | Auto-execute commands on property changes                          |
| **ComponentTriggers**           | Component hooks for property changes                               |
| **PropertyTriggers**            | Internal method execution on changes                               |
| **ModelObservers**              | External and internal service subscriptions                        |
| **ModelReferences**             | Cross-model reactive subscriptions                                 |
| **ModelPatterns**               | Partial constructor pattern examples                               |
| **GenericModels**               | Generic observable models with DI                                  |
| **ObservableBatches**           | Batched property notifications                                     |
| **ValueEquality**               | Automatic value equality                                           |
| **CrossComponentCommunication** | Share models across components                                     |
| **InternalModelObservers**      | Auto-detected private methods reacting to referenced model changes |

Run the sample application:
```bash
dotnet run --project RxBlazorV2Sample
```

### ReactivePatternSample

The **ReactivePatternSample** is a multi-project Blazor application demonstrating all reactive patterns in a real-world scenario:

| Project | Description |
|---------|-------------|
| **ReactivePatternSample** | Main Blazor WASM host application |
| **ReactivePatternSample.Auth** | Authentication model with login/logout commands and triggers |
| **ReactivePatternSample.Settings** | User preferences with component triggers for theme/language |
| **ReactivePatternSample.Status** | Status bar model with internal observers and notifications |
| **ReactivePatternSample.Storage** | Persistence layer with external model observers |
| **ReactivePatternSample.Todo** | Todo list demonstrating commands, triggers, and cross-model reactivity |
| **ReactivePatternSample.Share** | Sharing functionality with async commands and dialogs |

**Key demonstrations:**
- Cross-project model references via partial constructors
- Internal model observers auto-detecting property reads
- External model observers with `[ObservableModelObserver]`
- Component triggers propagating from referenced models
- Real-world reactive composition patterns

Run the reactive pattern sample:
```bash
dotnet run --project ReactivePatternSample/ReactivePatternSample
```

## Project Structure

- **RxBlazorV2** - Core runtime library with base classes and interfaces
- **RxBlazorV2Generator** - Roslyn source generator for code generation
- **RxBlazorV2CodeFix** - Code analyzers and fixes for common issues
- **RxBlazorV2.MudBlazor** - MudBlazor button components with command binding
- **RxBlazorV2Sample** - Sample Blazor WebAssembly application with isolated examples
- **ReactivePatternSample** - Multi-project sample demonstrating all reactive patterns

## Diagnostics

The generator provides comprehensive diagnostics (RXBG001-RXBG082) with code fixes. See the [Diagnostics Help](RxBlazorV2Generator/Diagnostics/Help/) folder for detailed documentation.

Key diagnostic ranges:
- **RXBG001-RXBG019**: Core model and property diagnostics
- **RXBG020-RXBG029**: DI and service registration diagnostics
- **RXBG030-RXBG049**: Command diagnostics
- **RXBG050-RXBG059**: Service scope and cross-assembly diagnostics
- **RXBG060-RXBG069**: Component generation diagnostics
- **RXBG070-RXBG079**: Generic model diagnostics
- **RXBG080-RXBG082**: Model observer diagnostics

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

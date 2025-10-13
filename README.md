# RxBlazorV2

A reactive programming framework for Blazor applications built on top of [R3 (Reactive Extensions)](https://github.com/Cysharp/R3). RxBlazorV2 uses Roslyn source generators to automatically create observable models with reactive property bindings, command patterns, and dependency injection support.

## Features

- **🔄 Reactive State Management**: Automatic change notifications using R3 observables
- **⚡ Source Generation**: Zero runtime reflection - all code generated at compile time
- **🎯 Command Pattern**: Declarative observable commands with automatic CanExecute support
- **🔗 Model References**: Automatic cross-model reactive subscriptions
- **💉 DI Integration**: Automatic service registration with configurable lifetimes
- **📦 Observable Collections**: Built-in support for reactive collections with batch updates
- **🎭 Generic Models**: Full support for generic observable models with type constraints
- **🔔 Notification Batching**: Suspend/resume change notifications for bulk updates
- **🎨 Blazor Optimized**: Consumer-level chunking for efficient UI updates

## Installation

```bash
dotnet add package RxBlazorV2
dotnet add package RxBlazorV2Generator
```

## Core Concepts

### Observable Properties
Properties marked as `partial` with both getter and setter are automatically enhanced with change notifications, value equality checks, and integration with the reactive observable stream.

### Observable Commands
Commands link UI actions to model methods with automatic `CanExecute` tracking. Supports:
- `IObservableCommand` - Synchronous, no parameters
- `IObservableCommand<T>` - Synchronous with parameter
- `IObservableCommandAsync` - Asynchronous, no parameters
- `IObservableCommandAsync<T>` - Asynchronous with parameter and cancellation support

### Model References
Models can reference and react to changes in other models through automatic dependency injection and reactive subscriptions.

**Two Patterns for Working with Multiple Models:**

#### 1. ModelReference Pattern (Model-to-Model)
Use `[ObservableModelReference<T>]` when your **model** needs to react to changes in another model for **business logic or side effects**.

```csharp
[ObservableModelReference<ProductCatalogModel>]
public partial class ShoppingCartModel : ObservableModel
{
    public partial decimal Total { get; set; }

    // Automatically recalculates when ProductCatalog prices change
    private void RecalculateTotal()
    {
        Total = Quantity * ProductCatalogModel.Price;
    }
}
```

✅ **Use when:**
- Your model needs to perform calculations based on another model's data
- You need business logic to execute when referenced model changes
- The relationship is at the model/domain logic level
- Observable streams are automatically merged

#### 2. Component Injection Pattern (Component-to-Models)
Use `@inject` or `[Inject]` when your **component** needs to display data from multiple models with **automatic reactive updates**.

```csharp
@inherits ObservableComponent<ShoppingCartModel>
@inject UserProfileModel UserProfile
@inject ProductCatalogModel Catalog

<MudText>Cart Total: @Model.Total</MudText>
<MudText>User: @UserProfile.UserName</MudText>
<MudText>Laptop Price: @Catalog.LaptopPrice</MudText>
```

✅ **Use when:**
- Your component needs to display properties from multiple models
- No business logic needed - just UI composition
- Generator automatically creates subscriptions for accessed properties
- Component re-renders when any referenced property changes

**See `/samples/model-patterns` for a complete working example demonstrating both patterns.**

### Observable Collections
Built-in support for reactive collections with automatic change notifications and batch update capabilities.

### Notification Suspension
Batch multiple property changes into a single notification for improved performance during bulk updates.

### Dependency Injection
Models automatically detect and inject dependencies with configurable lifetime scopes (Singleton, Scoped, Transient).

### Generic Models
Full support for generic observable models with type constraints and automatic DI registration.

### Command Triggers
Commands can automatically execute when specified properties change, with configurable cancellation strategies (Switch vs Merge).

## Architecture

**Key Design Principles:**
- **Model Level**: Immediate, synchronous notifications for data integrity
- **Consumer Level**: Batched, chunked updates for UI performance
- **No Runtime Reflection**: All code generated at compile time
- **Type Safety**: Full IntelliSense and compile-time checking

## Requirements

- .NET 9.0 or later
- C# 13 (for `field` keyword support in properties)
- R3 v1.3.0 or later

## Sample Application

See the **RxBlazorV2Sample** project for comprehensive, interactive examples demonstrating all features:

- **Basic Commands** - Fundamentals of sync/async commands and observable properties
- **Parameterized Commands** - Commands that accept parameters with type safety
- **Commands with CanExecute** - Conditional command execution with automatic UI updates
- **Commands with Cancellation** - Long-running async operations with cancellation support
- **Command Triggers** - Automatic command execution with Switch vs Merge strategies
- **Model Patterns** - ModelReference vs Component Injection - when to use each pattern
- **Model References** - Sharing state between models with reactive updates
- **Generic Models** - Type-safe generic observable models with DI integration
- **Observable Batches** - Batch multiple property changes for performance
- **Value Equality** - Automatic value equality implementation
- **Cross Component Communication** - Share models across multiple Blazor components

Run the sample application:
```bash
dotnet run --project RxBlazorV2Sample
```

## Project Structure

- **RxBlazorV2** - Core runtime library with base classes and interfaces
- **RxBlazorV2Generator** - Roslyn source generator for code generation
- **RxBlazorV2CodeFix** - Code analyzers and fixes for common issues
- **RxBlazorV2Sample** - Sample Blazor WebAssembly application with interactive examples

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

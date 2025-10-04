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

## Quick Start

### 1. Installation

```bash
dotnet add package RxBlazorV2
dotnet add package RxBlazorV2Generator
```

### 2. Create an Observable Model

```csharp
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

[ObservableModelScope(ModelScope.Singleton)]
public partial class CounterModel : ObservableModel
{
    // Partial properties - automatically generate change notifications
    public partial int Counter { get; set; }
    public partial string? Message { get; set; }

    // Observable command with automatic CanExecute
    [ObservableCommand(nameof(IncrementCounter), nameof(CanIncrement))]
    public partial IObservableCommand IncrementCommand { get; }

    private void IncrementCounter()
    {
        Counter++;
        Message = $"Counter is now {Counter}";
    }

    private bool CanIncrement() => Counter < 10;
}
```

### 3. Register Services

```csharp
// In Program.cs
builder.Services.Initialize(); // Auto-generated registration
```

### 4. Use in Blazor Components

```razor
@inject CounterModel Model

<h1>Counter: @Model.Counter</h1>
<p>@Model.Message</p>

<button @onclick="() => Model.IncrementCommand.Execute()">
    Increment
</button>
```

## Core Concepts

### Observable Properties

Properties marked as `partial` with both getter and setter are automatically enhanced with change notifications:

```csharp
public partial int Age { get; set; }  // Generates:
                                      // - Backing field
                                      // - Change detection (field != value)
                                      // - StateHasChanged notification
```

**Generated code includes:**
- Value equality check (for equatable types) to prevent unnecessary notifications
- Automatic `StateHasChanged()` calls
- Integration with the reactive observable stream

### Observable Commands

Commands link UI actions to model methods with automatic `CanExecute` tracking:

```csharp
// Synchronous command
[ObservableCommand(nameof(Save), nameof(CanSave))]
public partial IObservableCommand SaveCommand { get; }

private void Save() { /* implementation */ }
private bool CanSave() => !string.IsNullOrEmpty(Name);

// Async command with cancellation
[ObservableCommand(nameof(LoadAsync))]
public partial IObservableCommandAsync LoadCommand { get; }

private async Task LoadAsync(CancellationToken token)
{
    await Task.Delay(1000, token);
}

// Parameterized command
[ObservableCommand(nameof(AddValue))]
public partial IObservableCommand<int> AddCommand { get; }

private void AddValue(int amount) { Total += amount; }
```

**Supported Command Patterns:**
- `IObservableCommand` - Synchronous, no parameters
- `IObservableCommand<T>` - Synchronous with parameter
- `IObservableCommandAsync` - Asynchronous, no parameters
- `IObservableCommandAsync<T>` - Asynchronous with parameter and cancellation support

### Model References

Models can reference and react to changes in other models:

```csharp
[ObservableModelReference<SettingsModel>]
public partial class DataModel : ObservableModel
{
    // SettingsModel automatically injected via DI
    // Automatic subscription to SettingsModel changes

    public partial string DisplayValue { get; set; }

    // Access referenced model
    private void UpdateDisplay()
    {
        DisplayValue = SettingsModel.Format;
    }
}
```

### Observable Collections

Built-in support for reactive collections:

```csharp
public partial ObservableList<string> Items { get; set; }

// Add items
Items.Add("New Item");
Items.AddRange(new[] { "Item 1", "Item 2" });

// Batch updates with notification suspension
using (SuspendNotifications())
{
    Items.Clear();
    Items.AddRange(Enumerable.Range(0, 1000).Select(i => $"Item {i}"));
    // Single notification fired at the end
}
```

### Notification Suspension

Batch multiple property changes into a single notification:

```csharp
using (SuspendNotifications())
{
    FirstName = "John";
    LastName = "Doe";
    Age = 30;
    Email = "john@example.com";
    // Single notification with all property names
}
```

**Features:**
- Supports nested suspension (only fires when all suspensions are released)
- Can be aborted to prevent notification
- Automatically collects all changed property names
- Works seamlessly with observable collections

### Dependency Injection

Models automatically detect and inject dependencies:

```csharp
[ObservableModelScope(ModelScope.Scoped)]
public partial class UserService : ObservableModel
{
    private readonly IHttpClient _httpClient;  // Auto-detected, injected
    private readonly ILogger _logger;           // Auto-detected, injected

    // Constructor generated automatically with DI parameters
}
```

**Scope Options:**
- `ModelScope.Singleton` - Single instance for application lifetime
- `ModelScope.Scoped` - Instance per scope (e.g., per user session in Blazor Server)
- `ModelScope.Transient` - New instance every time

### Generic Models

Full support for generic observable models:

```csharp
public partial class GenericModel<T, P> : ObservableModel
    where T : class
    where P : struct
{
    public partial ObservableList<T> Items { get; set; }
    public partial P Value { get; set; }
}

// Register in DI
services.GenericModel<string, int>();
```

## Advanced Features

### Lifecycle Hooks

Override lifecycle methods for custom initialization:

```csharp
protected override void OnContextReady()
{
    // Synchronous initialization
    LoadInitialData();
}

protected override async Task OnContextReadyAsync()
{
    // Asynchronous initialization
    await LoadDataFromApiAsync();
}

protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        // Cleanup
    }
    base.Dispose(disposing);
}
```

### Command Triggers

Automatically execute commands when properties change:

```csharp
[ObservableCommand(nameof(SaveData))]
[ObservableCommandTrigger(nameof(IsDirty), CanTrigger = nameof(CanSave))]
public partial IObservableCommand AutoSaveCommand { get; }

// Command executes automatically when IsDirty changes to true
// Only if CanSave() returns true
```

### Performance Tuning

Configure UI update frequency for Blazor components:

```xml
<PropertyGroup>
  <RxBlazorObservableUpdateFrequencyMs>100</RxBlazorObservableUpdateFrequencyMs>
</PropertyGroup>
```

This controls how often Blazor UI updates are batched (default: 100ms). Lower values = more responsive but more frequent renders.

## Architecture

```
┌─────────────────────────────────────┐
│   Blazor Components (UI Layer)      │
│   - Subscribe to Observable<T>      │
│   - Chunked updates (configurable)  │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│   Observable Models (Domain Layer)  │
│   - Immediate notifications         │
│   - No chunking at model level      │
│   - Cross-model subscriptions       │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│   R3 Observable Stream              │
│   - Property change notifications   │
│   - Command execution tracking      │
└─────────────────────────────────────┘
```

**Key Design Principles:**
- **Model Level**: Immediate, synchronous notifications for data integrity
- **Consumer Level**: Batched, chunked updates for UI performance
- **No Runtime Reflection**: All code generated at compile time
- **Type Safety**: Full IntelliSense and compile-time checking

## Project Structure

- **RxBlazorV2** - Core runtime library with base classes and interfaces
- **RxBlazorV2Generator** - Roslyn source generator for code generation
- **RxBlazorV2CodeFix** - Code analyzers and fixes for common issues
- **RxBlazorV2Sample** - Sample Blazor WebAssembly application
- **RxBlazorV2Test** - Unit tests for generator and runtime

## Requirements

- .NET 9.0 or later
- C# 13 (for `field` keyword support in properties)
- R3 v1.3.0 or later

## Example Applications

See the `RxBlazorV2Sample` project for a complete working example with:
- Multiple observable models
- Command patterns
- Model references
- Observable collections
- Generic models
- Blazor component integration

## Best Practices

1. **Use partial properties for all reactive state**
   ```csharp
   public partial string Name { get; set; } // ✅ Good
   public string Name { get; set; }         // ❌ Bad - no notifications
   ```

2. **Keep command methods private**
   ```csharp
   [ObservableCommand(nameof(Save))]
   public partial IObservableCommand SaveCommand { get; }
   private void Save() { } // ✅ Good - encapsulated
   ```

3. **Use notification suspension for bulk updates**
   ```csharp
   using (SuspendNotifications())
   {
       // Multiple property changes
   } // ✅ Single notification
   ```

4. **Leverage model references for composition**
   ```csharp
   [ObservableModelReference<SettingsModel>]
   public partial class DataModel : ObservableModel
   {
       // ✅ Automatic DI and reactive subscriptions
   }
   ```

5. **Choose appropriate scopes**
   - `Singleton` for application-wide state
   - `Scoped` for user-session state
   - `Transient` for short-lived operations

## Contributing

Contributions are welcome! Please ensure:
- All tests pass
- Code follows existing patterns
- Source generator changes include corresponding tests

## License

[Add your license here]

## Credits

Built with:
- [R3](https://github.com/Cysharp/R3) - High-performance Reactive Extensions
- [ObservableCollections](https://github.com/Cysharp/ObservableCollections) - Reactive collection implementations
- [Microsoft.CodeAnalysis](https://github.com/dotnet/roslyn) - Roslyn compiler platform

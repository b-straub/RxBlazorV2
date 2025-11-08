# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

RxBlazorV2 is a reactive programming framework for Blazor applications built on top of the R3 (Reactive Extensions) library. It uses Roslyn source generation to automatically create observable models with command bindings and dependency injection support.

## Core Architecture

### Observable Model System
- **ObservableModel**: Base class providing reactive state management using R3
- **Source Generation**: Automatic code generation for partial properties, commands, and DI registration
- **Attribute-Driven**: Uses attributes to configure generation behavior and model relationships

### Key Components
1. **RxBlazorV2** - Core library with ObservableModel base class and interfaces
2. **RxBlazorV2Generator** - Roslyn incremental source generator for code generation  
3. **RxBlazorV2Sample** - Blazor WebAssembly sample application demonstrating usage
4. **GeneratorRunner** - Console tool for testing source generator functionality

## Development Commands

### Build and Test
```bash
# Build entire solution
dotnet build

# Test source generator (primary testing method)
dotnet run --project GeneratorRunner

# Run Blazor sample application
dotnet run --project RxBlazorV2Sample

# Clean all build artifacts
./DeleteBinObj.sh
```

### Source Generator Testing
Use GeneratorRunner to validate generator output:
- Modify test files in GeneratorRunner/Program.cs to test different scenarios
- Generator output is displayed in console for verification
- Check against expected_generated_code.cs for reference patterns

## Code Generation Patterns

### Observable Model Declaration
```csharp
[ObservableModelScope(ModelScope.Scoped)] // or Singleton, Transient
public partial class MyModel : ObservableModel
{
    // Partial properties - generator creates implementations with change notifications
    public partial string Name { get; set; } = "Default";
    public partial int Count { get; set; }
    
    // Command properties - generator creates backing fields and implementations
    [ObservableCommand(nameof(ExecuteMethod), nameof(CanExecuteMethod))]
    public partial IObservableCommand MyCommand { get; }
    
    // Method implementations
    private void ExecuteMethod() { /* implementation */ }
    private bool CanExecuteMethod() => true;
}
```

### Model References and DI (Partial Constructor Pattern)
```csharp
public partial class MyModel : ObservableModel
{
    // Declare partial constructor with ObservableModel dependencies
    public partial MyModel(OtherModel other, SomeService service);

    // Generator creates: protected OtherModel Other { get; }
    // Generator creates constructor implementation with DI injection
    // Generator merges observables from referenced ObservableModels
    // Non-ObservableModel params become private fields with underscore prefix
}
```

**Key Points:**
- All constructor parameters are considered DI dependencies
- ObservableModel parameters become `protected` properties with auto-subscription
- Other service parameters become `private readonly` fields
- Property names use PascalCase of parameter names (e.g., `other` → `Other`)

### Generated Code Patterns
- **Properties**: Use `field` keyword with `StateHasChanged(nameof(PropertyName))`
- **Commands**: Different factories based on method signatures:
  - `ObservableCommandFactory` for sync methods
  - `ObservableCommandAsyncFactory` for async methods
  - Generic versions for parametized commands
- **DI Registration**: `AddObservableModels()` extension method generated automatically

### Component Trigger Patterns

Component triggers allow Blazor components to react to specific property changes in models through generated hook methods.

#### Local Triggers (Same Model)
```csharp
public partial class SettingsModel : ObservableModel
{
    // Sync trigger - generates OnIsDayChanged() hook in SettingsModelComponent
    [ObservableComponentTrigger]
    public partial bool IsDay { get; set; }

    // Async trigger - generates OnThemeChangedAsync(CancellationToken) hook
    [ObservableComponentTriggerAsync]
    public partial string Theme { get; set; }

    // Custom hook names
    [ObservableComponentTrigger(hookMethodName: "HandleDayNightSwitch")]
    public partial bool IsDayCustom { get; set; }
}
```

**Generated Component Hooks:**
```csharp
// In SettingsModelComponent.g.cs
protected virtual void OnIsDayChanged() { }
protected virtual Task OnThemeChangedAsync(CancellationToken ct) { return Task.CompletedTask; }
protected virtual void HandleDayNightSwitch() { }
```

#### Referenced Model Triggers (Cross-Model)
```csharp
// SettingsModel.cs (no [ObservableComponent] needed)
public partial class SettingsModel : ObservableModel
{
    [ObservableComponentTrigger]
    public partial bool IsDay { get; set; }
}

// WeatherModel.cs
[ObservableComponent]  // includeReferencedTriggers: true by default
public partial class WeatherModel : ObservableModel
{
    public partial WeatherModel(SettingsModel settings);

    public partial string Temperature { get; set; }
}
```

**Generated Hooks in WeatherModelComponent:**
```csharp
// Hook naming: On{RefProperty}{TriggerProperty}Changed[Async]
protected virtual void OnSettingsIsDayChanged() { }

// Component subscribes to Model.Observable with filter "Model.Settings.IsDay"
```

**Important Notes:**
- Referenced model does NOT need `[ObservableComponent]` attribute
- Triggers automatically generate hooks when `includeReferencedTriggers: true` (default)
- Set `[ObservableComponent(includeReferencedTriggers: false)]` to disable
- Referenced models MUST be in same assembly (see RXBG052 diagnostic)
- Reference counts as USED even with no code usage (prevents RXBG012 error)

### Property Trigger Patterns (Internal Model Methods)

Property triggers execute private methods automatically when properties change, without creating command objects.

```csharp
public partial class MyModel : ObservableModel
{
    // Sync trigger - infers from method signature
    [ObservableTrigger(nameof(ValidateInput))]
    public partial string Input { get; set; }

    // Explicit async trigger
    [ObservableTriggerAsync(nameof(SaveAsync))]
    public partial string Data { get; set; }

    // Parametrized sync trigger
    [ObservableTrigger<string>(nameof(LogChange), "InputChanged")]
    public partial int Count { get; set; }

    // Parametrized async trigger with cancellation
    [ObservableTriggerAsync<int>(nameof(ValidateRangeAsync), 100)]
    public partial int Value { get; set; }

    // Optional can-trigger guard
    [ObservableTriggerAsync(nameof(SaveAsync), nameof(CanSave))]
    public partial string SecureData { get; set; }

    private void ValidateInput() { /* validation */ }
    private async Task SaveAsync() { await _repo.SaveAsync(Data); }
    private void LogChange(string message) { _logger.Log($"{message}: {Count}"); }
    private async Task ValidateRangeAsync(int max, CancellationToken ct) { /* validate */ }
    private bool CanSave() => !string.IsNullOrEmpty(SecureData);
}
```

**Key Points:**
- Methods must be `private`
- `[ObservableTrigger]` infers async from method signature (CancellationToken parameter or "Async" suffix)
- `[ObservableTriggerAsync]` makes async explicit and clearer
- Generic versions support parametrized methods
- Optional `canTriggerMethod` parameter for conditional execution
- Avoid circular triggers (method modifying the same property)

### Callback Trigger Patterns (External Service Subscriptions)

Callback triggers provide clean API for external services to subscribe to property changes, replacing manual `Observable.Where()` subscriptions.

```csharp
// In Model
[ObservableModelScope(ModelScope.Scoped)]
public partial class StatusModel : ObservableModel
{
    // Generates: OnCurrentUserChanged(Action callback)
    [ObservableCallbackTrigger]
    public partial ClaimsPrincipal? CurrentUser { get; set; }

    // Generates: HandleThemeUpdateAsync(Func<CancellationToken, Task> callback)
    [ObservableCallbackTriggerAsync("HandleThemeUpdateAsync")]
    public partial string Theme { get; set; }

    // Both sync and async callbacks on same property
    [ObservableCallbackTrigger]
    [ObservableCallbackTriggerAsync]
    public partial UserSettings? Settings { get; set; }
}

// In External Service
public class MyAuthService
{
    private AuthenticationState _authenticationState;

    public MyAuthService(StatusModel statusModel)
    {
        // Clean subscription instead of manual Observable.Where()
        statusModel.OnCurrentUserChanged(() =>
        {
            _authenticationState = new AuthenticationState(
                statusModel.CurrentUser ?? new ClaimsPrincipal());
            NotifyAuthenticationStateChanged(Task.FromResult(_authenticationState));
        });

        // Async callback with CancellationToken
        statusModel.OnSettingsChangedAsync(async (ct) =>
        {
            await SaveSettingsToFileAsync(statusModel.Settings, ct);
        });
    }
}
```

**Generated Code (partial):**
```csharp
// Callback storage
private readonly List<Action> _onCurrentUserChangedCallbacks = new();
private readonly List<Func<CancellationToken, Task>> _onSettingsChangedAsyncCallbacks = new();

// Registration methods
public void OnCurrentUserChanged(Action callback)
{
    if (callback is null) throw new ArgumentNullException(nameof(callback));
    _onCurrentUserChangedCallbacks.Add(callback);
}

public void OnSettingsChangedAsync(Func<CancellationToken, Task> callback)
{
    if (callback is null) throw new ArgumentNullException(nameof(callback));
    _onSettingsChangedAsyncCallbacks.Add(callback);
}

// Constructor subscriptions
Subscriptions.Add(Observable.Where(p => p.Intersect(["Model.CurrentUser"]).Any())
    .Subscribe(_ => {
        foreach (var callback in _onCurrentUserChangedCallbacks) callback();
    }));
```

**Key Points:**
- Replaces verbose `Observable.Where(c => c.Contains("Model.PropertyName"))` subscriptions
- Subscriptions auto-managed via model's `CompositeDisposable`
- All callbacks disposed when model is disposed
- Optional custom method names
- Callbacks invoked on Observable's scheduler thread

## Key Attributes

- `[ObservableModelScope]` - Controls DI lifetime (Singleton, Scoped, Transient)
- `[ObservableCommand]` - Links command properties to implementation methods
- `[ObservableComponent]` - Enables component generation with optional `includeReferencedTriggers` parameter
- `[ObservableComponentTrigger]` - Generates sync hook method in component (optional custom name parameter)
- `[ObservableComponentTriggerAsync]` - Generates async hook method in component (optional custom name parameter)
- `[ObservableTrigger]` - Executes sync methods automatically when property changes (supports inference of async)
- `[ObservableTriggerAsync]` - Explicitly marks async method execution on property changes
- `[ObservableTrigger<T>]` - Executes parametrized sync methods on property changes
- `[ObservableTriggerAsync<T>]` - Executes parametrized async methods on property changes
- `[ObservableCallbackTrigger]` - Generates sync callback registration method for external services
- `[ObservableCallbackTriggerAsync]` - Generates async callback registration method for external services

## Command Method Signatures

The generator supports multiple command patterns:
```csharp
void Method() → IObservableCommand
Task Method() → IObservableCommandAsync
void Method(T param) → IObservableCommand<T>
Task Method(T param) → IObservableCommandAsync<T>
Task Method(T param, CancellationToken token) → IObservableCommandAsync<T> // with cancellation
```

## Dependencies and Framework Versions

- **.NET 9.0** for main projects (RxBlazorV2, RxBlazorV2Sample, GeneratorRunner)
- **.NET Standard 2.0** for source generator (RxBlazorV2Generator)
- **R3 v1.3.0** for reactive programming
- **MudBlazor v8.7.0** for UI components in sample
- **Microsoft.CodeAnalysis v4.8.0+** for source generation

## Important Files

- `/RxBlazorV2Generator/RxBlazorGenerator.cs` - Main source generator implementation
- `/expected_generated_code.cs` - Reference for expected generator output patterns
- `/GeneratorRunner/Program.cs` - Source generator test runner configuration
- Sample models in `/RxBlazorV2Sample/Model/` demonstrate usage patterns

## Development Notes

- Use GeneratorRunner for testing generator changes - modify as needed for different test scenarios
- The generator uses semantic analysis for DI field detection rather than hardcoded type lists
- Property initializers should be set on partial property declarations, not in generated code
- Field keyword approach is used for generated property implementations
- All command methods must be private and follow specific signature patterns for proper generation
- **Partial Constructor Pattern**: Declare dependencies via partial constructors (C# 14 feature)
  - ObservableModel dependencies are auto-detected and subscribed
  - Generated properties for ObservableModels are `protected` (accessible in derived classes)
  - Regular DI services become private fields with underscore prefix
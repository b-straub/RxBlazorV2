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

## Key Attributes

- `[ObservableModelScope]` - Controls DI lifetime (Singleton, Scoped, Transient)
- `[ObservableCommand]` - Links command properties to implementation methods

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
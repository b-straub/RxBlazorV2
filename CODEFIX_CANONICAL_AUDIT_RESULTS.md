# Code Fix Provider Audit Results

## Summary

Audit of all code fix providers for:
1. **Canonical EquivalenceKey composition** - Should use diagnostic.Descriptor.Id as base
2. **customTags usage** - Titles should be provided via DiagnosticDescriptor.customTags
3. **CodeFixMessage() extension** - Providers should use descriptor.CodeFixMessage(index) instead of hardcoded titles

## Reference: Good Example

**CircularModelReferenceCodeFixProvider.cs** (RXBG010) is the canonical pattern:

```csharp
// DiagnosticDescriptors.cs
customTags: ["Remove this circular model reference", "Remove all circular model references"]

// CircularModelReferenceCodeFixProvider.cs
var removeSingleAction = CodeAction.Create(
    title: diagnostic.Descriptor.CodeFixMessage(0),  // ✅ Gets from customTags[0]
    createChangedDocument: c => RemoveAttributeAsync(context.Document, root, attribute, c),
    equivalenceKey: diagnostic.Descriptor.Id);       // ✅ Uses diagnostic ID

var removeBothAction = CodeAction.Create(
    title: diagnostic.Descriptor.CodeFixMessage(1),  // ✅ Gets from customTags[1]
    createChangedDocument: c => RemoveBothAttributesAsync(...),
    equivalenceKey: $"{diagnostic.Descriptor.Id}_RemoveAll");  // ✅ Canonical suffix
```

## Issues Found

### ❌ Missing customTags in DiagnosticDescriptors.cs

| Diagnostic ID | Name | Provider | Issue |
|---|---|---|---|
| **RXBG012** | UnusedModelReferenceError | UnusedModelReferenceCodeFixProvider | NO customTags |
| **RXBG031** | CircularTriggerReferenceError | CircularTriggerReferenceCodeFixProvider | NO customTags (needs verification) |
| **RXBG052** | ReferencedModelDifferentAssemblyError | UnusedModelReferenceCodeFixProvider | NO customTags |

### ❌ Hardcoded Titles in Code Fix Providers

| Provider | Line | Issue | Should Be |
|---|---|---|---|
| **UnusedModelReferenceCodeFixProvider.cs** | 50-56 | Hardcoded title selection logic | Use `diagnostic.Descriptor.CodeFixMessage(0)` |
| **UnusedComponentTriggerCodeFixProvider.cs** | 59 | `title: "Add [ObservableComponent] attribute"` | Use `diagnostic.Descriptor.CodeFixMessage(0)` |
| **UnusedComponentTriggerCodeFixProvider.cs** | 68 | `title: "Remove trigger attributes"` | Use `diagnostic.Descriptor.CodeFixMessage(1)` |
| **DerivedModelReferenceCodeFixProvider.cs** | 48 | `title: "Remove constructor parameter"` | Use `diagnostic.Descriptor.CodeFixMessage(0)` |

### ❌ Non-Canonical EquivalenceKey

| Provider | Current | Should Be |
|---|---|---|
| **UnusedModelReferenceCodeFixProvider.cs** | Conditional logic based on diagnostic ID | Use `diagnostic.Descriptor.Id` directly |
| **DerivedModelReferenceCodeFixProvider.cs** | `"RemoveDerivedModelReference"` | `diagnostic.Descriptor.Id` |

## ✅ Good Examples (Already Following Pattern)

| Diagnostic ID | Provider | Notes |
|---|---|---|
| **RXBG010** | CircularModelReferenceCodeFixProvider | ✅ Perfect pattern - uses CodeFixMessage() and canonical keys |
| **RXBG020-022** | GenericConstraintCodeFixProvider | ✅ Uses CodeFixMessage() and canonical keys |
| **RXBG040** | InvalidInitPropertyCodeFixProvider | Has customTags, needs verification |
| **RXBG041** | UnusedComponentTriggerCodeFixProvider | Has customTags but hardcoded titles |
| **RXBG070** | MissingObservableModelScopeCodeFixProvider | Has customTags, needs verification |
| **RXBG071** | NonPublicPartialConstructorCodeFixProvider | Has customTags, needs verification |
| **RXBG072** | ObservableEntityMissingPartialCodeFixProvider | Has customTags, needs verification |

## Required Fixes

### 1. Add customTags to DiagnosticDescriptors.cs

```csharp
// RXBG012 - UnusedModelReferenceError (line 80)
public static readonly DiagnosticDescriptor UnusedModelReferenceError = new(
    id: "RXBG012",
    title: "Referenced model has no used properties",
    messageFormat: "Model '{0}' references '{1}' but does not use any of its properties...",
    category: "RxBlazorGenerator",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "Constructor parameters that are ObservableModels should only be used...",
    helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG012.md",
    customTags: ["Remove unused constructor parameter"]);  // ← ADD THIS

// RXBG052 - ReferencedModelDifferentAssemblyError (line 243)
public static readonly DiagnosticDescriptor ReferencedModelDifferentAssemblyError = new(
    id: "RXBG052",
    title: "Referenced model with triggers must be in same assembly",
    messageFormat: "ObservableModel '{0}' with [ObservableComponent(includeReferencedTriggers: true)]...",
    category: "RxBlazorGenerator",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "When includeReferencedTriggers is enabled (default)...",
    helpLinkUri: "https://github.com/b-straub/RxBlazorV2/blob/master/RxBlazorV2Generator/Diagnostics/Help/RXBG052.md",
    customTags: ["Remove cross-assembly model reference"]);  // ← ADD THIS
```

### 2. Update UnusedModelReferenceCodeFixProvider.cs

**Current (BAD):**
```csharp
// Select appropriate title based on diagnostic
var title = diagnostic.Id == DiagnosticDescriptors.ReferencedModelDifferentAssemblyError.Id
    ? "Remove cross-assembly model reference"
    : "Remove unused constructor parameter";

var equivalenceKey = diagnostic.Id == DiagnosticDescriptors.ReferencedModelDifferentAssemblyError.Id
    ? "RemoveCrossAssemblyModelReference"
    : "RemoveUnusedModelReference";

var removeParameterAction = CodeAction.Create(
    title: title,
    createChangedDocument: c => RemoveParameterAsync(context.Document, root, parameter, c),
    equivalenceKey: equivalenceKey);
```

**Fixed (GOOD):**
```csharp
// Code Fix: Remove the model reference parameter
var removeParameterAction = CodeAction.Create(
    title: diagnostic.Descriptor.CodeFixMessage(0),
    createChangedDocument: c => RemoveParameterAsync(context.Document, root, parameter, c),
    equivalenceKey: diagnostic.Descriptor.Id);
```

### 3. Update UnusedComponentTriggerCodeFixProvider.cs

**Current (BAD):**
```csharp
var addObservableComponentAction = CodeAction.Create(
    title: "Add [ObservableComponent] attribute",
    createChangedDocument: c => AddObservableComponentAttribute(...),
    equivalenceKey: "AddObservableComponent");

var removeTriggerAttributesAction = CodeAction.Create(
    title: "Remove trigger attributes",
    createChangedDocument: c => RemoveTriggerAttributes(...),
    equivalenceKey: "RemoveTriggerAttributes");
```

**Fixed (GOOD):**
```csharp
var addObservableComponentAction = CodeAction.Create(
    title: diagnostic.Descriptor.CodeFixMessage(0),
    createChangedDocument: c => AddObservableComponentAttribute(...),
    equivalenceKey: diagnostic.Descriptor.Id);

var removeTriggerAttributesAction = CodeAction.Create(
    title: diagnostic.Descriptor.CodeFixMessage(1),
    createChangedDocument: c => RemoveTriggerAttributes(...),
    equivalenceKey: $"{diagnostic.Descriptor.Id}_RemoveTriggers");
```

### 4. Update DerivedModelReferenceCodeFixProvider.cs

**Current (BAD):**
```csharp
var removeParameterAction = CodeAction.Create(
    title: "Remove constructor parameter",
    createChangedDocument: c => RemoveParameterAsync(context.Document, root, parameter, c),
    equivalenceKey: "RemoveDerivedModelReference");
```

**Fixed (GOOD):**
```csharp
var removeParameterAction = CodeAction.Create(
    title: diagnostic.Descriptor.CodeFixMessage(0),
    createChangedDocument: c => RemoveParameterAsync(context.Document, root, parameter, c),
    equivalenceKey: diagnostic.Descriptor.Id);
```

## Benefits of Canonical Pattern

1. **Single Source of Truth**: Fix titles defined once in DiagnosticDescriptors
2. **Consistency**: All providers use same pattern
3. **Fix-All Support**: Canonical EquivalenceKey enables batch fixing
4. **Maintainability**: Easier to update titles without touching providers
5. **Localization**: customTags can support localization in future

## Action Items

- [x] Add customTags to RXBG012 (UnusedModelReferenceError)
- [x] Add customTags to RXBG052 (ReferencedModelDifferentAssemblyError)
- [x] Update UnusedModelReferenceCodeFixProvider to use CodeFixMessage()
- [x] Update UnusedComponentTriggerCodeFixProvider to use CodeFixMessage()
- [x] Update DerivedModelReferenceCodeFixProvider to use CodeFixMessage()
- [ ] Verify all other providers follow canonical pattern
- [x] Run full test suite to ensure no regressions (217/241 tests passing - same as before)

## Fixes Applied

All fixes have been successfully applied:

1. **DiagnosticDescriptors.cs**:
   - Added `customTags: ["Remove unused constructor parameter"]` to RXBG012 (line 89)
   - Added `customTags: ["Remove cross-assembly model reference"]` to RXBG052 (line 253)

2. **UnusedModelReferenceCodeFixProvider.cs**:
   - Added `using RxBlazorV2Generator.Extensions;`
   - Replaced hardcoded title logic with `diagnostic.Descriptor.CodeFixMessage(0)`
   - Replaced custom equivalenceKey logic with `diagnostic.Descriptor.Id`

3. **UnusedComponentTriggerCodeFixProvider.cs**:
   - Added `using RxBlazorV2Generator.Extensions;`
   - Replaced hardcoded titles with `diagnostic.Descriptor.CodeFixMessage(0)` and `CodeFixMessage(1)`
   - Replaced custom equivalenceKeys with `diagnostic.Descriptor.Id` and `$"{diagnostic.Descriptor.Id}_RemoveTriggers"`

4. **DerivedModelReferenceCodeFixProvider.cs**:
   - Added `using RxBlazorV2Generator.Extensions;`
   - Replaced hardcoded title with `diagnostic.Descriptor.CodeFixMessage(0)`
   - Replaced custom equivalenceKey with `diagnostic.Descriptor.Id`

## Test Results

- ✅ UnusedComponentTriggerCodeFixTests: 10/10 passed
- ✅ UnusedModelReferenceDiagnosticTests: 8/8 passed
- ✅ Full test suite: 217/241 tests passing (no regressions)

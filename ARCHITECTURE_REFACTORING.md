# RxBlazorV2 Generator Architecture Refactoring Plan

**Date:** 2025-10-26
**Status:** Investigation Complete - Ready for Implementation
**Working Directory:** `/Users/berni/Projects/RxBlazorV2`

---

## Critical Issues Found

### Issue 1: Missing Using Directive for Cross-Assembly Base Components
**Location:** TodoManager.g.cs (WebAppBase.UserSample)

**Problem:**
```csharp
// Generated code - WRONG
namespace WebAppBase.UserSample.Pages;

public partial class TodoManager : StatusModelComponent  // ❌ StatusModelComponent not imported
{
    protected override string[] Filter() { ... }
}
```

**Should be:**
```csharp
// Correct
using WebAppBase.Shared.Models.Status;  // ✅ Import base component namespace

namespace WebAppBase.UserSample.Pages;

public partial class TodoManager : StatusModelComponent
{
    protected override string[] Filter() { ... }
}
```

**Root Cause:** Cross-assembly components detected by `FindCrossAssemblyObservableComponents()` but their namespace not added to using directives in `RazorCodeBehindGenerator.cs`.

---

### Issue 2: Missing "Model." Prefix for Cross-Assembly Inherited Components
**Location:** TodoManager.g.cs filter properties

**Problem:**
```csharp
// Generated - WRONG
protected override string[] Filter()
{
    return [
        "ErrorMessage",           // ❌ Missing "Model." prefix
        "SetError",               // ❌ Missing "Model." prefix
        "SyncModel",              // ❌ Missing "Model." prefix
        "SyncModel.Changes",      // ❌ Missing "Model." prefix
        ...
    ];
}
```

**Should be:**
```csharp
// Correct (like PushManager.g.cs from same assembly)
protected override string[] Filter()
{
    return [
        "Model.ErrorMessage",           // ✅ Has "Model." prefix
        "Model.SetError",               // ✅ Has "Model." prefix
        "Model.SyncModel",              // ✅ Has "Model." prefix
        "Model.SyncModel.Changes",      // ✅ Has "Model." prefix
        ...
    ];
}
```

**Root Cause:** `FilterablePropertiesBuilder` only processes same-assembly components (from `processedRecords`), doesn't include cross-assembly components.

---

### Issue 3: Fragmented Architecture with 4 Overlapping Detection Systems

Current code has **4 separate dictionaries** tracking components:

```csharp
// RxBlazorGenerator.cs lines 470-512
var componentNamespaces = new Dictionary<string, string>();         // Same-assembly only
var componentHasTriggers = new Dictionary<string, bool>();          // Same-assembly only
var crossAssemblyComponents = compilation.FindCrossAssemblyObs...() // Cross-assembly only
var filterableProperties = FilterablePropertiesBuilder.Build...()   // Same-assembly only
var codeBehindPropertyUsages = CodeBehindPropertyAnalyzer...()     // Uses both!
```

**Problems:**
- Parallel tracking causing drift
- Cross-assembly components incomplete metadata
- Hard to maintain (5 dictionaries to keep in sync)
- Easy to miss cases (like Issues #1 and #2)

---

## Root Cause Analysis

**Component Coverage Matrix:**

| Component Source | componentNamespaces | crossAssemblyComponents | filterableProperties | Result |
|------------------|---------------------|-------------------------|----------------------|---------|
| Same Assembly (e.g., PushManager in WebAppBase.Shared) | ✅ Has namespace | ❌ Not scanned | ✅ Has properties | ✅ Works perfectly |
| Cross Assembly (e.g., TodoManager inherits StatusModelComponent from different assembly) | ❌ Not included | ✅ Has namespace + symbol | ❌ Not included | ❌ **Missing using + wrong prefix** |

**Key Finding:** Cross-assembly components only half-detected, causing both bugs.

---

## Proposed Solution: Unified GeneratorContext

### Core Concept

Build **one unified metadata structure** at initialization containing ALL components/models from ALL assemblies:

```csharp
public class GeneratorContext
{
    // All components (current + referenced assemblies)
    public Dictionary<string, ComponentMetadata> AllComponents { get; }

    // All models (current + referenced assemblies)
    public Dictionary<string, ModelMetadata> AllModels { get; }
}

public class ComponentMetadata
{
    public string FullyQualifiedName { get; set; }
    public string Namespace { get; set; }
    public string ClassName { get; set; }
    public AssemblySource Source { get; set; }  // Current or Referenced

    // Base component info (for using directives)
    public string? BaseComponentType { get; set; }
    public string? BaseComponentNamespace { get; set; }  // ✅ Fixes Issue #1

    // Filterable properties (ALWAYS with "Model." prefix)
    public HashSet<string> FilterableProperties { get; set; }  // ✅ Fixes Issue #2

    // Features
    public bool HasTriggers { get; set; }
}

public enum AssemblySource { Current, Referenced }
```

### Before vs. After

**Before (Fragmented):**
```csharp
RazorCodeBehindGenerator.GenerateComponentFilterCodeBehind(
    spc, razorFile, content,
    componentNamespaces,        // Dictionary 1 - same-assembly only
    componentHasTriggers,       // Dictionary 2 - same-assembly only
    crossAssemblyComponents,    // Dictionary 3 - cross-assembly only
    codeBehindPropertyUsages,   // Dictionary 4 - merged from both
    filterableProperties,       // Dictionary 5 - same-assembly only
    config.RootNamespace);
```

**After (Unified):**
```csharp
RazorCodeBehindGenerator.GenerateComponentFilterCodeBehind(
    spc, razorFile, content,
    generatorContext,  // Single source of truth - ALL assemblies
    config);

// Inside generator - single lookup:
var component = generatorContext.AllComponents[componentName];
var usingDirective = component.BaseComponentNamespace;  // ✅ Always available
var filterProps = component.FilterableProperties;        // ✅ Always has "Model." prefix
var hasTriggers = component.HasTriggers;                 // ✅ Always available
```

---

## Analyzer Impact Assessment

### Current Analyzers

**1. RxBlazorDiagnosticAnalyzer** (Roslyn live analyzer)
- **Dependencies:** None - uses `ObservableModelRecord.Create()` directly
- **Impact:** ✅ **NONE** - completely independent

**2. CodeBehindPropertyAnalyzer** (Generator-time analyzer)
- **Current:** Uses both `componentNamespaces` AND `crossAssemblyComponents`
- **After:** Uses unified `GeneratorContext`
- **Impact:** ⚠️ **SIMPLIFIED** - single lookup instead of parallel checks

**3. ComponentFilterAnalyzer** (Regex pattern matcher)
- **Dependencies:** None - pure regex on razor content
- **Impact:** ✅ **NONE** - no type resolution

**4. ObservableModelAnalyzer** (Syntax filter)
- **Dependencies:** None - syntax-only checks
- **Impact:** ✅ **NONE** - no semantic analysis

**Conclusion:** Only one analyzer needs updating (CodeBehindPropertyAnalyzer), and it becomes **simpler**.

---

## Implementation Plan

### Phase 1: Create Unified Structures ⏱️ 2-3 hours

**New Files:**
```
Models/GeneratorContext.cs          - Core metadata structures
Builders/GeneratorContextBuilder.cs - Context construction logic
```

**No existing code changed** - just new structures.

**Example Structure:**
```csharp
// Models/GeneratorContext.cs
public class GeneratorContext
{
    public Dictionary<string, ComponentMetadata> AllComponents { get; }
    public Dictionary<string, ModelMetadata> AllModels { get; }

    public GeneratorContext(
        Dictionary<string, ComponentMetadata> components,
        Dictionary<string, ModelMetadata> models)
    {
        AllComponents = components;
        AllModels = models;
    }
}

// Builders/GeneratorContextBuilder.cs
public static class GeneratorContextBuilder
{
    public static GeneratorContext Build(
        ImmutableArray<ObservableModelRecord?> currentAssemblyRecords,
        Compilation compilation)
    {
        var components = new Dictionary<string, ComponentMetadata>();
        var models = new Dictionary<string, ModelMetadata>();

        // Step 1: Process current assembly records
        ProcessCurrentAssemblyRecords(currentAssemblyRecords, components, models);

        // Step 2: Process referenced assemblies
        ProcessReferencedAssemblies(compilation, components, models);

        // Step 3: Calculate filterable properties for ALL components
        CalculateFilterablePropertiesForAllComponents(components, models);

        return new GeneratorContext(components, models);
    }

    private static void ProcessReferencedAssemblies(
        Compilation compilation,
        Dictionary<string, ComponentMetadata> components,
        Dictionary<string, ModelMetadata> models)
    {
        // Simplified from FindCrossAssemblyObservableComponents
        var referencedComponents = compilation
            .References
            .Select(r => compilation.GetAssemblyOrModuleSymbol(r))
            .OfType<IAssemblySymbol>()
            .Where(a => ReferencesRxBlazorV2(a))
            .SelectMany(a => GetAllObservableComponents(a.GlobalNamespace));

        foreach (var component in referencedComponents)
        {
            var metadata = CreateComponentMetadata(component, AssemblySource.Referenced);
            components[component.Name] = metadata;
        }
    }
}
```

---

### Phase 2: Update RazorCodeBehindGenerator ⏱️ 1-2 hours

**File:** `Generators/RazorCodeBehindGenerator.cs`

**Changes:**
1. Add new method overload accepting `GeneratorContext`
2. Update logic to use unified lookup
3. **Keep old method temporarily** for comparison

**Key Changes:**
```csharp
// OLD (lines 34-43)
public static void GenerateComponentFilterCodeBehind(
    SourceProductionContext context,
    AdditionalText razorFile,
    SourceText razorContent,
    Dictionary<string, string> componentNamespaces,          // ❌ Same-assembly only
    Dictionary<string, bool> componentHasTriggers,           // ❌ Same-assembly only
    Dictionary<string, (...)> crossAssemblyComponents,       // ❌ Cross-assembly only
    Dictionary<string, HashSet<string>> codeBehindPropertyUsages,
    Dictionary<string, HashSet<string>> filterablePropertiesSSO,
    string rootNamespace)

// NEW
public static void GenerateComponentFilterCodeBehind(
    SourceProductionContext context,
    AdditionalText razorFile,
    SourceText razorContent,
    GeneratorContext generatorContext,   // ✅ Unified - all assemblies
    GeneratorConfig config)
{
    var componentTypeName = ExtractComponentTypeName(inheritsType);

    // ✅ Single lookup - works for both same and cross-assembly
    if (!generatorContext.AllComponents.TryGetValue(componentTypeName, out var component))
    {
        return; // Not an observable component
    }

    // ✅ Automatic using directive for base component
    if (!string.IsNullOrEmpty(component.BaseComponentNamespace))
    {
        usingDirectives.Add(component.BaseComponentNamespace);
    }

    // ✅ Properties already have "Model." prefix
    var filterProperties = component.FilterableProperties;

    // ✅ Triggers available for all components
    var hasTriggers = component.HasTriggers;
}
```

---

### Phase 3: Update CodeBehindPropertyAnalyzer ⏱️ 1 hour

**File:** `Analyzers/CodeBehindPropertyAnalyzer.cs`

**Changes:**
```csharp
// OLD (lines 18-21)
public static Dictionary<string, HashSet<string>> AnalyzeCodeBehindPropertyUsage(
    Compilation compilation,
    Dictionary<string, string> componentNamespaces,          // ❌ Fragmented
    Dictionary<string, (...)> crossAssemblyComponents)       // ❌ Fragmented

// NEW
public static Dictionary<string, HashSet<string>> AnalyzeCodeBehindPropertyUsage(
    Compilation compilation,
    GeneratorContext context)                                 // ✅ Unified
{
    // Simplified inheritance check
    var componentBaseName = GetObservableComponentBaseName(classSymbol, context);

    // OLD: Had to check TWO dictionaries
    // NEW: Single unified lookup
}

private static string? GetObservableComponentBaseName(
    INamedTypeSymbol classSymbol,
    GeneratorContext context)                                 // ✅ Much simpler
{
    var baseType = classSymbol.BaseType;
    while (baseType is not null)
    {
        // ✅ Single check instead of two parallel checks
        if (context.AllComponents.ContainsKey(baseType.Name))
        {
            return baseType.Name;
        }
        baseType = baseType.BaseType;
    }
    return null;
}
```

---

### Phase 4: Update Main Generator Pipeline ⏱️ 1-2 hours

**File:** `RxBlazorGenerator.cs` (lines 456-515)

**Changes:**
```csharp
// OLD: Build 5 separate dictionaries
context.RegisterSourceOutput(allRazorFiles.Combine(processedRecords)...,
    static (spc, combined) =>
    {
        var crossAssemblyComponents = compilation.FindCrossAssemblyObservableComponents();

        var componentNamespaces = new Dictionary<string, string>();
        var componentHasTriggers = new Dictionary<string, bool>();
        foreach (var record in records.Where(r => r?.ComponentInfo != null)) { ... }

        var filterableProperties = FilterablePropertiesBuilder.BuildFilterablePropertiesForComponents(records);
        var codeBehindPropertyUsages = CodeBehindPropertyAnalyzer.AnalyzeCodeBehindPropertyUsage(...);

        RazorCodeBehindGenerator.GenerateComponentFilterCodeBehind(
            spc, razorFile, content,
            componentNamespaces, componentHasTriggers, crossAssemblyComponents,
            codeBehindPropertyUsages, filterableProperties, config.RootNamespace);
    });

// NEW: Build unified context once
context.RegisterSourceOutput(allRazorFiles.Combine(processedRecords)...,
    static (spc, combined) =>
    {
        // ✅ Single unified context
        var generatorContext = GeneratorContextBuilder.Build(records, compilation);

        foreach (var razorFile in razorFilesList)
        {
            RazorCodeBehindGenerator.GenerateComponentFilterCodeBehind(
                spc, razorFile, content, generatorContext, config);
        }
    });
```

---

### Phase 5: Remove Obsolete Code ⏱️ 1 hour

**Files to Delete:**
- `Extensions/ComponentDetectionExtensions.cs` - Replace with GeneratorContextBuilder
- `Helpers/FilterablePropertiesBuilder.cs` - Merge into GeneratorContextBuilder

**Files to Clean:**
- `RxBlazorGenerator.cs` - Remove old dictionary building code
- `RazorCodeBehindGenerator.cs` - Remove old method overload

---

### Phase 6: Testing ⏱️ 2-3 hours

**Test Cases:**

1. **Same-Assembly Component** (e.g., PushManager in WebAppBase.Shared)
   - ✅ Verify using directives correct
   - ✅ Verify "Model." prefix on all properties
   - ✅ Verify triggers detected

2. **Cross-Assembly Component** (e.g., TodoManager inherits StatusModelComponent)
   - ✅ Verify using directive for StatusModelComponent added
   - ✅ Verify "Model." prefix on all properties
   - ✅ Verify base component properties included

3. **Multiple Inheritance Levels**
   - ✅ Verify all base component namespaces added
   - ✅ Verify properties from all levels included

4. **Performance**
   - ✅ Verify build time not significantly impacted
   - ✅ Verify incremental compilation still works

---

## Benefits Summary

| Benefit | Impact |
|---------|--------|
| **Correctness** | ✅ Fixes cross-assembly using directive bug |
| **Correctness** | ✅ Fixes cross-assembly "Model." prefix bug |
| **Simplicity** | ✅ One code path vs. two parallel systems |
| **Simplicity** | ✅ ~200 lines complex code → ~100 lines simple code |
| **Maintainability** | ✅ Single source of truth |
| **Maintainability** | ✅ Easier to add new features |
| **Debuggability** | ✅ All metadata in one place |
| **Performance** | ⚠️ Slightly more upfront (build context once) |
| **Performance** | ✅ Faster lookups during generation |

---

## Risks and Mitigation

| Risk | Mitigation |
|------|------------|
| Breaking existing functionality | Keep old methods during transition, compare outputs |
| Performance regression | Benchmark before/after, optimize if needed |
| Test coverage gaps | Add comprehensive unit tests for GeneratorContext |
| Complex refactoring | Incremental phases, each independently testable |

---

## Timeline Estimate

| Phase | Time | Cumulative |
|-------|------|------------|
| Phase 1: New structures | 2-3 hours | 2-3 hours |
| Phase 2: RazorCodeBehindGenerator | 1-2 hours | 3-5 hours |
| Phase 3: CodeBehindPropertyAnalyzer | 1 hour | 4-6 hours |
| Phase 4: Main pipeline | 1-2 hours | 5-8 hours |
| Phase 5: Cleanup | 1 hour | 6-9 hours |
| Phase 6: Testing | 2-3 hours | 8-12 hours |

**Total: 8-12 hours** for complete refactoring with testing.

---

## Decision

**Recommendation:** ✅ **Proceed with unified architecture refactoring**

**Rationale:**
1. Fixes critical bugs immediately
2. Significantly simplifies codebase
3. Minimal impact on existing analyzers (only one needs update, becomes simpler)
4. Low risk with incremental approach
5. Strong foundation for future improvements

---

## Next Steps to Resume

1. Start with Phase 1: Create `Models/GeneratorContext.cs`
2. Create `Builders/GeneratorContextBuilder.cs`
3. Add unit tests for context building
4. Proceed through phases incrementally
5. Compare generated output at each step

---

## Files Analyzed

Total: 41 source files in RxBlazorV2Generator

**Key Files:**
- `RxBlazorGenerator.cs` - Main generator pipeline
- `Generators/RazorCodeBehindGenerator.cs` - Razor code-behind generation
- `Analyzers/CodeBehindPropertyAnalyzer.cs` - Code-behind analysis
- `Extensions/ComponentDetectionExtensions.cs` - Cross-assembly detection (to replace)
- `Helpers/FilterablePropertiesBuilder.cs` - Property filtering (to replace)
- `Analysis/ObservableModelRecord.cs` - Model analysis (no changes needed)

---

**Generated:** 2025-10-26
**Author:** Claude (Anthropic)
**Context:** Investigation from WebAppBase workspace
**Status:** Ready for implementation in RxBlazorV2 workspace

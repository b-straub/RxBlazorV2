---
name: rxblazor-expert
description: Use when the user asks about RxBlazorV2 reactive patterns, architecture decisions, model vs service boundaries, command design, cross-model communication, DI patterns, or mentions ObservableModel, ObservableCommand, ObservableTrigger, ObservableComponent. Also use when reviewing or designing reactive Blazor models.
user-invocable: true
argument-hint: "[question or scenario]"
---

# RxBlazorV2 Expert Guide

You are an expert architect for **RxBlazorV2**, a reactive programming framework for Blazor built on R3 with Roslyn source generation.

## Step 1: Load Reference Documentation

Read these bundled references (relative to this skill file):

1. **Pattern catalog**: `references/reactive-patterns.md` -- complete pattern reference with examples
2. **API reference**: `references/RxBlazorV2-api.xml` -- core library XML docs (attributes, base classes, interfaces, commands)
3. **MudBlazor integration**: `references/RxBlazorV2.MudBlazor-api.xml` -- reactive button components and status model
4. **Common scenarios**: `references/common-scenarios.md` -- architecture examples with before/after code

## Step 2: Apply Architectural Principles

These principles override any pattern choice. Apply them before recommending a pattern.

### The Core Rule

**Reactive models manage UI state. Services contain domain logic.**

Ask: "Can I describe this logic without mentioning UI?" If yes, it belongs in a service.

### Command Atomicity

Commands should be self-contained. Everything a command triggers should complete within the command, using direct service calls — not reactive property signals to other models.

```csharp
// CORRECT: Atomic command with direct service calls
private async Task SaveAsync()
{
    var result = await DataService.SaveAsync(Item);
    if (result.Success)
    {
        await SyncService.PushAsync();  // Same command, direct call
        Status = "Saved";
    }
}
```

### The Property Signal Anti-Pattern

Properties are **state**, not **events**. Never use a property change as a signal for cross-model workflow orchestration.

**Why it breaks:**
- Property setters fire on every assignment, not just meaningful transitions
- Setting `Status = null` then `Status = "Done"` fires the observer twice
- No guarantee of ordering or exactly-once delivery
- Creates invisible coupling between models

**What to do instead:**
- For workflows: direct service calls within the command
- For UI reactions: `[ObservableComponentTrigger]` (component-level)
- For same-model reactions: `[ObservableTrigger]` (property-level)
- For fire-and-forget: `[ObservableModelObserver]` on a service

## Step 3: Pattern Decision Tree

Walk the user through this decision tree:

```
"I need to react to something"
  │
  ├─ Same model's property changed?
  │   └─ YES → [ObservableTrigger] or [ObservableTriggerAsync]
  │
  ├─ UI component needs to update?
  │   └─ YES → [ObservableComponentTrigger] or [ObservableComponentTriggerAsync]
  │
  ├─ Another model's UI state changed?
  │   ├─ Is this a UI state reaction? (e.g., theme changed → update display)
  │   │   └─ YES → Internal observer (auto-detected private method)
  │   └─ Is this a domain workflow? (e.g., save completed → sync to server)
  │       └─ YES → Direct service call in the originating command
  │
  ├─ Need fire-and-forget side effect? (logging, persistence, analytics)
  │   └─ YES → [ObservableModelObserver] on a service
  │
  └─ Need to auto-execute a command when property changes?
      └─ YES → [ObservableCommandTrigger]
```

## Step 4: Architecture Review Checklist

When reviewing or designing models, check:

1. **Model size**: Over 300 lines? Likely contains domain logic → extract services
2. **Constructor params**: More than 5 services? Model is doing too much → split responsibilities
3. **Duplicate methods**: Same helper in 2+ models? → Extract to shared service
4. **Cross-model property watching**: Model observes another model's property to trigger workflow? → Move workflow into originating command
5. **Service boundary**: Cross-project dependency? → Interface in consuming project, implementation in providing project

## Step 5: Respond to the User

### Format

1. **Pattern Identification**: Name the pattern(s) that apply
2. **Architecture Check**: Flag any violations of the core principles
3. **Recommended Implementation**: Concrete code using proper attributes
4. **Anti-Pattern Warnings**: If the user's approach has issues, explain why and show the fix

### Quick Reference Table

| I want to... | Use | Attribute/Mechanism |
|---|---|---|
| React to my own property change | Property Trigger | `[ObservableTrigger]` |
| React to injected model's UI state | Internal Observer | Auto-detected private method |
| Bind button to method | Command | `[ObservableCommand]` |
| Auto-run command on property change | Command Trigger | `[ObservableCommandTrigger]` |
| React in UI component to model change | Component Trigger | `[ObservableComponentTrigger]` |
| Fire-and-forget side effect | External Observer | `[ObservableModelObserver]` on service |
| Multi-step domain workflow | Direct service call | Call services within command |
| Define reactive contract for inheritance | Abstract Base Class | Abstract properties with trigger attributes |

### Common Scenarios with Solutions

See [references/common-scenarios.md](references/common-scenarios.md) for detailed examples of:
- Cross-model communication patterns
- Multi-project service extraction
- Command composition
- Collection observation patterns

$ARGUMENTS

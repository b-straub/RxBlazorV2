---
name: rxblazor-audit
description: Use when the user asks to audit, review, or check a project for RxBlazorV2 anti-patterns, or when they want to find reactive issues in their codebase.
user-invocable: true
argument-hint: "[project path]"
---

# RxBlazorV2 Reactive Audit

Scan a project for reactive anti-patterns and output a prioritized TODO list with correct solutions.

## Instructions

**First, load bundled reference documentation (relative to this skill file):**
- `references/reactive-patterns.md` -- complete pattern reference
- `references/RxBlazorV2-api.xml` -- core library API (attributes, commands, interfaces)
- `references/RxBlazorV2.MudBlazor-api.xml` -- MudBlazor reactive button components

**Then scan the project at:** $ARGUMENTS

### Step 1: Discovery

Find all files containing reactive patterns:
1. All `*.cs` files with `ObservableModel`, `ObservableComponent`, or `ObservableCommand`
2. All `*.razor` files that inherit from `*Component` base classes
3. All `*.razor` files with `@code` blocks (check for local state anti-patterns)
4. All services with `[ObservableModelObserver]` attributes

### Step 2: Detect Issues by Severity

**CRITICAL (must fix):**
- `StateHasChanged()` — Manual calls bypass reactive system. Use partial properties. When found in a component hook method (e.g., `OnXChanged()`), note that `RenderAndHook` already re-renders -- the call is redundant.
- `_ = *.ExecuteAsync()` or `_ = SomeAsync()` — Fire-and-forget async loses exceptions and bypasses cancellation. Use proper command binding or `[ObservableTriggerAsync]`.
- `[ObservableTrigger]` with sync method that calls `_ = AsyncMethod()` inside — Sync trigger wrapping async work. Use `[ObservableTriggerAsync]` so the generator manages the async subscription with proper error handling and cancellation.
- `InvokeAsync(() =>` in model code — Should be in component only.
- `[ObservableModelObserver]` methods that call back to model (`model.Set*`, `model.Notify*`) — External observer orchestrating workflow. Use command pattern instead.
- Local mutable fields in `*ModelComponent` — Private fields (`bool _isX`, `string _error`, `List<T> _items`) set in async methods inside a component that inherits from a generated `*ModelComponent`. These should be partial properties on a model with proper commands. The component is bypassing the reactive system entirely.
- Manual `_isLoading`/`_isProcessing` booleans — Hand-rolled loading state (`IsLoading = true; try { ... } finally { IsLoading = false; }`) when `IObservableCommandAsync.Executing` provides this automatically. Use command binding and bind to `Command.Executing` in the UI.

**ARCHITECTURE (design issue):**
- Model over 300 lines — Likely contains domain logic. Extract to services.
- Constructor with 5+ non-model parameters — Model doing too much. Split responsibilities.
- Same helper method in 2+ models — Extract to shared service.
- Property used as cross-model event signal — Use direct service call in command instead.
- Reactive chain: property change -> observer -> property change -> observer — Use direct calls for workflows.
- Reactive component with 2+ `@inject` services AND async logic — Component has complex behavior that belongs in its own model. Create a dedicated `*Model` with commands for the async operations, and let the component inherit from the generated `*ModelComponent`.

**WARNING (likely wrong):**
- `Property = !Property` or `Property++` as only mutation — Toggle/counter notification signal. Use semantic status property.
- Methods named `Notify*` that only set a property — Indirect notification pattern. Inline into the source.
- Deep property paths `Model.Sub.Property` (3+ levels) — Should inject the leaf model directly.
- `EventCallback` handler that only calls `StateHasChanged()` — Dead code. In reactive components, the model already triggers re-renders. Remove the callback chain.
- `EventCallback` handler that doesn't use the received value — Dead EventCallback chain. The handler ignores the data passed to it, suggesting the callback is unnecessary or the handler is a no-op.

**SUGGESTION (review needed):**
- Private methods accessing referenced model properties without clear purpose — May be accidental observer.
- Commands without CanExecute that modify shared state — Add guard condition.
- Public methods on models that should be private command methods — Encapsulate via command.
- `[ObservableTriggerAsync]` method without `CancellationToken` parameter — Missing cancellation support. The generator's `SubscribeAwait` provides a token for disposal/switch cancellation; the method should accept and propagate it.
- Standard `MudButton`/`MudIconButton` with `OnClick` calling a command — When the project references `RxBlazorV2.MudBlazor`, use the reactive button components instead. See replacements table below.

### MudBlazor Reactive Button Replacements

When the project references `RxBlazorV2.MudBlazor`, flag standard MudBlazor buttons that manually invoke commands and suggest the reactive replacements:

| Instead of | Use | Benefit |
|---|---|---|
| `<MudButton OnClick="@(() => Model.Command.Execute())">` | `<MudButtonRx Command="@Model.Command">` | Auto-disables via CanExecute |
| `<MudButton OnClick="@(async () => await Model.Command.ExecuteAsync())">` | `<MudButtonAsyncRx Command="@Model.Command">` | Auto-disables, shows progress, supports cancel |
| `<MudButton Disabled="@(!CanDoThing)" OnClick="@DoThing">` | `<MudButtonRx Command="@Model.ThingCommand">` | CanExecute handled by command |
| `<MudIconButton OnClick="@(() => Model.Command.Execute())">` | `<MudIconButtonRx Command="@Model.Command">` | Same benefits for icon buttons |
| `<MudIconButton OnClick="@(async () => await Model.Command.ExecuteAsync())">` | `<MudIconButtonAsyncRx Command="@Model.Command">` | Badge-based progress indicator |
| Parameterized: `OnClick="@(() => Model.Command.Execute(item))"` | `<MudButtonRxOf T="ItemType" Command="@Model.Command" Parameter="@item">` | Type-safe parameter binding |

### Step 3: Output TODO List

```markdown
# Reactive Audit: [ProjectName]

## Critical Issues

### 1. [File:Line] - [Issue Type]
**Current:**
\`\`\`csharp
// problematic code
\`\`\`

**Problem:** [Why this is wrong -- reference the specific principle]

**Solution:**
\`\`\`csharp
// corrected code using proper pattern
\`\`\`

---

## Architecture Issues

### ...

## Warnings

### ...

## Summary
- Critical: N issues
- Architecture: N issues
- Warnings: N issues
- Suggestions: N issues

## Key Principles Applied
1. Models manage UI state, services contain domain logic
2. Commands are atomic -- direct service calls, not reactive chains
3. Properties are state, not events
4. External observers are fire-and-forget only
```

### Key Principles

1. **Models manage UI state, services contain domain logic**
2. **Commands are atomic** -- everything completes within the command
3. **Properties are state, not events** -- don't use property changes as cross-model signals
4. **External observers are fire-and-forget only** -- persistence, logging, analytics
5. **Every `StateHasChanged()` call is suspicious** -- find the reactive alternative
6. **Fat models need splitting** -- extract services, keep models under 300 lines
7. **Components don't own async state** -- if a component has loading booleans, local collections, or try/catch/finally around async calls, it needs a model
8. **Command `.Executing` replaces manual loading flags** -- never hand-roll `_isLoading = true/false`

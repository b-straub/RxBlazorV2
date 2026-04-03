---
name: rxblazor-audit
description: Use when the user asks to audit, review, or check a project for RxBlazorV2 anti-patterns, or when they want to find reactive issues in their codebase.
user-invocable: true
argument-hint: "[project path]"
---

# RxBlazorV2 Reactive Audit

Scan a project for reactive anti-patterns and output a prioritized TODO list with correct solutions.

## Instructions

**First, load reference documentation:**
- Read `docs/REACTIVE_PATTERNS.md` from the repo root for the complete pattern reference
- Read `CLAUDE.md` from the repo root for architecture principles

**Then scan the project at:** $ARGUMENTS

### Step 1: Discovery

Find all files containing reactive patterns:
1. All `*.cs` files with `ObservableModel`, `ObservableComponent`, or `ObservableCommand`
2. All `*.razor` files that inherit from `*Component` base classes
3. All services with `[ObservableModelObserver]` attributes

### Step 2: Detect Issues by Severity

**CRITICAL (must fix):**
- `StateHasChanged()` — Manual calls bypass reactive system. Use partial properties.
- `_ = *.ExecuteAsync()` or `_ = SomeAsync()` — Fire-and-forget async loses exceptions and bypasses cancellation. Use proper command binding or `[ObservableTriggerAsync]`.
- `[ObservableTrigger]` with sync method that calls `_ = AsyncMethod()` inside — Sync trigger wrapping async work. Use `[ObservableTriggerAsync]` so the generator manages the async subscription with proper error handling and cancellation.
- `InvokeAsync(() =>` in model code — Should be in component only.
- `[ObservableModelObserver]` methods that call back to model (`model.Set*`, `model.Notify*`) — External observer orchestrating workflow. Use command pattern instead.

**ARCHITECTURE (design issue):**
- Model over 300 lines — Likely contains domain logic. Extract to services.
- Constructor with 5+ non-model parameters — Model doing too much. Split responsibilities.
- Same helper method in 2+ models — Extract to shared service.
- Property used as cross-model event signal — Use direct service call in command instead.
- Reactive chain: property change → observer → property change → observer — Use direct calls for workflows.

**WARNING (likely wrong):**
- `Property = !Property` or `Property++` as only mutation — Toggle/counter notification signal. Use semantic status property.
- Methods named `Notify*` that only set a property — Indirect notification pattern. Inline into the source.
- Deep property paths `Model.Sub.Property` (3+ levels) — Should inject the leaf model directly.

**SUGGESTION (review needed):**
- Private methods accessing referenced model properties without clear purpose — May be accidental observer.
- Commands without CanExecute that modify shared state — Add guard condition.
- Public methods on models that should be private command methods — Encapsulate via command.
- `[ObservableTriggerAsync]` method without `CancellationToken` parameter — Missing cancellation support. The generator's `SubscribeAwait` provides a token for disposal/switch cancellation; the method should accept and propagate it.

### Step 3: Output TODO List

```markdown
# Reactive Audit: [ProjectName]

## Critical Issues

### 1. [File:Line] - [Issue Type]
**Current:**
\`\`\`csharp
// problematic code
\`\`\`

**Problem:** [Why this is wrong — reference the specific principle]

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
2. Commands are atomic — direct service calls, not reactive chains
3. Properties are state, not events
4. External observers are fire-and-forget only
```

### Key Principles

1. **Models manage UI state, services contain domain logic**
2. **Commands are atomic** — everything completes within the command
3. **Properties are state, not events** — don't use property changes as cross-model signals
4. **External observers are fire-and-forget only** — persistence, logging, analytics
5. **Every `StateHasChanged()` call is suspicious** — find the reactive alternative
6. **Fat models need splitting** — extract services, keep models under 300 lines

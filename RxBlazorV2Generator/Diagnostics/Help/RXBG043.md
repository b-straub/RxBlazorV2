# RXBG043: Invalid Observable Component Batch Declaration

## Description

This diagnostic is reported when an `[ObservableComponentBatchAsync]` batch cannot be turned into a component hook, either because the batch id is not usable as a method name or because a member declares a negative debounce window.

## Cause

A batch does two things that put requirements on its arguments: the id names a generated method, and each member's window feeds `Debounce(TimeSpan.FromMilliseconds(window))`.

### Cause 1: Batch Id Is Not a Valid C# Identifier

```csharp
// ❌ RXBG043: 'search results' cannot become On...BatchChangedAsync
[ObservableComponentBatchAsync("search results", 250)]
public partial string SearchTerm { get; set; } = "";
```

The id becomes the hook name via `On{PascalCase(BatchId)}BatchChangedAsync`, so it must be a valid, non-keyword C# identifier. Ids containing spaces, punctuation, a leading digit, or a reserved word are rejected.

**Message:**

```
RXBG043: Batch 'search results' is invalid: the batch id is not a valid C# identifier, so no hook
method name can be formed. No hook method is generated for this batch.
```

### Cause 2: Negative Debounce Window

```csharp
// ❌ RXBG043: negative window
[ObservableComponentBatchAsync("search", -250)]
public partial string SearchTerm { get; set; } = "";
```

**Message:**

```
RXBG043: Batch 'search' is invalid: the debounce window is negative (-250 ms).
No hook method is generated for this batch.
```

The window is not clamped to zero — silently reinterpreting a negative value as "no debounce" would hide a typo that changes behaviour.

## How to Fix

### Fix 1: Use an Identifier-Safe Batch Id

```csharp
// ✅ Valid identifier → OnSearchResultsBatchChangedAsync
[ObservableComponentBatchAsync("searchResults", 250)]
public partial string SearchTerm { get; set; } = "";

[ObservableComponentBatchAsync("searchResults")]
public partial bool HighlightMatches { get; set; }
```

Note that `[ObservableBatch]` — the unrelated attribute that groups properties for `SuspendNotifications` — has no such constraint, because it generates nothing:

```csharp
// ✅ Fine: no hook name is derived from an [ObservableBatch] id
[ObservableBatch("search results")]
public partial string SearchTerm { get; set; } = "";
```

### Fix 2: Use a Non-Negative Window

```csharp
// ✅ Debounced: typing settles before the hook runs
[ObservableComponentBatchAsync("search", 250)]
public partial string SearchTerm { get; set; } = "";

// ✅ Immediate: a discrete action such as a switch click
[ObservableComponentBatchAsync("search")]
public partial bool HighlightMatches { get; set; }
```

## Hook Naming

| Batch id | Generated hook | Valid |
|---|---|---|
| `search` | `OnSearchBatchChangedAsync` | ✅ |
| `searchResults` | `OnSearchResultsBatchChangedAsync` | ✅ |
| `_filters` | `On_filtersBatchChangedAsync` | ✅ |
| `search results` | — | ❌ contains a space |
| `search-results` | — | ❌ contains a hyphen |
| `2ndPass` | — | ❌ starts with a digit |
| `class` | — | ❌ reserved keyword |

## What Is *Not* an Error

Members of one batch declaring **different** debounce windows is the whole point of the per-property window, not a conflict:

```csharp
// ✅ The typed term settles; the switch reloads at once. Both feed OnSearchBatchChangedAsync.
[ObservableComponentBatchAsync("search", 250)]
public partial string SearchTerm { get; set; } = "";

[ObservableComponentBatchAsync("search")]
public partial bool HighlightMatches { get; set; }
```

The generator emits one filtered stream per distinct window and merges them into a single subscription.

## Severity

**Error** — the batch is skipped and no hook method is generated, so any `override` of the expected hook fails to compile.

## Related Diagnostics

- [RXBG044](RXBG044.md): Observable component batch has no effect

## See Also

- `[ObservableComponentBatchAsync]` attribute documentation
- `[ObservableBatch]` — the unrelated notification-grouping attribute
- Component Batch Triggers in the reactive patterns guide

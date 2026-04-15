# RXBG043: Non-Observable Collection Type on Partial Property

## Description

This diagnostic is reported when a partial property in an `ObservableModel` uses a non-observable collection type such as `List<T>`, `IList<T>`, `Collection<T>`, or `HashSet<T>`. These types do not fire reactive notifications when items are mutated (Add, Remove, Clear), making collection changes invisible to the reactive system.

## Cause

Mutable collection types from `System.Collections.Generic` do not integrate with the R3/ObservableCollections reactive pipeline. Only full property reassignment (`Emails = newList`) triggers a property change notification — individual mutations are silently lost.

This is especially dangerous when combined with `[ObservableComponentTrigger]`, as the trigger only fires on property reassignment, not on item-level mutations.

## How to Fix

Replace with the corresponding reactive collection type from ObservableCollections and use `init` accessor:

| Non-Observable Type | Replacement |
|---|---|
| `List<T>`, `IList<T>`, `Collection<T>`, `ICollection<T>` | `ObservableList<T>` |
| `HashSet<T>`, `ISet<T>` | `ObservableHashSet<T>` |
| `Dictionary<K,V>`, `IDictionary<K,V>` | `ObservableDictionary<K,V>` |

### Example

```csharp
// Before: broken - mutations are invisible
[ObservableComponentTrigger]
public partial List<Email> Emails { get; set; } = [];

// After: reactive - each Add/Remove fires notifications
[ObservableComponentTrigger(ComponentTriggerType.HookOnly)]
public partial ObservableList<Email> Emails { get; init; } = [];
```

Use `init` instead of `set` because `ObservableList<T>` gets reactivity from observing the collection itself, not from property reassignment.

## Severity

**Error** — Collection mutations are silently lost, which causes UI state to become stale.

## Related Diagnostics

- RXBG040: Invalid init accessor on non-observable collection property
- RXBG042: Redundant ObservableComponentTrigger on razor-observed property

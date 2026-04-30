# RXBG091: Error formatter method not found

## Description

The third positional argument of `[ObservableCommand]` names a method that
maps an `Exception` to a user-facing string. The named method must exist
on the declaring type or one of its base types.

## Cause

The `nameof(...)` (or string literal) supplied as the third argument does
not resolve to any member named that on the model.

```csharp
// WRONG: no method named "FormatLoadError" exists on the model
[ObservableCommand(nameof(LoadAsync), nameof(CanLoad), "FormatLoadError")]
public partial IObservableCommandAsync LoadCommand { get; }
```

## How to Fix

Add a method matching the signature `string Method(Exception)`:

```csharp
[ObservableCommand(nameof(LoadAsync), nameof(CanLoad), nameof(FormatLoadError))]
public partial IObservableCommandAsync LoadCommand { get; }

private string FormatLoadError(Exception ex) =>
    $"Failed to load: {ex.Message}";
```

Or remove the third argument if you don't need a per-command formatter —
the framework will fall back to `ex.Message` as today.

A code fix is offered in the IDE that scaffolds a stub method.

## Severity

**Error** — code generation for the command property is skipped until
the formatter resolves.

## Related Diagnostics

- RXBG092: Error formatter method has invalid signature

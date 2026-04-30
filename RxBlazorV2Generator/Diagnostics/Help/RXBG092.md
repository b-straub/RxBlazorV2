# RXBG092: Error formatter method has invalid signature

## Description

The method named as the third positional argument of `[ObservableCommand]`
exists on the model, but does not match the required signature
`string Method(Exception)`. The framework calls it with a single
`Exception` argument and expects a `string` return value.

## Cause

A method with that name was found, but at least one of these is wrong:

- Return type is not `string`
- Parameter count is not exactly 1
- Parameter type is not `System.Exception`

```csharp
// WRONG: returns void
private void FormatLoadError(Exception ex) { }

// WRONG: extra parameter
private string FormatLoadError(Exception ex, int extra) => ex.Message;

// WRONG: parameter is a derived exception
private string FormatLoadError(InvalidOperationException ex) => ex.Message;
```

## How to Fix

Make the signature `string Method(Exception ex)`:

```csharp
private string FormatLoadError(Exception ex) => ex switch
{
    OperationCanceledException => "Load was cancelled.",
    HttpRequestException http  => $"Network error: {http.Message}",
    _                          => $"Failed to load: {ex.Message}",
};
```

Static methods are also accepted:

```csharp
private static string FormatLoadError(Exception ex) =>
    $"Failed to load: {ex.Message}";
```

## Severity

**Error** — code generation for the command property is skipped until
the formatter has a valid signature.

## Related Diagnostics

- RXBG091: Error formatter method not found

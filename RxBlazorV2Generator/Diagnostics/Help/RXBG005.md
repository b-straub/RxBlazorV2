# RXBG005: Razor File Read Error

## Description

This diagnostic is reported when the source generator cannot read a Razor component file. This indicates an I/O or file access issue during generator execution.

## Cause

This error occurs when:
- A Razor file cannot be accessed or read
- File permissions prevent reading
- The file path is invalid or the file doesn't exist
- There are encoding or format issues with the file

## How to Fix

1. Verify that the Razor file exists and is accessible
2. Check file permissions
3. Ensure the file is not locked by another process
4. Verify the file encoding is correct (UTF-8 is recommended)
5. Try cleaning and rebuilding the project

## Example

```razor
@* YourComponent.razor *@
@inherits ObservableComponent<YourModel>

<div>
    @* Component content *@
</div>
```

If you receive this error, check that the file system allows reading this file.

## Related Diagnostics

- RXBG002: Razor component analysis error

# RxBlazorV2.MudBlazor

Reactive MudBlazor button components for [RxBlazorV2](https://github.com/b-straub/RxBlazorV2). Provides automatic progress indicators, cancellation support, and confirmation dialogs for command bindings.

## Installation

```bash
dotnet add package RxBlazorV2.MudBlazor
```

## Components

| Component | Description |
|-----------|-------------|
| `MudButtonRx` | Sync command button |
| `MudButtonAsyncRx` | Async command button with progress/cancel |
| `MudButtonRxOf<T>` | Parameterized sync command button |
| `MudButtonAsyncRxOf<T>` | Parameterized async command button |
| `MudIconButtonRx` | Sync icon button |
| `MudIconButtonAsyncRx` | Async icon button with badge progress |
| `MudIconButtonRxOf<T>` | Parameterized sync icon button |
| `MudIconButtonAsyncRxOf<T>` | Parameterized async icon button |
| `MudFabRx` | Sync floating action button |
| `MudFabAsyncRx` | Async FAB with progress |
| `MudFabRxOf<T>` | Parameterized sync FAB |
| `MudFabAsyncRxOf<T>` | Parameterized async FAB |

## Usage

### Basic Async Button with Progress

```razor
<MudButtonAsyncRx Command="@Model.SaveCommand"
                  Variant="Variant.Filled"
                  Color="Color.Primary">
    Save
</MudButtonAsyncRx>
```

### With Cancellation Support

```razor
<MudButtonAsyncRx Command="@Model.LongRunningCommand"
                  CancelText="Cancel"
                  CancelColor="Color.Warning">
    Start Process
</MudButtonAsyncRx>
```

### With Confirmation Dialog

```razor
<MudButtonAsyncRx Command="@Model.DeleteCommand"
                  ConfirmExecutionAsync="@ConfirmDeleteAsync"
                  Color="Color.Error">
    Delete
</MudButtonAsyncRx>

@code {
    private async Task<bool> ConfirmDeleteAsync()
    {
        return await DialogService.ShowMessageBox(
            "Confirm Delete",
            "Are you sure you want to delete this item?",
            yesText: "Delete", cancelText: "Cancel") == true;
    }
}
```

### Parameterized Command

```razor
@foreach (var item in Items)
{
    <MudButtonAsyncRxOf T="ItemModel"
                        Command="@Model.ProcessItemCommand"
                        Parameter="@item">
        Process @item.Name
    </MudButtonAsyncRxOf>
}
```

### Icon Button with Progress Badge

```razor
<MudIconButtonAsyncRx Command="@Model.RefreshCommand"
                      Icon="@Icons.Material.Filled.Refresh"
                      HasProgress="true" />
```

## Parameters

All async button components support:

| Parameter | Type | Description |
|-----------|------|-------------|
| `Command` | `IObservableCommandAsync` | Required. The command to execute |
| `CanExecute` | `Func<bool>` | Additional execution guard |
| `ConfirmExecutionAsync` | `Func<Task<bool>>` | Confirmation before execution |
| `CancelText` | `string` | Text for cancel mode (enables cancellation) |
| `CancelColor` | `Color` | Button color during cancel mode |
| `HasProgress` | `bool` | Show progress spinner (default: true) |

Parameterized versions (`*RxOf<T>`) also require:

| Parameter | Type | Description |
|-----------|------|-------------|
| `Parameter` | `T` | The value to pass to the command |

## Requirements

- .NET 10.0+
- [RxBlazorV2](https://www.nuget.org/packages/RxBlazorV2) 1.0.0+
- [MudBlazor](https://www.nuget.org/packages/MudBlazor) 8.0.0+

## License

MIT License - see [LICENSE](https://github.com/b-straub/RxBlazorV2/blob/master/LICENSE) for details.

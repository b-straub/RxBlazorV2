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
| `StatusDisplay` | Error and message display with snackbar/icon |
| `MudSwipeoutRx<TItem>` | Row with reveal-on-swipe action panels (left/right), overswipe + swipe-to-delete |
| `MudSortableSwipeoutListRx<TItem>` | Reactive sortable list, coordinates with child swipeouts |

## StatusDisplay Component

The `StatusDisplay` component provides reactive error and status message handling with configurable display modes.

### Setup

Add the `StatusDisplay` component to your layout (e.g., in the AppBar):

```razor
@using RxBlazorV2.MudBlazor.Components

<MudAppBar>
    <MudSpacer />
    <StatusDisplay />
</MudAppBar>
```

### StatusModel

Inject `StatusModel` into your models to report errors and messages:

```csharp
public partial class MyModel : ObservableModel
{
    public partial MyModel(StatusModel statusModel);

    private void DoSomething()
    {
        StatusModel.AddMessage("Operation completed");
    }

    private void HandleError()
    {
        // Errors are automatically captured from commands via IErrorModel
        // Or add manually:
        StatusModel.HandleError(new Exception("Something went wrong"));
    }
}
```

### Display Modes

| Mode | Description |
|------|-------------|
| `SNACKBAR` | Show only snackbar notification |
| `ICON` | Show only icon with badge and tooltip |
| `SNACKBAR_AND_ICON` | Show both snackbar and icon |

### Message Modes

| Mode | Description |
|------|-------------|
| `AGGREGATE` | Collect all messages (default for errors) |
| `SINGLE` | Clear previous before adding new (default for messages) |

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ErrorDisplayMode` | `StatusDisplayMode` | `SNACKBAR_AND_ICON` | How errors are displayed |
| `ErrorMode` | `StatusMessageMode` | `AGGREGATE` | Error accumulation mode |
| `ErrorSnackbarOptions` | `Action<SnackbarOptions>?` | Hide close icon | Snackbar configuration |
| `MessageDisplayMode` | `StatusDisplayMode` | `SNACKBAR` | How messages are displayed |
| `MessageMode` | `StatusMessageMode` | `SINGLE` | Message accumulation mode |
| `MessageSnackbarOptions` | `Action<SnackbarOptions>?` | Hide close icon | Snackbar configuration |
| `SnackbarPositionClass` | `string` | `TopEnd` | Snackbar position |

### Customization Example

```razor
<StatusDisplay ErrorDisplayMode="StatusDisplayMode.ICON"
               MessageDisplayMode="StatusDisplayMode.SNACKBAR_AND_ICON"
               ErrorMode="StatusMessageMode.AGGREGATE"
               MessageMode="StatusMessageMode.SINGLE"
               SnackbarPositionClass="@Defaults.Classes.Position.BottomCenter" />
```

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

## Sortable + Swipeout

`MudSortableSwipeoutListRx<TItem>` and `MudSwipeoutRx<TItem>` add iOS-Mail-style swipe action panels and drag-to-reorder to a reactive list.

### Setup

The components ship with a stylesheet and a JS module. Reference the stylesheet from your `index.html`:

```html
<link href="_content/RxBlazorV2.MudBlazor/SwipeoutSortable.css" rel="stylesheet" />
```

The JS module is loaded automatically on first render — no extra `<script>` tag needed.

### Basic usage

```razor
<MudSortableSwipeoutListRx TItem="TaskItem"
                           Items="@Model.Tasks"
                           KeySelector="@(t => t.Id)"
                           Reorder="@(p => Model.ReorderCommand.ExecuteAsync(p))"
                           ActivationMode="SortActivation.DRAG_HANDLE">
    <ItemTemplate Context="task">
        <MudSwipeoutRx TItem="TaskItem" Item="task"
                       LeftActions="@BuildLeftActions(task)"
                       RightActions="@BuildRightActions(task)">
            <ChildContent Context="t">
                <MudPaper Class="pa-3 d-flex align-center" Elevation="0" Square="true">
                    <MudIcon Icon="@Icons.Material.Filled.DragIndicator" data-rxb-sort-handle Class="mr-3" />
                    <MudText>@t.Title</MudText>
                </MudPaper>
            </ChildContent>
        </MudSwipeoutRx>
    </ItemTemplate>
</MudSortableSwipeoutListRx>
```

```csharp
private IReadOnlyList<SwipeoutAction<TaskItem>> BuildRightActions(TaskItem task) => new[]
{
    new SwipeoutAction<TaskItem>
    {
        Icon = Icons.Material.Outlined.Archive,
        AriaLabel = "Archive",
        CommandAsyncOfItem = Model.ArchiveCommand
    },
    new SwipeoutAction<TaskItem>
    {
        Icon = Icons.Material.Filled.Delete,
        Color = Color.Error,
        AriaLabel = "Delete",
        IsDelete = true,                           // outermost action only
        CommandAsyncOfItem = Model.DeleteCommand
    }
};
```

### Action descriptor

`SwipeoutAction<TItem>` is a plain init-only record. Set **exactly one** command property:

| Property | Use for |
|---|---|
| `Command` | `IObservableCommand` (sync, no parameter) |
| `CommandOfItem` | `IObservableCommand<TItem>` (sync, item as parameter) |
| `CommandAsync` | `IObservableCommandAsync` (async, no parameter) |
| `CommandAsyncOfItem` | `IObservableCommandAsync<TItem>` (async, item as parameter) |

Plus visual properties: `Icon` (required), `Color`, `AriaLabel`, `ConfirmExecutionAsync`, `IsDelete`.

### Overswipe and swipe-to-delete

- Up to **3 actions per side**.
- The **outermost** action — `index 0` on the left, the last index on the right — is automatically the overswipe target. Drag past `actionsWidth + 60 px` and release to fire it.
- Set `IsDelete = true` on the outermost **right-side** action to enable swipe-to-delete: the row sweeps fully across before the click fires. Your command should remove the item from the model so Blazor re-renders without the row.

### Activation modes for sortable

| Mode | Use when |
|---|---|
| `DRAG_HANDLE` (default) | Element with `data-rxb-sort-handle` is the only drag start — best for desktop |
| `TAP_HOLD` | Long-press anywhere on a row starts a sort — best for touch |
| `ALWAYS` | Vertical movement on the row starts a sort — coexists with swipeout (which owns horizontal) |

### Reactivity

- `Items` is plain `IEnumerable<TItem>`. The component re-renders when its `ObservableComponent` parent does — typically after a property change in your `ObservableModel`.
- The `Reorder` callback fires with a `SortableMove` record (covers both intra-list and cross-list cases — see below). Your model owns the list mutation.
- Action commands run through the same `MudIconButton[Async]Rx` pipeline as everywhere else, including `ConfirmExecutionAsync`. Overswipe and delete just dispatch a synthetic click on the marked button.
- `KeySelector` is required for stable Blazor keys so swipeout JS instances stay attached to the correct DOM nodes after a reorder.

### Cross-list drag (groups)

Two lists with the same `SortableGroup.Name` can exchange items, subject to per-list pull/put rules. Useful for contact groups, tag baskets, kanban columns.

```razor
@code {
    // Source: items stay here when dragged out (clone semantics); doesn't accept incoming.
    private readonly SortableGroup _allGroup = new()
    {
        Name = "contacts",
        Pull = SortablePull.CLONE,
        Put = false
    };

    // Target: items dragged out are removed; accepts items from any list in the "contacts" group.
    private readonly SortableGroup _vipGroup = new()
    {
        Name = "contacts",
        Pull = SortablePull.MOVE,
        Put = true
    };
}

<MudSortableSwipeoutListRx TItem="Contact"
                           ListId="all-contacts"
                           Items="@Model.AllContacts"
                           KeySelector="@(c => c.Id)"
                           Reorder="@(p => Model.ReorderCommand.ExecuteAsync(p))"
                           Group="@_allGroup">
    <ItemTemplate Context="c">@ContactRow(c)</ItemTemplate>
</MudSortableSwipeoutListRx>

<MudSortableSwipeoutListRx TItem="Contact"
                           ListId="vip-group"
                           Items="@Model.VipContacts"
                           KeySelector="@(c => c.Id)"
                           Reorder="@(p => Model.ReorderCommand.ExecuteAsync(p))"
                           Group="@_vipGroup">
    <ItemTemplate Context="c">@ContactRow(c)</ItemTemplate>
</MudSortableSwipeoutListRx>
```

**Pull modes** (`SortablePull`):

| Mode | Effect |
|---|---|
| `MOVE` (default) | Items dragged out are removed from this list (handler should `RemoveAt` on the source) |
| `CLONE` | Items dragged out stay in this list; handler inserts a copy in the target. The `SortableMove.IsClone` flag is true on cross-list drops |
| `NONE` | Items cannot be dragged out at all |

**Single handler**, two semantics — the `SortableMove` record covers both cases:

```csharp
private async Task ReorderAsync(SortableMove move)
{
    var src = ListById(move.SourceListId);
    var tgt = ListById(move.TargetListId);

    if (move.SourceListId == move.TargetListId)
    {
        // Intra-list reorder.
        var item = src[move.FromIndex];
        src.RemoveAt(move.FromIndex);
        src.Insert(move.ToIndex, item);
    }
    else
    {
        // Cross-list — fires on the source list's component.
        var item = src[move.FromIndex];
        if (!move.IsClone) src.RemoveAt(move.FromIndex);
        tgt.Insert(move.ToIndex, item);
    }
}
```

`Reorder` fires once per drop, on the **source** list's component. Wire the same handler on every list — `move.SourceListId` / `move.TargetListId` tell you which lists are involved.

### Input handling

The gesture engine listens to two parallel input paths:

- **Pointer Events** for mouse and pen — uses `setPointerCapture` so a pointer that leaves the row mid-drag still reports back to the row. Pointer events with `pointerType === "touch"` are filtered out by both engines.
- **Touch Events** for finger input — `touchmove` is registered non-passive so `preventDefault()` on the first move wins the gesture from the browser's scroll heuristic. Pointer events on touch devices are unreliable for early-threshold disambiguation on iOS Safari, so finger gestures take this path instead.

Drag state lives entirely in JS; only `OpenedSide` and the `SortableMove` cross to .NET. Honours `prefers-reduced-motion`.

### Touch and scrolling

`.rxb-swipeout` is `touch-action: none` by default — combined with `preventDefault` on every touchmove, JS owns every gesture that starts on a row. The trade-off is:

- **Page scrolling comes from outside the rows** — the surrounding scroll container (`MudPaper`, `MudContainer`, the page body, etc.). Touching gutters / headers / empty space scrolls normally.
- **Inside a row, touch-scrolling is not available.** Vertical drag goes to the sortable (in `ALWAYS` mode), horizontal drag goes to the swipeout.

If a specific list really needs row-internal touch scroll (typically `DRAG_HANDLE` mode with no swipeout), opt back in via the CSS variable:

```css
.my-list .rxb-swipeout {
    --rxb-touch-action: pan-y;   /* let the browser scroll the row; swipe will be unreliable on touch */
}
```

The drag handle (`[data-rxb-sort-handle]`) carries `touch-action: none` regardless — handle-grabs always work on touch.

## Requirements

- .NET 10.0+
- [RxBlazorV2](https://www.nuget.org/packages/RxBlazorV2) 1.0.0+
- [MudBlazor](https://www.nuget.org/packages/MudBlazor) 8.0.0+

## License & Acknowledgements

MIT License — see [LICENSE](https://github.com/b-straub/RxBlazorV2/blob/master/LICENSE).

The swipeout + sortable gesture algorithms in `MudSwipeoutRx` / `MudSortableSwipeoutListRx`
(elasticity, velocity-snap, overswipe, swipe-to-delete sweep, midpoint-cross sortable,
edge auto-scroll, tap-hold activation) are derived from
[Framework7](https://framework7.io) © 2014 Vladimir Kharlampidi (MIT).
The cross-list group API (pull / put / clone semantics) follows the
[SortableJS](https://github.com/SortableJS/Sortable) model, with
[BlazorSortable](https://github.com/the-urlist/BlazorSortable) © 2023 The Urlist (MIT) as a
reference for Blazor interop conventions. The implementation here is a clean re-port in plain
DOM with Pointer Events + Touch Events. See [NOTICE](https://github.com/b-straub/RxBlazorV2/blob/master/NOTICE)
for full attribution.

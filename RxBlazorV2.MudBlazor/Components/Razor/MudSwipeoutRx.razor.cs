using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace RxBlazorV2.MudBlazor.Components.Razor;

/// <summary>
/// Side of a swipeout row that is currently revealed.
/// </summary>
public enum SwipeoutSide
{
    /// <summary>The row is closed.</summary>
    NONE,
    /// <summary>The left action panel is revealed.</summary>
    LEFT,
    /// <summary>The right action panel is revealed.</summary>
    RIGHT
}

/// <summary>
/// A single sortable / swipeable row.
///
/// <para>
/// Gesture is handled in JS via Pointer Events with <c>setPointerCapture</c>. Only the open/close
/// state crosses the JS↔.NET boundary. Each action's command runs through the standard
/// <c>MudIconButton[Async]Rx</c> click pipeline (with optional <c>ConfirmExecutionAsync</c>) — overswipe
/// and swipe-to-delete just dispatch a synthetic click on the outermost action.
/// </para>
///
/// <para>
/// Place inside a <see cref="MudSortableSwipeoutListRx{TItem}"/> for coordinated sortable + swipeout
/// behaviour, or use stand-alone for a swipe-only row.
/// </para>
/// </summary>
/// <typeparam name="TItem">The item this row represents.</typeparam>
public partial class MudSwipeoutRx<TItem> : ComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./_content/RxBlazorV2.MudBlazor/SwipeoutSortable.js";

    private ElementReference _rowRef;
    private DotNetObjectReference<MudSwipeoutRx<TItem>>? _selfRef;
    private IJSObjectReference? _module;
    private IJSObjectReference? _instance;
    private bool _initialized;
    private SwipeoutSide _internalState = SwipeoutSide.NONE;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// The item rendered in this row. Passed to <see cref="ChildContent"/>, <see cref="LeftActions"/>,
    /// and <see cref="RightActions"/> render fragments so action commands can be parameterized over it.
    /// </summary>
    [Parameter, EditorRequired]
    public required TItem Item { get; set; }

    /// <summary>
    /// The row's main content. Required.
    /// </summary>
    [Parameter, EditorRequired]
    public required RenderFragment<TItem> ChildContent { get; set; }

    /// <summary>
    /// Actions revealed when swiping left-to-right. Up to three. The action at index 0 (leftmost,
    /// closest to the row's left edge) is automatically the overswipe target.
    /// </summary>
    [Parameter]
    public IReadOnlyList<SwipeoutAction<TItem>>? LeftActions { get; set; }

    /// <summary>
    /// Actions revealed when swiping right-to-left. Up to three. The last action (rightmost, closest
    /// to the row's right edge) is automatically the overswipe target. To enable swipe-to-delete,
    /// set <see cref="SwipeoutAction{TItem}.IsDelete"/> on that last action.
    /// </summary>
    [Parameter]
    public IReadOnlyList<SwipeoutAction<TItem>>? RightActions { get; set; }

    private const int MaxActionsPerSide = 3;

    /// <summary>
    /// Two-way bindable. The currently revealed side. Setting this from .NET drives the row to that
    /// state via JS animation. Use <c>@bind-OpenedSide</c> to mirror the gesture's settled state in your model.
    /// </summary>
    [Parameter]
    public SwipeoutSide OpenedSide { get; set; } = SwipeoutSide.NONE;

    /// <summary>
    /// Fired when the user-driven gesture (or external <see cref="OpenedSide"/> change) settles into a new state.
    /// </summary>
    [Parameter]
    public EventCallback<SwipeoutSide> OpenedSideChanged { get; set; }

    /// <summary>
    /// Optional CSS classes appended to the row root.
    /// </summary>
    [Parameter]
    public string? AdditionalClasses { get; set; }

    /// <summary>
    /// Optional inline style on the row root.
    /// </summary>
    [Parameter]
    public string? Style { get; set; }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _selfRef = DotNetObjectReference.Create(this);
            _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
            _instance = await _module.InvokeAsync<IJSObjectReference>("createSwipeout", _rowRef, _selfRef, new { });
            _initialized = true;

            if (OpenedSide != SwipeoutSide.NONE)
            {
                await DriveStateAsync(OpenedSide);
            }
            return;
        }

        if (_initialized && OpenedSide != _internalState)
        {
            await DriveStateAsync(OpenedSide);
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        ValidateActions(LeftActions, nameof(LeftActions));
        ValidateActions(RightActions, nameof(RightActions));

        if (_initialized && _instance is not null)
        {
            // Action contents may have changed (e.g., reactive Visibility) — re-measure widths.
            await _instance.InvokeVoidAsync("refresh");
        }
    }

    private static void ValidateActions(IReadOnlyList<SwipeoutAction<TItem>>? actions, string side)
    {
        if (actions is null)
        {
            return;
        }
        if (actions.Count > MaxActionsPerSide)
        {
            throw new InvalidOperationException(
                $"MudSwipeoutRx.{side}: at most {MaxActionsPerSide} actions are supported (got {actions.Count}).");
        }
        for (var i = 0; i < actions.Count; i++)
        {
            var slots = actions[i].CommandSlots;
            if (slots != 1)
            {
                throw new InvalidOperationException(
                    $"MudSwipeoutRx.{side}[{i}]: exactly one of Command, CommandOfItem, CommandAsync, CommandAsyncOfItem must be set (got {slots}).");
            }
        }
    }

    private async Task DriveStateAsync(SwipeoutSide target)
    {
        if (_instance is null)
        {
            return;
        }
        switch (target)
        {
            case SwipeoutSide.LEFT:
            {
                await _instance.InvokeVoidAsync("open", "left");
                break;
            }
            case SwipeoutSide.RIGHT:
            {
                await _instance.InvokeVoidAsync("open", "right");
                break;
            }
            case SwipeoutSide.NONE:
            {
                await _instance.InvokeVoidAsync("close");
                break;
            }
        }
    }

    /// <summary>
    /// Invoked from JS when the gesture settles into a new state.
    /// </summary>
    [JSInvokable]
    public async Task OnStateChangedAsync(string state)
    {
        var newState = state switch
        {
            "left-open" => SwipeoutSide.LEFT,
            "right-open" => SwipeoutSide.RIGHT,
            _ => SwipeoutSide.NONE
        };
        if (newState == _internalState)
        {
            return;
        }
        _internalState = newState;
        OpenedSide = newState;
        await OpenedSideChanged.InvokeAsync(newState);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_instance is not null)
        {
            try
            {
                await _instance.InvokeVoidAsync("dispose");
                await _instance.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Browser navigated away — nothing to clean up.
            }
        }
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // ignore
            }
        }
        _selfRef?.Dispose();
    }
}

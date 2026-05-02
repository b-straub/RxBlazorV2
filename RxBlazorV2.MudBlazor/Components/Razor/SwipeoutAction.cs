using MudBlazor;
using RxBlazorV2.Interface;

namespace RxBlazorV2.MudBlazor.Components.Razor;

/// <summary>
/// Descriptor for a single swipeout action.
///
/// <para>
/// Set <b>exactly one</b> of <see cref="Command"/>, <see cref="CommandOfItem"/>, <see cref="CommandAsync"/>,
/// or <see cref="CommandAsyncOfItem"/> — the <c>...OfItem</c> variants receive the row's item as their parameter.
/// The component validates this at render time.
/// </para>
///
/// <para>
/// Up to three actions can be supplied per side via <see cref="MudSwipeoutRx{TItem}.LeftActions"/> /
/// <see cref="MudSwipeoutRx{TItem}.RightActions"/>. The <b>outermost</b> action — index 0 on the left,
/// the last index on the right — is automatically the overswipe target. Mark that outermost right-side
/// action with <see cref="IsDelete"/> to enable swipe-to-delete (full-row sweep before the click fires).
/// </para>
/// </summary>
/// <typeparam name="TItem">The row's item type — used by <c>...OfItem</c> command variants.</typeparam>
public sealed class SwipeoutAction<TItem>
{
    /// <summary>
    /// Material icon (e.g., <c>Icons.Material.Filled.Delete</c>).
    /// </summary>
    public required string Icon { get; init; }

    /// <summary>
    /// Background color of the action cell, applied via MudBlazor's theme palette so the icon picks
    /// up the contrast text color automatically (iOS-Mail style filled frame). Default:
    /// <see cref="Color.Default"/> — transparent cell, default icon color.
    /// </summary>
    public Color Color { get; init; } = Color.Default;

    /// <summary>
    /// Optional aria-label / tooltip text.
    /// </summary>
    public string? AriaLabel { get; init; }

    /// <summary>
    /// When true on the outermost right-side action, the swipe-to-delete gesture is enabled:
    /// the row sweeps fully across before the action's command fires. Has no effect on non-outermost
    /// actions or on left-side actions.
    /// </summary>
    public bool IsDelete { get; init; }

    /// <summary>
    /// Optional confirmation function called before the command executes.
    /// </summary>
    public Func<Task<bool>>? ConfirmExecutionAsync { get; init; }

    /// <summary>Sync command without parameter.</summary>
    public IObservableCommand? Command { get; init; }

    /// <summary>Sync command receiving the row item as parameter.</summary>
    public IObservableCommand<TItem>? CommandOfItem { get; init; }

    /// <summary>Async command without parameter.</summary>
    public IObservableCommandAsync? CommandAsync { get; init; }

    /// <summary>Async command receiving the row item as parameter.</summary>
    public IObservableCommandAsync<TItem>? CommandAsyncOfItem { get; init; }

    internal int CommandSlots => (Command is not null ? 1 : 0)
                                 + (CommandOfItem is not null ? 1 : 0)
                                 + (CommandAsync is not null ? 1 : 0)
                                 + (CommandAsyncOfItem is not null ? 1 : 0);
}

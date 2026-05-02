using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace RxBlazorV2.MudBlazor.Components.Razor;

/// <summary>
/// Activation gesture for sortable drag.
/// </summary>
public enum SortActivation
{
    /// <summary>
    /// Pointer-down must hit an element marked with <c>data-rxb-sort-handle</c>. Recommended for
    /// pointer/desktop UIs where the row content also needs to be tappable.
    /// </summary>
    DRAG_HANDLE,

    /// <summary>
    /// Long-press anywhere on a row begins a sort. Recommended for touch UIs.
    /// </summary>
    TAP_HOLD,

    /// <summary>
    /// Vertical movement anywhere on a row begins a sort. Coexists with swipeout (which owns
    /// the horizontal axis) but can interfere with rich content like text selection.
    /// </summary>
    ALWAYS
}

/// <summary>
/// Reactive sortable list with first-class swipeout coordination.
///
/// <para>
/// Renders <see cref="Items"/> via <see cref="ItemTemplate"/> and exposes a sortable gesture
/// (Pointer Events, with edge auto-scroll) that emits <see cref="Reorder"/> with the resolved
/// (from, to) indices when the user drops a row in a new position. The component itself does NOT
/// mutate <see cref="Items"/> — the user's reactive model owns the list and re-renders the component
/// after applying the move (matches the project's "commands must be atomic" rule).
/// </para>
///
/// <para>
/// Pair the list's <see cref="ItemTemplate"/> with <see cref="MudSwipeoutRx{TItem}"/> for combined
/// sort + swipeout behaviour. Open swipeouts close automatically when a sort begins; sort is
/// blocked while any swipeout is mid-drag.
/// </para>
/// </summary>
/// <typeparam name="TItem">Item type. Must have a stable identity exposed via <see cref="KeySelector"/>.</typeparam>
public partial class MudSortableSwipeoutListRx<TItem> : ComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./_content/RxBlazorV2.MudBlazor/SwipeoutSortable.js";

    private ElementReference _listRef;
    private DotNetObjectReference<MudSortableSwipeoutListRx<TItem>>? _selfRef;
    private IJSObjectReference? _module;
    private IJSObjectReference? _instance;
    private bool _initialized;
    private bool _previousEnabled;
    private SortActivation _previousActivation;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// The items to render. Reactivity is the caller's responsibility — when the underlying model
    /// list changes, the surrounding ObservableComponent re-renders this component with a new <see cref="Items"/>.
    /// </summary>
    [Parameter, EditorRequired]
    public required IEnumerable<TItem> Items { get; set; }

    /// <summary>
    /// Renders one item. Typically a <see cref="MudSwipeoutRx{TItem}"/>.
    /// </summary>
    [Parameter, EditorRequired]
    public required RenderFragment<TItem> ItemTemplate { get; set; }

    /// <summary>
    /// Selects a stable key for a given item. Required for correct DOM diff/reuse across reorders
    /// — Blazor uses the key to identify which DOM nodes belong to which items, so swipeout JS
    /// instances stay attached to the correct row after a reorder.
    /// </summary>
    [Parameter, EditorRequired]
    public required Func<TItem, object> KeySelector { get; set; }

    /// <summary>
    /// Fired with a <see cref="SortableMove"/> describing the drop. Covers both intra-list reorders
    /// (source equals target) and cross-list moves/clones when <see cref="Group"/> is configured.
    /// Fires once per drop, on the source list's component.
    /// </summary>
    [Parameter]
    public EventCallback<SortableMove> Reorder { get; set; }

    /// <summary>
    /// Stable identifier used to disambiguate this list in <see cref="SortableMove"/> events. Defaults
    /// to a fresh GUID — set explicitly (e.g. <c>"all-contacts"</c>) when you have multiple lists in
    /// the same group so the handler can dispatch by id.
    /// </summary>
    [Parameter]
    public string ListId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Cross-list group configuration. When set, this list participates in drag-exchange with other
    /// lists declaring the same <see cref="SortableGroup.Name"/>, subject to each list's pull/put rules.
    /// Default: <c>null</c> — list is isolated (intra-list reorder only).
    /// </summary>
    [Parameter]
    public SortableGroup? Group { get; set; }

    /// <summary>
    /// Activation gesture. Default: <see cref="SortActivation.DRAG_HANDLE"/>.
    /// </summary>
    [Parameter]
    public SortActivation ActivationMode { get; set; } = SortActivation.DRAG_HANDLE;

    /// <summary>
    /// When false, sortable gesture is disabled (rows are still rendered and any inner swipeouts still work).
    /// </summary>
    [Parameter]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Optional CSS classes appended to the list root.
    /// </summary>
    [Parameter]
    public string? AdditionalClasses { get; set; }

    /// <summary>
    /// Optional inline style on the list root.
    /// </summary>
    [Parameter]
    public string? Style { get; set; }

    private object GetKey(TItem item)
    {
        return KeySelector(item);
    }

    private string ActivationAttr => ActivationMode switch
    {
        SortActivation.TAP_HOLD => "tap-hold",
        SortActivation.ALWAYS => "always",
        _ => "drag-handle"
    };

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _selfRef = DotNetObjectReference.Create(this);
            _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
            var opts = new
            {
                activation = ActivationMode switch
                {
                    SortActivation.TAP_HOLD => "tap-hold",
                    SortActivation.ALWAYS => "always",
                    _ => "drag-handle"
                },
                enabled = Enabled,
                listId = ListId,
                groupName = Group?.Name,
                pull = Group?.Pull switch
                {
                    SortablePull.NONE => "none",
                    SortablePull.CLONE => "clone",
                    _ => "move"
                },
                put = Group?.Put ?? false
            };
            _instance = await _module.InvokeAsync<IJSObjectReference>("createSortable", _listRef, _selfRef, opts);
            _previousEnabled = Enabled;
            _previousActivation = ActivationMode;
            _initialized = true;
            return;
        }

        if (_initialized && _instance is not null && Enabled != _previousEnabled)
        {
            await _instance.InvokeVoidAsync(Enabled ? "enable" : "disable");
            _previousEnabled = Enabled;
        }

        if (_initialized && _instance is not null && ActivationMode != _previousActivation)
        {
            var mode = ActivationMode switch
            {
                SortActivation.TAP_HOLD => "tap-hold",
                SortActivation.ALWAYS => "always",
                _ => "drag-handle"
            };
            await _instance.InvokeVoidAsync("setActivation", mode);
            _previousActivation = ActivationMode;
        }
    }

    /// <summary>
    /// Invoked from JS when a drop produces a reordering. Always fires on the source list's component.
    /// </summary>
    [JSInvokable]
    public Task OnReorderAsync(string sourceListId, int fromIndex, string targetListId, int toIndex, bool isClone, bool isRemove)
    {
        return Reorder.InvokeAsync(new SortableMove(sourceListId, fromIndex, targetListId, toIndex, isClone, isRemove));
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
                // ignore
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

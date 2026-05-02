namespace RxBlazorV2.MudBlazor.Components.Razor;

/// <summary>
/// Pull mode for a sortable list — controls whether items can be dragged out and how.
/// </summary>
public enum SortablePull
{
    /// <summary>Items cannot be dragged out of this list.</summary>
    NONE,
    /// <summary>Items dragged out are moved (removed from this list, inserted into the target).</summary>
    MOVE,
    /// <summary>Items dragged out are cloned (kept in this list, inserted into the target).</summary>
    CLONE
}

/// <summary>
/// Cross-list group configuration. Two <see cref="MudSortableSwipeoutListRx{TItem}"/> instances with
/// the same <see cref="Name"/> can exchange items by drag-and-drop, subject to each list's
/// <see cref="Pull"/> and <see cref="Put"/> rules.
/// </summary>
public sealed class SortableGroup
{
    /// <summary>
    /// Group identifier — lists with the same name belong to the same exchange group.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// How items leave this list when dragged into another list of the same group. Default:
    /// <see cref="SortablePull.MOVE"/>.
    /// </summary>
    public SortablePull Pull { get; init; } = SortablePull.MOVE;

    /// <summary>
    /// Whether this list accepts items dragged from other lists in the same group. Default: <c>true</c>.
    /// </summary>
    public bool Put { get; init; } = true;
}

/// <summary>
/// Result of a drag-drop. Covers intra-list reorders (<see cref="SourceListId"/> equals
/// <see cref="TargetListId"/>), cross-list moves/clones, and drag-out-to-remove (when
/// <see cref="IsRemove"/> is true and the source list's pull mode is <see cref="SortablePull.MOVE"/>).
/// </summary>
/// <param name="SourceListId">Identifier of the list the item was dragged from.</param>
/// <param name="FromIndex">0-based index of the item in the source list at drag start.</param>
/// <param name="TargetListId">Identifier of the list the item was dropped into. Empty string when <see cref="IsRemove"/> is true.</param>
/// <param name="ToIndex">0-based index in the target list where the item should land. <c>-1</c> when <see cref="IsRemove"/> is true.</param>
/// <param name="IsClone">
/// True when the source list's <see cref="SortableGroup.Pull"/> is <see cref="SortablePull.CLONE"/>
/// and the move went cross-list — handler should leave the source list untouched and insert a copy
/// into the target. False otherwise.
/// </param>
/// <param name="IsRemove">
/// True when the item was dropped outside any valid target list and the source list has
/// <see cref="SortablePull.MOVE"/>. Handler should remove the item from the source list and ignore
/// <paramref name="TargetListId"/> / <paramref name="ToIndex"/>.
/// </param>
public sealed record SortableMove(
    string SourceListId,
    int FromIndex,
    string TargetListId,
    int ToIndex,
    bool IsClone,
    bool IsRemove);

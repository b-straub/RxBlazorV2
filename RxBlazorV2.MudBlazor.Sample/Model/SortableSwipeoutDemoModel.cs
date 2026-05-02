using ObservableCollections;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2.MudBlazor.Components;
using RxBlazorV2.MudBlazor.Components.Razor;

namespace RxBlazorV2.MudBlazor.Sample.Model;

public sealed record TaskItem(Guid Id, string Title, bool IsPinned, bool IsArchived);

[ObservableModelScope(ModelScope.Scoped)]
[ObservableComponent]
public partial class SortableSwipeoutDemoModel : ObservableModel
{
    public partial SortableSwipeoutDemoModel(StatusModel statusModel);

    public ObservableList<TaskItem> Tasks { get; } = new();

    [ObservableCommand(nameof(ReorderAsync))]
    public partial IObservableCommandAsync<SortableMove> ReorderCommand { get; }

    [ObservableCommand(nameof(PinAsync))]
    public partial IObservableCommandAsync<TaskItem> PinCommand { get; }

    [ObservableCommand(nameof(ArchiveAsync))]
    public partial IObservableCommandAsync<TaskItem> ArchiveCommand { get; }

    [ObservableCommand(nameof(DeleteAsync))]
    public partial IObservableCommandAsync<TaskItem> DeleteCommand { get; }

    [ObservableCommand(nameof(ResetAsync))]
    public partial IObservableCommandAsync ResetCommand { get; }

    protected override Task OnContextReadyAsync()
    {
        Seed();
        return Task.CompletedTask;
    }

    private void Seed()
    {
        Tasks.Clear();
        Tasks.Add(new TaskItem(Guid.NewGuid(), "Review pull requests", false, false));
        Tasks.Add(new TaskItem(Guid.NewGuid(), "Write release notes", false, false));
        Tasks.Add(new TaskItem(Guid.NewGuid(), "Investigate flaky test", false, false));
        Tasks.Add(new TaskItem(Guid.NewGuid(), "Update dependencies", false, false));
        Tasks.Add(new TaskItem(Guid.NewGuid(), "Plan Q3 OKRs", false, false));
        Tasks.Add(new TaskItem(Guid.NewGuid(), "Sync with design team", false, false));
    }

    private async Task ReorderAsync(SortableMove move)
    {
        // Single-list demo — source equals target; intra-list reorder.
        var item = Tasks[move.FromIndex];
        Tasks.RemoveAt(move.FromIndex);
        Tasks.Insert(move.ToIndex, item);
        StatusModel.AddInfo($"Moved \"{item.Title}\" {move.FromIndex} → {move.ToIndex}");
        await Task.CompletedTask;
    }

    private async Task PinAsync(TaskItem item)
    {
        var index = IndexOf(item);
        if (index < 0)
        {
            return;
        }
        var updated = item with { IsPinned = !item.IsPinned };
        Tasks[index] = updated;
        StatusModel.AddSuccess(updated.IsPinned ? $"Pinned \"{item.Title}\"" : $"Unpinned \"{item.Title}\"");
        await Task.CompletedTask;
    }

    private async Task ArchiveAsync(TaskItem item)
    {
        var index = IndexOf(item);
        if (index < 0)
        {
            return;
        }
        var updated = item with { IsArchived = !item.IsArchived };
        Tasks[index] = updated;
        StatusModel.AddInfo(updated.IsArchived ? $"Archived \"{item.Title}\"" : $"Unarchived \"{item.Title}\"");
        await Task.CompletedTask;
    }

    private async Task DeleteAsync(TaskItem item)
    {
        var index = IndexOf(item);
        if (index < 0)
        {
            return;
        }
        Tasks.RemoveAt(index);
        StatusModel.AddWarning($"Deleted \"{item.Title}\"");
        await Task.CompletedTask;
    }

    private async Task ResetAsync()
    {
        Seed();
        StatusModel.AddInfo("Reset task list");
        await Task.CompletedTask;
    }

    private int IndexOf(TaskItem item)
    {
        for (var i = 0; i < Tasks.Count; i++)
        {
            if (Tasks[i].Id == item.Id)
            {
                return i;
            }
        }
        return -1;
    }
}

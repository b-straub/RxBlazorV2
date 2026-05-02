using ObservableCollections;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2.MudBlazor.Components;
using RxBlazorV2.MudBlazor.Components.Razor;

namespace RxBlazorV2.MudBlazor.Sample.Model;

public sealed record Notification(
    Guid Id,
    string Sender,
    string Subject,
    string Preview,
    DateTimeOffset Timestamp,
    bool IsRead,
    bool IsArchived = false);

[ObservableModelScope(ModelScope.Scoped)]
[ObservableComponent]
public partial class NotificationsDemoModel : ObservableModel
{
    public partial NotificationsDemoModel(StatusModel statusModel);

    /// <summary>
    /// Items are kept in the order the database returned them — newest first by timestamp.
    /// The user cannot reorder, only swipe to act on individual rows.
    /// </summary>
    public ObservableList<Notification> Notifications { get; } = new();

    [ObservableCommand(nameof(ToggleReadAsync))]
    public partial IObservableCommandAsync<Notification> ToggleReadCommand { get; }

    [ObservableCommand(nameof(ArchiveAsync))]
    public partial IObservableCommandAsync<Notification> ArchiveCommand { get; }

    [ObservableCommand(nameof(DeleteAsync))]
    public partial IObservableCommandAsync<Notification> DeleteCommand { get; }

    [ObservableCommand(nameof(ResetAsync))]
    public partial IObservableCommandAsync ResetCommand { get; }

    public int UnreadCount => Notifications.Count(n => n.IsRead == false);

    protected override async Task OnContextReadyAsync(CancellationToken cancellationToken)
    {
        await LoadFromDatabaseAsync(cancellationToken);
    }

    private async Task LoadFromDatabaseAsync(CancellationToken cancellationToken)
    {
        // Simulate a DB roundtrip — the real app would call a repository / EF Core / Dapper.
        await Task.Delay(150, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var seed = new[]
        {
            new Notification(Guid.NewGuid(), "Ada Lovelace",     "Re: Analytical engine notes",       "Couldn't agree more — the punch-card layout you proposed handles the recursive case much…", now.AddMinutes(-3),   false),
            new Notification(Guid.NewGuid(), "GitHub",            "PR #482 needs review",              "Anders Hejlsberg requested your review on \"Add cancellation token to OnContextReadyAsync\".",  now.AddMinutes(-12),  false),
            new Notification(Guid.NewGuid(), "CI / build-bot",    "Build green on master",             "All 334 tests passed in 15.3s. Coverage 94.2%.",                                                  now.AddMinutes(-37),  true),
            new Notification(Guid.NewGuid(), "Grace Hopper",      "Lunch tomorrow?",                   "Found a great spot near the lab. Wednesday at 12:30?",                                            now.AddHours(-2),     false),
            new Notification(Guid.NewGuid(), "NuGet",             "MudBlazor 9.4.1 published",         "A new version of MudBlazor is available. See changelog for details.",                            now.AddHours(-6),     true),
            new Notification(Guid.NewGuid(), "Linus Torvalds",    "Re: signed-off-by",                 "Looks fine to me. Just rebase against master and resend.",                                       now.AddHours(-9),     true),
            new Notification(Guid.NewGuid(), "Calendar",          "Standup in 15 minutes",             "Daily engineering standup, room \"Pipelines\".",                                                  now.AddHours(-23),    true),
            new Notification(Guid.NewGuid(), "Margaret Hamilton", "Apollo guidance code review",       "I had a few comments on your priority-aware scheduler — see inline.",                            now.AddDays(-1),      true)
        };
        // Newest first — typical "ORDER BY Timestamp DESC".
        Notifications.Clear();
        foreach (var n in seed.OrderByDescending(n => n.Timestamp))
        {
            Notifications.Add(n);
        }
    }

    private async Task ToggleReadAsync(Notification item)
    {
        var index = IndexOf(item);
        if (index < 0)
        {
            return;
        }
        Notifications[index] = item with { IsRead = !item.IsRead };
        StatusModel.AddInfo(item.IsRead ? $"Marked unread: {item.Subject}" : $"Marked read: {item.Subject}");
        await Task.CompletedTask;
    }

    private async Task ArchiveAsync(Notification item)
    {
        var index = IndexOf(item);
        if (index < 0)
        {
            return;
        }
        var updated = item with { IsArchived = !item.IsArchived };
        Notifications[index] = updated;
        StatusModel.AddSuccess(updated.IsArchived ? $"Archived: {item.Subject}" : $"Unarchived: {item.Subject}");
        await Task.CompletedTask;
    }

    private async Task DeleteAsync(Notification item)
    {
        var index = IndexOf(item);
        if (index < 0)
        {
            return;
        }
        Notifications.RemoveAt(index);
        StatusModel.AddWarning($"Deleted: {item.Subject}");
        await Task.CompletedTask;
    }

    private async Task ResetAsync(CancellationToken cancellationToken)
    {
        await LoadFromDatabaseAsync(cancellationToken);
        StatusModel.AddInfo("Reloaded notifications");
    }

    private int IndexOf(Notification item)
    {
        for (var i = 0; i < Notifications.Count; i++)
        {
            if (Notifications[i].Id == item.Id)
            {
                return i;
            }
        }
        return -1;
    }
}

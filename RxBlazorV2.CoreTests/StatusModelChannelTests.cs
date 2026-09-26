using Microsoft.Extensions.Time.Testing;
using R3;
using RxBlazorV2.CoreTests.TestFixtures;
using RxBlazorV2.Model;

namespace RxBlazorV2.CoreTests;

/// <summary>
/// Runtime tests for status channels and dismissal of <see cref="StatusBaseModel"/>. Channels are
/// independent - modes, queue and Single-mode replacement never reach across them - and only user
/// dismissals, never programmatic clears, are reported through <see cref="StatusBaseModel.Dismissed"/>.
/// </summary>
public class StatusModelChannelTests
{
    private const string DbChannel = "db";
    private static readonly TimeSpan Window = TimeSpan.FromMilliseconds(500);

    private static (TestStatusModel Model, FakeTimeProvider Time) CreateModel()
    {
        var time = new FakeTimeProvider();
        var model = new TestStatusModel
        {
            QueueTimeProvider = time,
            QueueWindow = Window
        };

        return (model, time);
    }

    [Fact]
    public void Messages_WithoutChannel_UseDefaultChannel()
    {
        var (model, _) = CreateModel();

        model.AddInfo("System ready");
        model.HandleError(new InvalidOperationException("boom"), "LoadCommand", "LoadAsync");

        Assert.All(model.Messages, m => Assert.Equal(StatusMessage.DefaultChannel, m.Channel));
    }

    [Fact]
    public void SingleMode_ReplacesOnlyWithinItsChannel()
    {
        var (model, _) = CreateModel();
        model.GetChannelSettings(DbChannel).MessageMessageMode = StatusMessageMode.Single;

        model.AddInfo("System ready");
        model.AddInfo("Row 1 changed", channel: DbChannel);
        model.AddInfo("Row 2 changed", channel: DbChannel);

        Assert.Equal(2, model.Messages.Count);
        Assert.Contains(model.Messages, m => m.Message == "System ready");
        Assert.Contains(model.Messages, m => m.Message == "Row 2 changed");
    }

    [Fact]
    public void ChannelSettings_AreIndependent()
    {
        var (model, _) = CreateModel();

        model.GetChannelSettings(DbChannel).ErrorMessageMode = StatusMessageMode.Single;

        Assert.Equal(StatusMessageMode.Aggregate, model.ErrorMessageMode);
        Assert.Equal(StatusMessageMode.Single, model.GetChannelSettings(DbChannel).ErrorMessageMode);
    }

    [Fact]
    public void MessageOfOtherChannel_KeepsQueuedMessage()
    {
        var (model, time) = CreateModel();

        model.QueueInfo("Loading...");
        model.AddInfo("Row changed", channel: DbChannel);

        Assert.True(model.HasQueuedMessage);

        time.Advance(Window);

        Assert.Contains(model.Messages, m => m.Message == "Loading...");
    }

    [Fact]
    public void QueuedMessage_UsesWindowOfItsChannel()
    {
        var (model, time) = CreateModel();
        model.GetChannelSettings(DbChannel).QueueWindow = Window * 2;

        model.QueueInfo("Syncing...", channel: DbChannel);
        time.Advance(Window);
        Assert.Empty(model.Messages);

        time.Advance(Window);
        var message = Assert.Single(model.Messages);
        Assert.Equal(DbChannel, message.Channel);
    }

    [Fact]
    public void ClearByChannel_LeavesOtherChannels()
    {
        var (model, _) = CreateModel();

        model.AddError("Load failed");
        model.AddError("Sync failed", channel: DbChannel);
        model.QueueInfo("Loading...");

        model.ClearMessages(DbChannel);

        var message = Assert.Single(model.Messages);
        Assert.Equal("Load failed", message.Message);
        Assert.True(model.HasQueuedMessage);
    }

    [Fact]
    public void AddMessage_KeepsCallerSuppliedId()
    {
        var (model, _) = CreateModel();
        var recordId = Guid.NewGuid();

        model.AddMessage(new StatusMessage("Row changed", StatusSeverity.Info, "Db", DbChannel) { Id = recordId });

        Assert.Equal(recordId, Assert.Single(model.Messages).Id);
    }

    [Fact]
    public void DismissNonErrorMessages_ReportsOnlyItsChannel()
    {
        var (model, _) = CreateModel();
        var dismissed = new List<IReadOnlyList<StatusMessage>>();
        using var subscription = model.Dismissed.Subscribe(dismissed.Add);

        model.AddInfo("System ready");
        model.AddInfo("Row 1 changed", channel: DbChannel);
        model.AddWarning("Row 2 conflict", channel: DbChannel);
        model.AddError("Sync failed", channel: DbChannel);

        model.DismissNonErrorMessages(DbChannel);

        var batch = Assert.Single(dismissed);
        Assert.Equal(["Row 1 changed", "Row 2 conflict"], batch.Select(m => m.Message));
        Assert.Equal(2, model.Messages.Count);
    }

    [Fact]
    public void DismissMessages_ReportsOnlyMessagesStillPresent()
    {
        var (model, _) = CreateModel();
        var dismissed = new List<IReadOnlyList<StatusMessage>>();
        using var subscription = model.Dismissed.Subscribe(dismissed.Add);

        model.AddInfo("Row changed", channel: DbChannel);
        var message = model.Messages[0];

        model.DismissMessages([message]);
        model.DismissMessages([message]);

        Assert.Same(message, Assert.Single(Assert.Single(dismissed)));
        Assert.Empty(model.Messages);
    }

    [Fact]
    public void Clear_DoesNotReportDismissal()
    {
        var (model, _) = CreateModel();
        var dismissed = new List<IReadOnlyList<StatusMessage>>();
        using var subscription = model.Dismissed.Subscribe(dismissed.Add);

        model.AddInfo("Row changed", channel: DbChannel);
        model.ClearMessages();

        Assert.Empty(dismissed);
    }

    [Fact]
    public void DismissNonErrorMessages_DropsQueuedMessageWithoutReportingIt()
    {
        var (model, time) = CreateModel();
        var dismissed = new List<IReadOnlyList<StatusMessage>>();
        using var subscription = model.Dismissed.Subscribe(dismissed.Add);

        model.QueueInfo("Syncing...", channel: DbChannel);
        model.DismissNonErrorMessages(DbChannel);
        time.Advance(Window * 2);

        Assert.Empty(model.Messages);
        Assert.Empty(dismissed);
    }
}

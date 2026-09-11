using Microsoft.Extensions.Time.Testing;
using RxBlazorV2.CoreTests.TestFixtures;
using RxBlazorV2.Model;

namespace RxBlazorV2.CoreTests;

/// <summary>
/// Runtime tests for the queued (cancellable) messages of <see cref="StatusBaseModel"/>. A queued message
/// stays out of <see cref="StatusBaseModel.Messages"/> until its window elapses, and is dropped entirely
/// when another message arrives, when its category is cleared, or when the model is disposed inside that
/// window - so a transient message never flashes up after the operation it announced has finished.
/// </summary>
public class StatusModelQueueTests
{
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
    public void QueuedMessage_StaysInvisibleUntilWindowElapses()
    {
        var (model, time) = CreateModel();

        model.QueueInfo("Loading...", "LoadCommand");

        Assert.Empty(model.Messages);
        Assert.True(model.HasQueuedMessage);

        time.Advance(TimeSpan.FromMilliseconds(499));
        Assert.Empty(model.Messages);

        time.Advance(TimeSpan.FromMilliseconds(1));

        var message = Assert.Single(model.Messages);
        Assert.Equal("Loading...", message.Message);
        Assert.Equal("LoadCommand", message.Source);
        Assert.Equal(StatusSeverity.Info, message.Severity);
        Assert.False(model.HasQueuedMessage);
    }

    [Fact]
    public void ImmediateMessage_WithinWindow_CancelsQueuedMessage()
    {
        var (model, time) = CreateModel();

        model.QueueInfo("Loading...");
        model.AddSuccess("Loaded 42 rows");

        Assert.False(model.HasQueuedMessage);

        time.Advance(Window * 2);

        var message = Assert.Single(model.Messages);
        Assert.Equal("Loaded 42 rows", message.Message);
    }

    [Fact]
    public void ErrorOfDifferentCategory_WithinWindow_CancelsQueuedMessage()
    {
        var (model, time) = CreateModel();

        model.QueueInfo("Loading...");
        model.AddError("Load failed");

        time.Advance(Window * 2);

        var message = Assert.Single(model.Messages);
        Assert.Equal("Load failed", message.Message);
        Assert.Equal(StatusSeverity.Error, message.Severity);
    }

    [Fact]
    public void SecondQueuedMessage_WithinWindow_ReplacesFirstAndRestartsWindow()
    {
        var (model, time) = CreateModel();

        model.QueueInfo("First");
        time.Advance(TimeSpan.FromMilliseconds(400));

        model.QueueInfo("Second");
        time.Advance(TimeSpan.FromMilliseconds(400));

        // The first window has elapsed by now, but its message was replaced.
        Assert.Empty(model.Messages);

        time.Advance(TimeSpan.FromMilliseconds(100));

        var message = Assert.Single(model.Messages);
        Assert.Equal("Second", message.Message);
    }

    [Fact]
    public void ClearMessages_WithinWindow_CancelsQueuedMessage()
    {
        var (model, time) = CreateModel();

        model.QueueInfo("Loading...");
        model.ClearMessages();

        Assert.False(model.HasQueuedMessage);

        time.Advance(Window * 2);

        Assert.Empty(model.Messages);
    }

    [Fact]
    public void ClearNonErrorMessages_CancelsQueuedMessage()
    {
        var (model, time) = CreateModel();

        model.QueueInfo("Loading...");
        model.ClearNonErrorMessages();

        time.Advance(Window * 2);

        Assert.Empty(model.Messages);
    }

    [Fact]
    public void ClearErrorMessages_LeavesQueuedMessageIntact()
    {
        var (model, time) = CreateModel();

        // Only non-error messages can be queued, so clearing errors never touches a pending one.
        model.QueueInfo("Loading...");
        model.ClearErrorMessages();

        time.Advance(Window * 2);

        var message = Assert.Single(model.Messages);
        Assert.Equal("Loading...", message.Message);
    }

    [Fact]
    public void ClearMessagesBySeverity_CancelsOnlyMatchingQueuedMessage()
    {
        var (model, time) = CreateModel();

        model.QueueSuccess("Saved");
        model.ClearMessages(StatusSeverity.Info);
        time.Advance(Window * 2);
        Assert.Single(model.Messages);

        model.ClearMessages();
        model.QueueSuccess("Saved");
        model.ClearMessages(StatusSeverity.Success);
        time.Advance(Window * 2);

        Assert.Empty(model.Messages);
    }

    [Fact]
    public void CancelQueuedMessage_ReportsWhetherSomethingWasDropped()
    {
        var (model, time) = CreateModel();

        Assert.False(model.CancelQueuedMessage());

        model.QueueInfo("Loading...");
        Assert.True(model.CancelQueuedMessage());
        Assert.False(model.CancelQueuedMessage());

        time.Advance(Window * 2);
        Assert.Empty(model.Messages);
    }

    [Fact]
    public void ZeroWindow_PublishesImmediately()
    {
        var (model, _) = CreateModel();

        model.QueueInfo("Loading...", window: TimeSpan.Zero);

        Assert.False(model.HasQueuedMessage);
        Assert.Single(model.Messages);
    }

    [Fact]
    public void DefaultWindow_IsOneSecond()
    {
        var time = new FakeTimeProvider();
        var model = new TestStatusModel { QueueTimeProvider = time };

        model.QueueInfo("Loading...");

        time.Advance(TimeSpan.FromMilliseconds(999));
        Assert.Empty(model.Messages);

        time.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Single(model.Messages);
    }

    [Fact]
    public void ExplicitWindow_OverridesModelDefault()
    {
        var (model, time) = CreateModel();

        model.QueueInfo("Loading...", window: TimeSpan.FromMilliseconds(100));

        time.Advance(TimeSpan.FromMilliseconds(100));

        Assert.Single(model.Messages);
    }

    [Fact]
    public void QueuedMessage_RespectsSingleModeOnPublish()
    {
        var (model, time) = CreateModel();
        model.MessageMessageMode = StatusMessageMode.Single;

        model.AddInfo("Old message");
        model.QueueSuccess("Saved");
        time.Advance(Window);

        var message = Assert.Single(model.Messages);
        Assert.Equal("Saved", message.Message);
    }

    [Fact]
    public void Dispose_WithinWindow_DropsQueuedMessage()
    {
        var (model, time) = CreateModel();

        model.QueueInfo("Loading...");
        model.Dispose();

        // Disposing Subscriptions unsubscribed the pending delay, so nothing reaches the torn-down model.
        time.Advance(Window * 2);

        Assert.Empty(model.Messages);
    }

    [Fact]
    public void Queue_AfterDispose_PublishesNothing()
    {
        var (model, time) = CreateModel();
        model.Dispose();

        model.QueueInfo("Loading...");
        time.Advance(Window * 2);

        Assert.Empty(model.Messages);
    }

    [Fact]
    public void QueuedMessage_NotifiesSubscribersOnlyWhenPublished()
    {
        var (model, time) = CreateModel();
        var notifications = 0;

        using var subscription = model.Observable.Subscribe(_ => notifications++);

        model.QueueInfo("Loading...");
        Assert.Equal(0, notifications);

        time.Advance(Window);
        Assert.Equal(1, notifications);
    }
}

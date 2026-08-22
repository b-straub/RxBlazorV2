using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RxBlazorV2.CoreTests.TestFixtures;

namespace RxBlazorV2.CoreTests;

/// <summary>
/// What a command publishes when its own state changes, and what a filtered
/// component makes of it.
///
/// <para>
/// Two things went unnoticed for a long time because no test looked here.
/// A command with no observed properties published the generator's empty-string
/// placeholder, which intersects no component filter — so a component bound to
/// nothing but that command never re-rendered while it ran, and every async
/// button's progress spinner and disabled state stayed as they were. And the
/// parameterized async factory published its completion *before* clearing
/// <c>Executing</c>, so the last thing a subscriber saw still said "running".
/// </para>
/// <para>
/// The existing execution-state tests missed both: they subscribe without a
/// filter (so the placeholder looked fine) and the parameterized one asserted
/// the command's field after the await rather than what subscribers were told.
/// </para>
/// </summary>
public class ObservableCommandNotificationTests : BunitContext
{
    private readonly ITestOutputHelper _output;

    public ObservableCommandNotificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static List<string[]> Record(TestCommandModel model, out IDisposable subscription)
    {
        var seen = new List<string[]>();
        subscription = model.Observable.Subscribe(props => seen.Add(props));
        return seen;
    }

    /// <summary>
    /// How many of the notifications carried <paramref name="commandName"/>.
    /// The others are the command body's own property changes, which travel
    /// under their own names and are none of this test's business.
    /// </summary>
    private static int Carrying(IEnumerable<string[]> seen, string commandName)
        => seen.Count(props => props.Contains($"Model.{commandName}"));

    [Fact]
    public void SyncCommand_PublishesItsOwnName()
    {
        var model = new TestCommandModel();
        var seen = Record(model, out var subscription);
        using (subscription)
        {
            model.SyncCommand.Execute();
        }

        Assert.True(
            Carrying(seen, "SyncCommand") >= 1,
            "Executing a command must publish the command's own name — a component filters on it.");
    }

    [Fact]
    public void SyncCommandWithParam_PublishesItsOwnName()
    {
        var model = new TestCommandModel();
        var seen = Record(model, out var subscription);
        using (subscription)
        {
            model.SyncCommandWithParam.Execute(3);
        }

        Assert.True(
            Carrying(seen, "SyncCommandWithParam") >= 1,
            "Executing a command must publish the command's own name — a component filters on it.");
    }

    [Fact]
    public async Task AsyncCommand_PublishesItsOwnName()
    {
        var model = new TestCommandModel();
        var seen = Record(model, out var subscription);
        using (subscription)
        {
            await model.AsyncCommand.ExecuteAsync();
        }

        Assert.True(
            Carrying(seen, "AsyncCommand") >= 2,
            "Start and end must both publish the command's own name — a component filters on it, " +
            "and a spinner that never learns the command finished is as bad as one that never starts.");
    }

    [Fact]
    public async Task AsyncCommandWithParam_PublishesItsOwnName()
    {
        var model = new TestCommandModel();
        var seen = Record(model, out var subscription);
        using (subscription)
        {
            await model.AsyncCommandWithParam.ExecuteAsync(7);
        }

        Assert.True(
            Carrying(seen, "AsyncCommandWithParam") >= 2,
            "Start and end must both publish the command's own name — a component filters on it, " +
            "and a spinner that never learns the command finished is as bad as one that never starts.");
    }

    [Fact]
    public async Task CancelableCommand_PublishesItsOwnName()
    {
        var model = new TestCommandModel();
        var seen = Record(model, out var subscription);
        using (subscription)
        {
            await model.CancelableCommand.ExecuteAsync();
        }

        Assert.True(
            Carrying(seen, "CancelableCommand") >= 2,
            "Start and end must both publish the command's own name — a component filters on it, " +
            "and a spinner that never learns the command finished is as bad as one that never starts.");
    }

    [Fact]
    public async Task CancelableCommandWithParam_PublishesItsOwnName()
    {
        var model = new TestCommandModel();
        var seen = Record(model, out var subscription);
        using (subscription)
        {
            await model.CancelableAsyncCommandWithParam.ExecuteAsync(2);
        }

        Assert.True(
            Carrying(seen, "CancelableAsyncCommandWithParam") >= 2,
            "Start and end must both publish the command's own name — a component filters on it, " +
            "and a spinner that never learns the command finished is as bad as one that never starts.");
    }

    [Fact]
    public async Task ReturnCommand_PublishesItsOwnName()
    {
        var model = new TestReturnCommandModel();
        var seen = new List<string[]>();
        using var subscription = model.Observable.Subscribe(props => seen.Add(props));

        await model.AsyncReturnCommand.ExecuteAsync();

        Assert.True(
            Carrying(seen, "AsyncReturnCommand") >= 2,
            "Start and end must both publish the command's own name — a component filters on it, " +
            "and a spinner that never learns the command finished is as bad as one that never starts.");
    }

    [Fact]
    public async Task AsyncCommand_LastNotification_ReportsFinished()
    {
        var model = new TestCommandModel();
        var executingWhenSeen = new List<bool>();
        using var subscription = model.Observable.Subscribe(
            _ => executingWhenSeen.Add(model.AsyncCommand.Executing));

        await model.AsyncCommand.ExecuteAsync();

        Assert.True(executingWhenSeen.Count >= 2);
        Assert.True(executingWhenSeen[0], "The first notification should report the command running");
        Assert.False(executingWhenSeen[^1], "The last notification should report the command finished");
    }

    [Fact]
    public async Task AsyncCommandWithParam_LastNotification_ReportsFinished()
    {
        // The one the old test asserted the field for, after the await —
        // which passes whatever order the notification and the flag are in.
        var model = new TestCommandModel();
        var executingWhenSeen = new List<bool>();
        using var subscription = model.Observable.Subscribe(
            _ => executingWhenSeen.Add(model.AsyncCommandWithParam.Executing));

        await model.AsyncCommandWithParam.ExecuteAsync(5);

        Assert.True(executingWhenSeen.Count >= 2);
        Assert.True(executingWhenSeen[0], "The first notification should report the command running");
        Assert.False(executingWhenSeen[^1], "The last notification should report the command finished");
    }

    [Fact]
    public async Task Component_FilteredOnACommand_RendersWhenItRuns()
    {
        // End to end: this is a progress spinner. The component binds nothing
        // but the command, so its filter holds only the command's name — the
        // property the command body changes (Model.Value) is not in it. If the
        // command's own state changes do not reach the component, no button
        // anywhere repaints while it runs.
        Services.AddTransient<TestCommandModel>();

        var cut = Render<CommandFilterTestComponent>();
        Thread.Sleep(150);
        var before = cut.Instance.RenderCount;

        await cut.Instance.Model.AsyncCommandWithParam.ExecuteAsync(1);
        Thread.Sleep(150); // the subscription chunks over 100 ms

        _output.WriteLine($"Renders: {before} -> {cut.Instance.RenderCount}");
        Assert.True(cut.Instance.RenderCount > before,
            "A component filtered on a command must re-render when that command runs.");
        Assert.Contains("Executing: False", cut.Markup);
    }
}

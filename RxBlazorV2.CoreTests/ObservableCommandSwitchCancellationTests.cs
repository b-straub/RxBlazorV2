using RxBlazorV2.CoreTests.TestFixtures;

namespace RxBlazorV2.CoreTests;

/// <summary>
/// Cancellation of a delegate that is genuinely parked on an await.
///
/// <para>
/// The other cancellation suites all call Cancel() from inside a subscription, which runs while
/// ExecuteAsync is publishing its start notification - before it reaches await execute(...). That
/// never exercises a token the delegate is actually waiting on, which is what a user clicking a
/// cancel button mid-operation does.
/// </para>
/// </summary>
public class ObservableCommandSwitchCancellationTests
{
    private static async Task UntilParkedOnAwaitAsync()
    {
        await Task.Delay(50);
    }

    [Fact]
    public async Task MidFlightCancel_DelegateObservesCancellation()
    {
        var model = new SwitchCancellationModel();

        var run = model.RunCommand.ExecuteAsync();
        await UntilParkedOnAwaitAsync();
        model.RunCommand.Cancel();
        await run;

        Assert.Equal([1], model.Cancelled);
        Assert.Empty(model.Completed);
        Assert.Equal(0, model.Value);
        Assert.False(model.RunCommand.Executing);
    }

    /// <summary>
    /// A completed run leaves the source reusable, so this takes the CancellationTokenSource
    /// reuse branch of ResetCancellationToken rather than allocating a fresh source.
    /// </summary>
    [Fact]
    public async Task MidFlightCancel_AfterCompletedRun_StillCancels()
    {
        var model = new SwitchCancellationModel { DelayMilliseconds = 10 };
        await model.RunCommand.ExecuteAsync();
        Assert.Equal([1], model.Completed);

        model.DelayMilliseconds = 2000;
        var run = model.RunCommand.ExecuteAsync();
        await UntilParkedOnAwaitAsync();
        model.RunCommand.Cancel();
        await run;

        Assert.Equal([2], model.Cancelled);
        Assert.Equal([1], model.Completed);
    }

    [Fact]
    public async Task Switch_CancelsPreviousRun()
    {
        var model = new SwitchCancellationModel();

        var first = model.RunCommand.ExecuteAsync();
        await UntilParkedOnAwaitAsync();
        var second = model.RunCommand.ExecuteAsync();
        await UntilParkedOnAwaitAsync();

        // The switched-away run must have observed cancellation, not been left running.
        Assert.Equal([1], model.Cancelled);
        Assert.Empty(model.Completed);

        // The run that replaced it keeps the command executing.
        Assert.True(model.RunCommand.Executing);

        model.RunCommand.Cancel();
        await Task.WhenAll(first, second);

        Assert.Equal([1, 2], model.Cancelled);
        Assert.Empty(model.Completed);
        Assert.Equal(0, model.Value);
    }

    /// <summary>
    /// The original defect: after a switch, Cancel() reached the shared source but the
    /// switched-away run had lost its registration on it and ran to completion regardless.
    /// </summary>
    [Fact]
    public async Task Switch_ThenCancel_NothingCompletes()
    {
        var model = new SwitchCancellationModel();

        var first = model.RunCommand.ExecuteAsync();
        await UntilParkedOnAwaitAsync();
        var second = model.RunCommand.ExecuteAsync();
        await UntilParkedOnAwaitAsync();
        model.RunCommand.Cancel();
        await Task.WhenAll(first, second);

        Assert.Empty(model.Completed);
        Assert.Equal(0, model.Value);
        Assert.False(model.RunCommand.Executing);
    }

    [Fact]
    public async Task Switch_WithParam_CancelsPreviousRun()
    {
        var model = new SwitchCancellationModel();

        var first = model.RunWithParamCommand.ExecuteAsync(5);
        await UntilParkedOnAwaitAsync();
        var second = model.RunWithParamCommand.ExecuteAsync(7);
        await UntilParkedOnAwaitAsync();

        Assert.Equal([1], model.Cancelled);

        model.RunWithParamCommand.Cancel();
        await Task.WhenAll(first, second);

        Assert.Equal([1, 2], model.Cancelled);
        Assert.Empty(model.Completed);
        Assert.Equal(0, model.Value);
    }

    [Fact]
    public async Task Switch_ReturnCommand_CancelsPreviousRun()
    {
        var model = new SwitchCancellationModel();

        var first = model.RunReturnCommand.ExecuteAsync();
        await UntilParkedOnAwaitAsync();
        var second = model.RunReturnCommand.ExecuteAsync();
        await UntilParkedOnAwaitAsync();

        Assert.Equal([1], model.Cancelled);

        model.RunReturnCommand.Cancel();
        var results = await Task.WhenAll(first, second);

        Assert.Equal([1, 2], model.Cancelled);
        Assert.Empty(model.Completed);
        Assert.All(results, r => Assert.Null(r));
    }

    [Fact]
    public async Task Switch_ReturnCommandWithParam_CancelsPreviousRun()
    {
        var model = new SwitchCancellationModel();

        var first = model.RunReturnWithParamCommand.ExecuteAsync(5);
        await UntilParkedOnAwaitAsync();
        var second = model.RunReturnWithParamCommand.ExecuteAsync(7);
        await UntilParkedOnAwaitAsync();

        Assert.Equal([1], model.Cancelled);

        model.RunReturnWithParamCommand.Cancel();
        var results = await Task.WhenAll(first, second);

        Assert.Equal([1, 2], model.Cancelled);
        Assert.Empty(model.Completed);
        Assert.All(results, r => Assert.Null(r));
    }

    /// <summary>
    /// Switching is not cancellation of the command as a whole: the run that replaces the
    /// cancelled one must still be able to finish normally.
    /// </summary>
    [Fact]
    public async Task Switch_ReplacementRunCompletesNormally()
    {
        var model = new SwitchCancellationModel();

        var first = model.RunCommand.ExecuteAsync();
        await UntilParkedOnAwaitAsync();

        model.DelayMilliseconds = 10;
        var second = model.RunCommand.ExecuteAsync();
        await Task.WhenAll(first, second);

        Assert.Equal([1], model.Cancelled);
        Assert.Equal([2], model.Completed);
        Assert.Equal(1, model.Value);
        Assert.False(model.RunCommand.Executing);
    }
}

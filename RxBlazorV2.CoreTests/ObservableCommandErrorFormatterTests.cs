using RxBlazorV2.CoreTests.TestFixtures;
using RxBlazorV2.Model;

namespace RxBlazorV2.CoreTests;

/// <summary>
/// Runtime tests for the per-command error formatter feature wired through
/// <see cref="ObservableCommandBase.SetError"/>. Covers:
/// 1. Command.Error and Command.ErrorMessage are always populated on exception, regardless of
///    whether a StatusBaseModel is configured.
/// 2. When StatusBaseModel is wired, the formatted text is ALSO forwarded to it - the consumer
///    chooses which surface to render.
/// 3. Cancelable async factory short-circuits OperationCanceledException before invoking the formatter.
/// 4. ResetError() clears both surfaces.
/// 5. Static formatter method groups are accepted.
/// </summary>
public class ObservableCommandErrorFormatterTests
{
    private static string FormatBoom(Exception ex) => $"Custom prefix: {ex.Message}";

    private sealed class FormatterCounter
    {
        public int Calls { get; private set; }
        public Exception? LastException { get; private set; }

        public string Format(Exception ex)
        {
            Calls++;
            LastException = ex;
            return $"prefix: {ex.Message}";
        }
    }

    [Fact]
    public void SyncFactory_WithStatusModel_PopulatesBothCommandSurfacesAndStatusModel()
    {
        var model = new CounterModel();
        var status = new TestStatusModel();
        var counter = new FormatterCounter();

        var command = new ObservableCommandFactory(
            model,
            ["Counter1"],
            "RunCommand",
            "Run",
            execute: () => throw new InvalidOperationException("boom"),
            canExecute: null,
            statusModel: status,
            errorFormatter: counter.Format);

        command.Execute();

        // Formatter is invoked exactly once.
        Assert.Equal(1, counter.Calls);

        // Per-command surfaces are populated so a UI can bind an inline alert directly to the command.
        Assert.IsType<InvalidOperationException>(command.Error);
        Assert.Equal("prefix: boom", command.ErrorMessage);

        // The same formatted text is also forwarded to the status model for global logging / toasts.
        Assert.Single(status.Messages);
        Assert.Equal("prefix: boom", status.Messages[0].Message);
        Assert.Equal("RunCommand.Run", status.Messages[0].Source);
        Assert.Equal(StatusSeverity.Error, status.Messages[0].Severity);
    }

    [Fact]
    public void SyncFactory_NoStatusModel_PopulatesErrorMessageOnCommand()
    {
        var model = new CounterModel();
        var counter = new FormatterCounter();

        var command = new ObservableCommandFactory(
            model,
            ["Counter1"],
            "RunCommand",
            "Run",
            execute: () => throw new InvalidOperationException("boom"),
            canExecute: null,
            statusModel: null,
            errorFormatter: counter.Format);

        command.Execute();

        Assert.Equal(1, counter.Calls);
        Assert.NotNull(command.Error);
        Assert.IsType<InvalidOperationException>(command.Error);
        Assert.Equal("prefix: boom", command.ErrorMessage);
    }

    [Fact]
    public void SyncFactory_NoFormatter_NoStatusModel_ErrorMessageFallsBackToExceptionMessage()
    {
        var model = new CounterModel();

        var command = new ObservableCommandFactory(
            model,
            ["Counter1"],
            "RunCommand",
            "Run",
            execute: () => throw new InvalidOperationException("boom"));

        command.Execute();

        Assert.NotNull(command.Error);
        Assert.Equal("boom", command.ErrorMessage);
    }

    [Fact]
    public async Task AsyncFactory_WithStatusModel_PopulatesBothSurfaces()
    {
        var model = new CounterModel();
        var status = new TestStatusModel();

        var command = new ObservableCommandAsyncFactory(
            model,
            ["Counter1"],
            "LoadCommand",
            "Load",
            execute: () => throw new InvalidOperationException("boom"),
            canExecute: null,
            statusModel: status,
            errorFormatter: ex => $"Failed to load: {ex.Message}");

        await command.ExecuteAsync();

        Assert.IsType<InvalidOperationException>(command.Error);
        Assert.Equal("Failed to load: boom", command.ErrorMessage);
        Assert.Single(status.Messages);
        Assert.Equal("Failed to load: boom", status.Messages[0].Message);
        Assert.Equal("LoadCommand.Load", status.Messages[0].Source);
    }

    [Fact]
    public async Task AsyncFactory_NoStatusModel_PopulatesErrorMessageOnCommand()
    {
        var model = new CounterModel();

        var command = new ObservableCommandAsyncFactory(
            model,
            ["Counter1"],
            "LoadCommand",
            "Load",
            execute: () => throw new InvalidOperationException("boom"),
            canExecute: null,
            statusModel: null,
            errorFormatter: ex => $"Failed to load: {ex.Message}");

        await command.ExecuteAsync();

        Assert.IsType<InvalidOperationException>(command.Error);
        Assert.Equal("Failed to load: boom", command.ErrorMessage);
    }

    [Fact]
    public async Task CancelableAsyncFactory_OperationCanceled_DoesNotInvokeFormatter()
    {
        var model = new CounterModel();
        var status = new TestStatusModel();
        var counter = new FormatterCounter();

        var command = new ObservableCommandAsyncCancelableFactory(
            model,
            ["Counter1"],
            "WaitCommand",
            "Wait",
            execute: async ct =>
            {
                // Simulate work then cooperative cancellation.
                await Task.Delay(20, CancellationToken.None);
                ct.ThrowIfCancellationRequested();
            },
            canExecute: null,
            statusModel: status,
            errorFormatter: counter.Format);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(5);

        await command.ExecuteAsync(cts.Token);

        // OperationCanceledException is intercepted by the cancelable factory before the generic
        // catch arm runs, so the formatter is NOT invoked and no status message is recorded.
        Assert.Equal(0, counter.Calls);
        Assert.Empty(status.Messages);
        Assert.Null(command.Error);
        Assert.Null(command.ErrorMessage);
    }

    [Fact]
    public void ResetError_ClearsBothErrorAndErrorMessage()
    {
        var model = new CounterModel();

        var command = new ObservableCommandFactory(
            model,
            ["Counter1"],
            "RunCommand",
            "Run",
            execute: () => throw new InvalidOperationException("boom"),
            canExecute: null,
            statusModel: null,
            errorFormatter: ex => $"prefix: {ex.Message}");

        command.Execute();
        Assert.NotNull(command.Error);
        Assert.NotNull(command.ErrorMessage);

        command.ResetError();

        Assert.Null(command.Error);
        Assert.Null(command.ErrorMessage);
    }

    [Fact]
    public void StaticFormatterMethodGroup_BindsAndIsInvoked()
    {
        // The generator emits `formatErrorMethod` as a method-group reference that resolves to either
        // an instance or static method via standard C# overload resolution. Verify a static target works.
        var model = new CounterModel();

        var command = new ObservableCommandFactory(
            model,
            ["Counter1"],
            "RunCommand",
            "Run",
            execute: () => throw new InvalidOperationException("boom"),
            canExecute: null,
            statusModel: null,
            errorFormatter: FormatBoom);

        command.Execute();

        Assert.Equal("Custom prefix: boom", command.ErrorMessage);
    }
}

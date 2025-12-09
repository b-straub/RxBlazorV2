using Microsoft.Extensions.Time.Testing;
using RxBlazorV2.CoreTests.TestFixtures;

namespace RxBlazorV2.CoreTests;

public class ObservableModelIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public ObservableModelIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Observable_WithFakeTimeProvider_SchedulesCorrectly()
    {
        // Arrange
        var model = new TestObservableModel();
        var fakeTimeProvider = new FakeTimeProvider();
        var notificationCount = 0;
        var values = new List<int>();

        var observable = Observable.Range(0, 5, cancellationToken: TestContext.Current.CancellationToken);

        var subscription = observable
            .ObserveOn(fakeTimeProvider)
            .SubscribeOn(fakeTimeProvider)
            .Subscribe(value =>
            {
                notificationCount++;
                values.Add(value);
                model.Counter = value;
                _output.WriteLine($"Notification {notificationCount}: Value = {value}");
            });

        // Act
        fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(100));
        fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(100));
        fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(100));
        fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(100));
        fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(100));
        fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(100));

        // Assert
        Assert.Equal(5, notificationCount);
        Assert.Equal(4, model.Counter);
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, values);

        subscription.Dispose();
    }

    [Fact]
    public void ObservableModel_WithMultipleSubscribers_NotifiesAll()
    {
        // Arrange
        var model = new TestObservableModel();
        var notifications1 = new List<string>();
        var notifications2 = new List<string>();
        var notifications3 = new List<string>();

        model.Observable.Subscribe(props => notifications1.Add(string.Join(",", props)));
        model.Observable.Subscribe(props => notifications2.Add(string.Join(",", props)));
        model.Observable.Subscribe(props => notifications3.Add(string.Join(",", props)));

        // Act
        model.Counter = 1;
        model.Name = "Test";
        model.Counter = 2;

        // Assert
        Assert.Equal(3, notifications1.Count);
        Assert.Equal(3, notifications2.Count);
        Assert.Equal(3, notifications3.Count);
        Assert.Equal(notifications1, notifications2);
        Assert.Equal(notifications2, notifications3);
    }

    [Fact]
    public async Task CommandExecution_WithSuspension_BatchesNotifications()
    {
        // Arrange
        var model = new TestCommandModel();
        var notificationCount = 0;

        model.Observable.Subscribe(_ =>
        {
            notificationCount++;
            _output.WriteLine($"Notification {notificationCount}, Value: {model.Value}");
        });

        // Act
        using (model.SuspendNotifications())
        {
            await model.CancelableAsyncCommandWithParam.ExecuteAsync(1);
            await model.CancelableAsyncCommandWithParam.ExecuteAsync(2);
            await model.CancelableAsyncCommandWithParam.ExecuteAsync(3);
        }

        // Assert
        Assert.Equal(6, model.Value);
        Assert.Equal(3, model.ExecuteCount);
        // Notifications: 1 commands start + 1 batch at suspension
        Assert.True(model.CancelableAsyncCommandWithParam.CanExecute);
        Assert.False(model.CancelableAsyncCommandWithParam.Executing);
        Assert.Equal(2, notificationCount);
    }
    
    [Fact]
    public void PropertyChanges_DuringSuspension_WithBatches_FilterCorrectly()
    {
        // Arrange
        var model = new TestObservableModel();
        var notifications = new List<string[]>();

        model.Observable.Subscribe(props =>
        {
            notifications.Add(props);
            _output.WriteLine($"Notification: {string.Join(", ", props)}");
        });

        // Act
        using (model.SuspendNotifications("batch1"))
        {
            model.Counter = 1; // Not batched - immediate
            model.BatchProperty1 = 2; // batch1 - suspended
            model.Name = "Test"; // Not batched - immediate
            model.BatchProperty2 = 3; // batch1 & batch2 - suspended
        }

        // Assert
        Assert.Equal(3, notifications.Count);
        // First two are immediate
        Assert.Equal($"Model.{nameof(model.Counter)}", notifications[0][0]);
        Assert.Equal($"Model.{nameof(model.Name)}", notifications[1][0]);
        // Last notification is batched properties
        Assert.Equal(2, notifications[2].Length);
        Assert.Contains($"Model.{nameof(model.BatchProperty1)}", notifications[2]);
        Assert.Contains($"Model.{nameof(model.BatchProperty2)}", notifications[2]);
    }

    [Fact]
    public async Task MultipleAsyncCommands_ExecuteSequentially()
    {
        // Arrange
        var model = new TestCommandModel();
        var executionOrder = new List<int>();

        using var subscription = model.Observable.Subscribe(_ =>
        {
            if (model.AsyncCommandWithParam.Executing && model.LastParameter != 0)
            {
                if (!executionOrder.Contains(model.LastParameter))
                {
                    executionOrder.Add(model.LastParameter);
                }
            }
        });

        // Act
        await model.AsyncCommandWithParam.ExecuteAsync(1);
        await model.AsyncCommandWithParam.ExecuteAsync(2);
        await model.AsyncCommandWithParam.ExecuteAsync(3);

        // Assert
        Assert.Equal(new[] { 1, 2, 3 }, executionOrder);
        Assert.Equal(6, model.Value);
    }

    [Fact]
    public void ObservableModel_StateChanges_WithComplexScenario()
    {
        // Arrange
        var model = new TestObservableModel();
        var allNotifications = new List<string[]>();

        model.Observable.Subscribe(props =>
        {
            allNotifications.Add(props);
            _output.WriteLine($"Notification {allNotifications.Count}: {string.Join(", ", props)}");
        });

        // Act - Complex scenario with mixed operations
        model.Counter = 1;

        using (model.SuspendNotifications())
        {
            model.Counter = 2;
            model.Name = "First";
            model.Counter = 3;
        }

        model.Name = "Second";

        using (model.SuspendNotifications("batch1"))
        {
            model.BatchProperty1 = 10;
            model.Counter = 4;
        }

        // Assert
        Assert.Equal(5, allNotifications.Count);

        // First notification: Counter = 1
        Assert.Single(allNotifications[0]);
        Assert.Equal($"Model.{nameof(model.Counter)}", allNotifications[0][0]);

        // Second notification: Batch from first suspension
        Assert.Equal(2, allNotifications[1].Length);

        // Third notification: Name = "Second"
        Assert.Single(allNotifications[2]);
        Assert.Equal($"Model.{nameof(model.Name)}", allNotifications[2][0]);

        // Fourth notification: Counter = 4 (immediate during batch1 suspension)
        Assert.Single(allNotifications[3]);
        Assert.Equal($"Model.{nameof(model.Counter)}", allNotifications[3][0]);

        // Fifth notification: BatchProperty1 at end of suspension
        Assert.Single(allNotifications[4]);
        Assert.Equal($"Model.{nameof(model.BatchProperty1)}", allNotifications[4][0]);
    }

    [Fact]
    public void ObservableModel_ContextReady_InitializesOnce()
    {
        // Arrange
        var model = new TestObservableModel();

        // Act
        model.ContextReady();
        model.Counter = 5;
        model.ContextReady();
        model.Counter = 10;
        model.ContextReady();

        // Assert
        Assert.True(model.ContextReadyCalled);
        Assert.Equal(10, model.Counter);
    }

    [Fact]
    public async Task ObservableModel_ContextReadyAsync_InitializesOnce()
    {
        // Arrange
        var model = new TestObservableModel();

        // Act
        await model.ContextReadyAsync();
        model.Counter = 5;
        await model.ContextReadyAsync();
        model.Counter = 10;
        await model.ContextReadyAsync();

        // Assert
        Assert.True(model.ContextReadyAsyncCalled);
        Assert.Equal(10, model.Counter);
    }
}

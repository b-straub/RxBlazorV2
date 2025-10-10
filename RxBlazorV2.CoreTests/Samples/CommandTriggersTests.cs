using RxBlazorV2Sample.Samples.CommandTriggers;

namespace RxBlazorV2.CoreTests.Samples;

public class CommandTriggersTests
{
    private readonly CommandTriggersModel _model;

    public CommandTriggersTests(CommandTriggersModel model)
    {
        _model = model;
    }

    [Fact]
    public async Task SearchCommand_WhenSearchTextChanges_ShouldAutoExecute()
    {
        // Arrange
        _model.SearchText = "test";
        await Task.Delay(100, TestContext.Current.CancellationToken); // Allow reactive pipeline to process

        // Act
        _model.SearchText = "new search";
        await Task.Delay(600, TestContext.Current.CancellationToken); // Wait for debounce and execution

        // Assert
        Assert.Contains("Found results for 'new search'", _model.SearchResults);
    }

    [Fact]
    public void SearchCommand_WhenSearchTextTooShort_CannotExecute()
    {
        // Arrange
        _model.MinLength = 5;
        _model.SearchText = "ab";

        // Act & Assert
        Assert.False(_model.SearchCommand.CanExecute);
    }

    [Fact]
    public void SearchCommand_WhenSearchTextMeetsMinLength_CanExecute()
    {
        // Arrange
        _model.MinLength = 3;
        _model.SearchText = "test";

        // Act & Assert
        Assert.True(_model.SearchCommand.CanExecute);
    }

    [Fact]
    public async Task ParametrizedSearchCommand_ShouldAcceptParameter()
    {
        // Arrange
        _model.SearchText = "test";

        // Act
        await _model.ParametrizedSearchCommand.ExecuteAsync("manual");

        // Assert
        //Assert.Contains("manual mode", _model.SearchResults);
        Assert.Contains(_model.LogEntries, le => le.Message.Contains("manual mode"));
    }

    [Fact]
    public async Task SearchCommand_MultipleTriggers_ShouldIncrementSearchCount()
    {
        // Arrange
        _model.SearchText = "test";
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // Act
        await _model.SearchCommand.ExecuteAsync();
        await Task.Delay(100, TestContext.Current.CancellationToken);


        await _model.SearchCommand.ExecuteAsync();
        await Task.Delay(100, TestContext.Current.CancellationToken);
        
        // Assert search count
        // 2 from _model.SearchText -> PerformSearchAsync, PerformParametrizedSearchAsync
        // 2 from direct calls to PerformSearchAsync
        Assert.Equal(4, _model.SearchCount);
    }
    
    [Fact]
    public async Task SearchCommand_MultipleTriggers_ShouldDropSearchCount()
    {
        // Arrange
        _model.SearchText = "test1";
        await Task.Delay(10, TestContext.Current.CancellationToken);
        _model.SearchText = "test2";
        await Task.Delay(10, TestContext.Current.CancellationToken);
        _model.SearchText = "test3";
        await Task.Delay(10, TestContext.Current.CancellationToken);

        await Task.Delay(2000, TestContext.Current.CancellationToken);
        // Assert search count
        // 1 from _model.SearchText -> PerformSearchAsync
        // 3 from _model.SearchText -> PerformParametrizedSearchAsync
        Assert.Equal(4, _model.SearchCount);
        // only last trigger should be executed
        Assert.Contains("test3", _model.SearchResults);
    }
}

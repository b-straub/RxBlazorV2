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
    public async Task SwitchSearchCommand_WhenTextChanges_ShouldAutoExecute()
    {
        // Arrange
        _model.SwitchSearchText = "test";
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // Act
        _model.SwitchSearchText = "new search";
        await Task.Delay(1000, TestContext.Current.CancellationToken); // Wait for execution

        // Assert
        Assert.Contains("new search", _model.SearchResults);
        Assert.True(_model.SwitchSearchResults.Count > 0);
    }

    [Fact]
    public async Task SwitchSearchCommand_MultipleQuickChanges_ShouldCancelPrevious()
    {
        // Arrange & Act - Type quickly to trigger cancellations
        _model.SwitchSearchText = "test1";
        await Task.Delay(50, TestContext.Current.CancellationToken);
        _model.SwitchSearchText = "test2";
        await Task.Delay(50, TestContext.Current.CancellationToken);
        _model.SwitchSearchText = "test3";
        await Task.Delay(1000, TestContext.Current.CancellationToken); // Wait for last one to complete

        // Assert
        var cancelledCount = _model.SwitchSearchResults.Count(r => r.Cancelled);
        var completedCount = _model.SwitchSearchResults.Count(r => r.EndTime.HasValue && !r.Cancelled);

        // At least some searches should be cancelled
        Assert.True(cancelledCount > 0, "Expected some searches to be cancelled");
        // Only the last search should complete
        Assert.True(completedCount <= 1, "Expected at most 1 search to complete");
        // Last search text should be in results
        Assert.Contains("test3", _model.SearchResults);
    }

    [Fact]
    public void SwitchSearchCommand_WhenTextTooShort_CannotExecute()
    {
        // Arrange
        _model.MinLength = 5;
        _model.SwitchSearchText = "ab";

        // Act & Assert
        Assert.False(_model.SwitchSearchCommand.CanExecute);
    }

    [Fact]
    public void SwitchSearchCommand_WhenTextMeetsMinLength_CanExecute()
    {
        // Arrange
        _model.MinLength = 3;
        _model.SwitchSearchText = "test";

        // Act & Assert
        Assert.True(_model.SwitchSearchCommand.CanExecute);
    }

    [Fact]
    public async Task MergeSearchCommand_WhenTextChanges_ShouldAutoExecute()
    {
        // Arrange
        _model.MergeSearchText = "test";
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // Act
        _model.MergeSearchText = "merge search";
        await Task.Delay(1000, TestContext.Current.CancellationToken); // Wait for execution

        // Assert
        Assert.Contains("merge search", _model.SearchResults);
        Assert.True(_model.MergeSearchResults.Count > 0);
    }

    [Fact]
    public async Task MergeSearchCommand_MultipleQuickChanges_ShouldCompleteAll()
    {
        // Arrange & Act - Type quickly but all should complete
        _model.MergeSearchText = "merge1";
        await Task.Delay(50, TestContext.Current.CancellationToken);
        _model.MergeSearchText = "merge2";
        await Task.Delay(50, TestContext.Current.CancellationToken);
        _model.MergeSearchText = "merge3";
        await Task.Delay(1500, TestContext.Current.CancellationToken); // Wait for all to complete

        // Assert
        // All merge searches should complete (none cancelled)
        Assert.True(_model.MergeSearchResults.Count >= 3, "Expected at least 3 searches to be started");
        Assert.All(_model.MergeSearchResults, result => Assert.False(result.Cancelled));

        // All searches should eventually complete
        var completedCount = _model.MergeSearchResults.Count(r => r.EndTime.HasValue);
        Assert.True(completedCount >= 3, $"Expected at least 3 searches to complete, got {completedCount}");
    }

    [Fact]
    public void MergeSearchCommand_WhenTextTooShort_CannotExecute()
    {
        // Arrange
        _model.MinLength = 5;
        _model.MergeSearchText = "ab";

        // Act & Assert
        Assert.False(_model.MergeSearchCommand.CanExecute);
    }

    [Fact]
    public void MergeSearchCommand_WhenTextMeetsMinLength_CanExecute()
    {
        // Arrange
        _model.MinLength = 3;
        _model.MergeSearchText = "test";

        // Act & Assert
        Assert.True(_model.MergeSearchCommand.CanExecute);
    }

    [Fact]
    public async Task ClearResultsCommand_ShouldClearAllResults()
    {
        // Arrange - Add some searches
        _model.SwitchSearchText = "test";
        await Task.Delay(1000, TestContext.Current.CancellationToken);
        _model.MergeSearchText = "merge";
        await Task.Delay(1000, TestContext.Current.CancellationToken);

        // Act
        _model.ClearResultsCommand.Execute();

        // Assert
        Assert.Empty(_model.SwitchSearchResults);
        Assert.Empty(_model.MergeSearchResults);
        Assert.Equal(0, _model.SearchCount);
        Assert.Contains("cleared", _model.SearchResults.ToLower());
    }

    [Fact]
    public async Task SwitchSearchResults_ShouldTrackStartAndEndTimes()
    {
        // Arrange & Act
        _model.SwitchSearchText = "timing test";
        await Task.Delay(1000, TestContext.Current.CancellationToken);

        // Assert
        var completedSearch = _model.SwitchSearchResults.FirstOrDefault(r => r.EndTime.HasValue && !r.Cancelled);
        Assert.NotNull(completedSearch);
        Assert.True(completedSearch.EndTime > completedSearch.StartTime);
        Assert.Equal("timing test", completedSearch.Query);
        Assert.Equal("Switch", completedSearch.Mode);
    }

    [Fact]
    public async Task MergeSearchResults_ShouldTrackStartAndEndTimes()
    {
        // Arrange & Act
        _model.MergeSearchText = "merge timing";
        await Task.Delay(1000, TestContext.Current.CancellationToken);

        // Assert
        var completedSearch = _model.MergeSearchResults.FirstOrDefault(r => r.EndTime.HasValue);
        Assert.NotNull(completedSearch);
        Assert.True(completedSearch.EndTime > completedSearch.StartTime);
        Assert.Equal("merge timing", completedSearch.Query);
        Assert.Equal("Merge", completedSearch.Mode);
    }

    [Fact]
    public async Task SwitchAndMerge_ShouldWorkIndependently()
    {
        // Arrange & Act - Trigger both search types
        _model.SwitchSearchText = "switch1";
        _model.MergeSearchText = "merge1";
        await Task.Delay(100, TestContext.Current.CancellationToken);

        _model.SwitchSearchText = "switch2";
        _model.MergeSearchText = "merge2";
        await Task.Delay(1500, TestContext.Current.CancellationToken);

        // Assert - Both should have results
        Assert.True(_model.SwitchSearchResults.Count > 0, "Switch results should have entries");
        Assert.True(_model.MergeSearchResults.Count > 0, "Merge results should have entries");

        // Switch should have cancellations, merge should not
        var switchCancelled = _model.SwitchSearchResults.Any(r => r.Cancelled);
        var mergeCancelled = _model.MergeSearchResults.Any(r => r.Cancelled);

        Assert.True(switchCancelled, "Switch searches should have cancellations");
        Assert.False(mergeCancelled, "Merge searches should not have cancellations");
    }

    [Fact]
    public async Task SearchCount_ShouldIncrementForCompletedSearches()
    {
        // Arrange
        var initialCount = _model.SearchCount;

        // Act - Run one of each type
        _model.SwitchSearchText = "count test";
        await Task.Delay(1000, TestContext.Current.CancellationToken);

        _model.MergeSearchText = "count test";
        await Task.Delay(1000, TestContext.Current.CancellationToken);

        // Assert - Count should have increased by completed searches
        Assert.True(_model.SearchCount > initialCount);
    }
}

using RxBlazorV2Sample.Samples.ObservableBatches;

namespace RxBlazorV2.CoreTests.Samples;

public class ObservableBatchesTests
{
    private readonly ObservableBatchesModel _model;

    public ObservableBatchesTests(ObservableBatchesModel model)
    {
        _model = model;
    }

    [Fact]
    public void UpdateNameCommand_ShouldUpdateBothNameProperties()
    {
        // Act
        _model.UpdateNameCommand.Execute();

        // Assert
        Assert.NotEmpty(_model.FirstName);
        Assert.NotEmpty(_model.LastName);
        Assert.Contains("Name updated", _model.LastUpdate);
    }

    [Fact]
    public void UpdateCoordinatesCommand_ShouldUpdateBothCoordinates()
    {
        // Act
        _model.UpdateCoordinatesCommand.Execute();

        // Assert
        Assert.NotEqual(0, _model.Latitude);
        Assert.NotEqual(0, _model.Longitude);
        Assert.Contains("Coordinates batch updated", _model.LastUpdate);
    }

    [Fact]
    public void UpdateMeasurementsCommand_ShouldUpdateTemperatureAndHumidity()
    {
        // Act
        _model.UpdateMeasurementsCommand.Execute();

        // Assert
        Assert.InRange(_model.Temperature, -10, 40);
        Assert.InRange(_model.Humidity, 20, 100);
        Assert.Contains("Measurements batch updated", _model.LastUpdate);
    }

    [Fact]
    public void UpdateAllDataCommand_ShouldUpdateAllMeasurementProperties()
    {
        // Act
        _model.UpdateAllDataCommand.Execute();

        // Assert
        Assert.InRange(_model.Temperature, -10, 40);
        Assert.InRange(_model.Humidity, 20, 100);
        Assert.InRange(_model.Pressure, 950, 1050);
        Assert.Contains("All-data batch updated", _model.LastUpdate);
    }

    [Fact]
    public void UpdateCommands_ShouldIncrementUpdateCount()
    {
        // Act
        _model.UpdateNameCommand.Execute();
        _model.UpdateCoordinatesCommand.Execute();
        _model.UpdateMeasurementsCommand.Execute();

        // Assert
        Assert.Equal(3, _model.UpdateCount);
    }

    [Fact]
    public void CoordinateBatch_ShouldUpdateBothProperties()
    {
        // Arrange
        var initialLat = _model.Latitude;
        var initialLon = _model.Longitude;

        // Act
        _model.UpdateCoordinatesCommand.Execute();

        // Assert - Both properties should be updated in the batch
        Assert.NotEqual(initialLat, _model.Latitude);
        Assert.NotEqual(initialLon, _model.Longitude);
    }
}

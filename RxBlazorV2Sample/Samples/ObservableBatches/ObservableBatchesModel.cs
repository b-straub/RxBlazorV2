using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.ObservableBatches;

[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class ObservableBatchesModel : SampleBaseModel
{
    public override string Usage => "Group properties into batches to notify subscribers together";
    // Individual properties
    public partial string FirstName { get; set; } = "";
    public partial string LastName { get; set; } = "";

    // Properties in the same batch notify together
    [ObservableBatch("coordinates")]
    public partial double Latitude { get; set; }

    [ObservableBatch("coordinates")]
    public partial double Longitude { get; set; }

    // Properties in multiple batches
    [ObservableBatch("measurements")]
    [ObservableBatch("all-data")]
    public partial int Temperature { get; set; }

    [ObservableBatch("measurements")]
    [ObservableBatch("all-data")]
    public partial int Humidity { get; set; }

    [ObservableBatch("all-data")]
    public partial int Pressure { get; set; }

    public partial string LastUpdate { get; set; } = "Not updated yet";
    public partial int UpdateCount { get; set; }

    [ObservableCommand(nameof(UpdateName))]
    public partial IObservableCommand UpdateNameCommand { get; }

    [ObservableCommand(nameof(UpdateCoordinates))]
    public partial IObservableCommand UpdateCoordinatesCommand { get; }

    [ObservableCommand(nameof(UpdateMeasurements))]
    public partial IObservableCommand UpdateMeasurementsCommand { get; }

    [ObservableCommand(nameof(UpdateAllData))]
    public partial IObservableCommand UpdateAllDataCommand { get; }

    private void UpdateName()
    {
        FirstName = $"First{Random.Shared.Next(100)}";
        LastName = $"Last{Random.Shared.Next(100)}";
        UpdateCount++;
        LastUpdate = $"Name updated at {DateTime.Now:HH:mm:ss}";
        LogEntries.Add(new LogEntry($"Name updated: {FirstName} {LastName}", DateTime.Now));
    }

    private void UpdateCoordinates()
    {
        Latitude = Random.Shared.NextDouble() * 180 - 90;
        Longitude = Random.Shared.NextDouble() * 360 - 180;
        UpdateCount++;
        LastUpdate = $"Coordinates batch updated at {DateTime.Now:HH:mm:ss}";
        LogEntries.Add(new LogEntry($"Coordinates batch updated: ({Latitude:F2}, {Longitude:F2})", DateTime.Now));
    }

    private void UpdateMeasurements()
    {
        Temperature = Random.Shared.Next(-10, 40);
        Humidity = Random.Shared.Next(20, 100);
        UpdateCount++;
        LastUpdate = $"Measurements batch updated at {DateTime.Now:HH:mm:ss}";
        LogEntries.Add(new LogEntry($"Measurements batch updated: {Temperature}°C, {Humidity}%", DateTime.Now));
    }

    private void UpdateAllData()
    {
        Temperature = Random.Shared.Next(-10, 40);
        Humidity = Random.Shared.Next(20, 100);
        Pressure = Random.Shared.Next(950, 1050);
        UpdateCount++;
        LastUpdate = $"All-data batch updated at {DateTime.Now:HH:mm:ss}";
        LogEntries.Add(new LogEntry($"All-data batch updated: {Temperature}°C, {Humidity}%, {Pressure}hPa", DateTime.Now));
    }
}

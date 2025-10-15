using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.ValueEquality;

public record PersonRecord(string Name, int Age);

public record struct PointStruct(double X, double Y);

[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class ValueEqualityModel : SampleBaseModel
{
    public override string Usage => "Records and value types use value equality to prevent unnecessary notifications";
    public static readonly PersonRecord Person1 = new("Alice", 30);
    public static readonly PersonRecord Person2 = new("Alice", 30);
    public static readonly PersonRecord Person3 = new("Bob", 25);

    public static readonly PointStruct Point1 = new(10.5, 20.3);
    public static readonly PointStruct Point2 = new(10.5, 20.3);
    public static readonly PointStruct Point3 = new(15.0, 25.0);

    public partial PersonRecord CurrentPerson { get; set; } = Person1;
    public partial PointStruct CurrentPoint { get; set; } = Point1;
    public partial Guid CurrentGuid { get; set; } = Guid.NewGuid();
    public partial DateTime CurrentDateTime { get; set; } = DateTime.Now;
    public partial decimal CurrentDecimal { get; set; } = 123.45m;

    public partial string LastMessage { get; set; } = "Click buttons to see value equality in action";

    [ObservableCommand(nameof(SetPersonToSameValue))]
    public partial IObservableCommand SetPersonToSameValueCommand { get; }

    [ObservableCommand(nameof(SetPersonToDifferentValue))]
    public partial IObservableCommand SetPersonToDifferentValueCommand { get; }

    [ObservableCommand(nameof(SetPointToSameValue))]
    public partial IObservableCommand SetPointToSameValueCommand { get; }

    [ObservableCommand(nameof(SetPointToDifferentValue))]
    public partial IObservableCommand SetPointToDifferentValueCommand { get; }

    [ObservableCommand(nameof(SetGuidToSameValue))]
    public partial IObservableCommand SetGuidToSameValueCommand { get; }

    [ObservableCommand(nameof(SetGuidToNewValue))]
    public partial IObservableCommand SetGuidToNewValueCommand { get; }

    private void SetPersonToSameValue()
    {
        CurrentPerson = Person2; // Same value as Person1 (Alice, 30)
        LastMessage = "Set CurrentPerson to same value (Alice, 30) - No property change notification sent";
        LogEntries.Add(new LogEntry("Set person to same value - no notification", DateTime.Now));
    }

    private void SetPersonToDifferentValue()
    {
        CurrentPerson = Person3; // Different value (Bob, 25)
        LastMessage = "Set CurrentPerson to different value (Bob, 25) - Property change notification sent";
        LogEntries.Add(new LogEntry($"Set person to different value: {Person3.Name}, {Person3.Age}", DateTime.Now));
    }

    private void SetPointToSameValue()
    {
        CurrentPoint = Point2; // Same value as Point1 (10.5, 20.3)
        LastMessage = "Set CurrentPoint to same value (10.5, 20.3) - No property change notification sent";
        LogEntries.Add(new LogEntry("Set point to same value - no notification", DateTime.Now));
    }

    private void SetPointToDifferentValue()
    {
        CurrentPoint = Point3; // Different value (15.0, 25.0)
        LastMessage = "Set CurrentPoint to different value (15.0, 25.0) - Property change notification sent";
        LogEntries.Add(new LogEntry($"Set point to different value: ({Point3.X}, {Point3.Y})", DateTime.Now));
    }

    private void SetGuidToSameValue()
    {
        var temp = CurrentGuid;
        CurrentGuid = temp; // Same value
        LastMessage = "Set CurrentGuid to same value - No property change notification sent";
        LogEntries.Add(new LogEntry("Set Guid to same value - no notification", DateTime.Now));
    }

    private void SetGuidToNewValue()
    {
        CurrentGuid = Guid.NewGuid(); // New value
        LastMessage = "Set CurrentGuid to new value - Property change notification sent";
        LogEntries.Add(new LogEntry($"Set Guid to new value: {CurrentGuid}", DateTime.Now));
    }
}

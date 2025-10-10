using RxBlazorV2Sample.Samples.ValueEquality;

namespace RxBlazorV2.CoreTests.Samples;

public class ValueEqualityTests
{
    private readonly ValueEqualityModel _model;

    public ValueEqualityTests(ValueEqualityModel model)
    {
        _model = model;
    }

    [Fact]
    public void SetPersonToSameValue_ShouldUpdateMessage()
    {
        // Arrange
        _model.CurrentPerson = ValueEqualityModel.Person1;

        // Act
        _model.SetPersonToSameValueCommand.Execute();

        // Assert
        Assert.Contains("same value", _model.LastMessage);
        Assert.Equal(ValueEqualityModel.Person2, _model.CurrentPerson);
    }

    [Fact]
    public void SetPersonToDifferentValue_ShouldUpdatePersonAndMessage()
    {
        // Arrange
        _model.CurrentPerson = ValueEqualityModel.Person1;

        // Act
        _model.SetPersonToDifferentValueCommand.Execute();

        // Assert
        Assert.Equal(ValueEqualityModel.Person3, _model.CurrentPerson);
        Assert.Contains("different value", _model.LastMessage);
    }

    [Fact]
    public void SetPointToSameValue_ShouldUpdateMessage()
    {
        // Arrange
        _model.CurrentPoint = ValueEqualityModel.Point1;

        // Act
        _model.SetPointToSameValueCommand.Execute();

        // Assert
        Assert.Contains("same value", _model.LastMessage);
        Assert.Equal(ValueEqualityModel.Point2, _model.CurrentPoint);
    }

    [Fact]
    public void SetPointToDifferentValue_ShouldUpdatePointAndMessage()
    {
        // Arrange
        _model.CurrentPoint = ValueEqualityModel.Point1;

        // Act
        _model.SetPointToDifferentValueCommand.Execute();

        // Assert
        Assert.Equal(ValueEqualityModel.Point3, _model.CurrentPoint);
        Assert.Contains("different value", _model.LastMessage);
    }

    [Fact]
    public void SetGuidToSameValue_ShouldUpdateMessage()
    {
        // Arrange
        var testGuid = Guid.NewGuid();
        _model.CurrentGuid = testGuid;

        // Act
        _model.SetGuidToSameValueCommand.Execute();

        // Assert
        Assert.Contains("same value", _model.LastMessage);
        Assert.Equal(testGuid, _model.CurrentGuid);
    }

    [Fact]
    public void SetGuidToNewValue_ShouldUpdateGuidAndMessage()
    {
        // Arrange
        var initialGuid = Guid.NewGuid();
        _model.CurrentGuid = initialGuid;

        // Act
        _model.SetGuidToNewValueCommand.Execute();

        // Assert
        Assert.NotEqual(initialGuid, _model.CurrentGuid);
        Assert.Contains("new value", _model.LastMessage);
    }

    [Fact]
    public void RecordEquality_Person1AndPerson2_ShouldBeEqual()
    {
        // Assert - Records with same values should be equal
        Assert.Equal(ValueEqualityModel.Person1, ValueEqualityModel.Person2);
    }

    [Fact]
    public void RecordEquality_Person1AndPerson3_ShouldNotBeEqual()
    {
        // Assert - Records with different values should not be equal
        Assert.NotEqual(ValueEqualityModel.Person1, ValueEqualityModel.Person3);
    }

    [Fact]
    public void RecordStructEquality_Point1AndPoint2_ShouldBeEqual()
    {
        // Assert - Record structs with same values should be equal
        Assert.Equal(ValueEqualityModel.Point1, ValueEqualityModel.Point2);
    }

    [Fact]
    public void RecordStructEquality_Point1AndPoint3_ShouldNotBeEqual()
    {
        // Assert - Record structs with different values should not be equal
        Assert.NotEqual(ValueEqualityModel.Point1, ValueEqualityModel.Point3);
    }

    [Fact]
    public void CurrentDecimal_DirectPropertySet_ShouldUpdate()
    {
        // Arrange
        _model.CurrentDecimal = 123.45m;

        // Act
        _model.CurrentDecimal = 456.78m;

        // Assert
        Assert.Equal(456.78m, _model.CurrentDecimal);
    }
}

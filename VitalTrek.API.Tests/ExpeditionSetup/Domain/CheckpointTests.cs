using System;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using Xunit;

namespace VitalTrek.API.Tests.ExpeditionSetup.Domain;

public class CheckpointTests
{
    private static CreateCheckpointCommand BuildCommand(
        int agencyId = 1,
        int routeId = 7,
        string? location = "Laguna Mitucocha",
        int sequenceOrder = 2,
        string? bluetoothId = "VT-CP-0002",
        double latitude = -10.2731,
        double longitude = -76.9034) =>
        new(agencyId, routeId, location!, sequenceOrder, bluetoothId!, latitude, longitude);

    [Fact]
    public void Constructor_WithValidCommand_AssociatesCheckpointWithItsRoute()
    {
        // Arrange
        var command = BuildCommand();

        // Act
        var checkpoint = new Checkpoint(command);

        // Assert
        Assert.Equal(command.AgencyId, checkpoint.AgencyId);
        Assert.Equal(command.RouteId, checkpoint.RouteId);
        Assert.Equal(command.SequenceOrder, checkpoint.SequenceOrder);
        Assert.Equal(command.Location, checkpoint.Location);
        Assert.Equal(command.BluetoothId, checkpoint.BluetoothId);
        Assert.Equal(command.Latitude, checkpoint.Latitude);
        Assert.Equal(command.Longitude, checkpoint.Longitude);
    }

    [Fact]
    public void Constructor_WithLowercaseBluetoothId_StoresItInUpperCase()
    {
        // Arrange
        var command = BuildCommand(bluetoothId: "vt-cp-0002");

        // Act
        var checkpoint = new Checkpoint(command);

        // Assert
        Assert.Equal("VT-CP-0002", checkpoint.BluetoothId);
    }

    [Fact]
    public void Constructor_WithSurroundingWhitespace_TrimsLocationAndBluetoothId()
    {
        // Arrange
        var command = BuildCommand(location: "  Laguna Mitucocha  ", bluetoothId: "  VT-CP-0002  ");

        // Act
        var checkpoint = new Checkpoint(command);

        // Assert
        Assert.Equal("Laguna Mitucocha", checkpoint.Location);
        Assert.Equal("VT-CP-0002", checkpoint.BluetoothId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveAgencyId_ThrowsArgumentException(int agencyId)
    {
        // Arrange
        var command = BuildCommand(agencyId: agencyId);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Checkpoint(command));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveRouteId_ThrowsArgumentException(int routeId)
    {
        // Arrange
        var command = BuildCommand(routeId: routeId);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Checkpoint(command));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Constructor_WithNonPositiveSequenceOrder_ThrowsArgumentException(int sequenceOrder)
    {
        // Arrange
        var command = BuildCommand(sequenceOrder: sequenceOrder);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Checkpoint(command));
    }

    [Fact]
    public void Constructor_WithFirstSequenceOrder_CreatesCheckpoint()
    {
        // Arrange
        var command = BuildCommand(sequenceOrder: 1);

        // Act
        var checkpoint = new Checkpoint(command);

        // Assert
        Assert.Equal(1, checkpoint.SequenceOrder);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyLocation_ThrowsArgumentException(string? location)
    {
        // Arrange
        var command = BuildCommand(location: location);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Checkpoint(command));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyBluetoothId_ThrowsArgumentException(string? bluetoothId)
    {
        // Arrange
        var command = BuildCommand(bluetoothId: bluetoothId);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Checkpoint(command));
    }

    [Fact]
    public void Constructor_WithLocationLongerThanMaximumLength_ThrowsArgumentException()
    {
        // Arrange
        var command = BuildCommand(location: new string('a', 121));

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Checkpoint(command));
    }

    [Fact]
    public void Constructor_WithBluetoothIdLongerThanMaximumLength_ThrowsArgumentException()
    {
        // Arrange
        var command = BuildCommand(bluetoothId: new string('a', 51));

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Checkpoint(command));
    }

    [Theory]
    [InlineData(90.1)]
    [InlineData(-90.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Constructor_WithLatitudeOutOfRange_ThrowsArgumentException(double latitude)
    {
        // Arrange
        var command = BuildCommand(latitude: latitude);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Checkpoint(command));
    }

    [Theory]
    [InlineData(180.1)]
    [InlineData(-180.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Constructor_WithLongitudeOutOfRange_ThrowsArgumentException(double longitude)
    {
        // Arrange
        var command = BuildCommand(longitude: longitude);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Checkpoint(command));
    }

    [Theory]
    [InlineData(90, 180)]
    [InlineData(-90, -180)]
    [InlineData(0, 0)]
    public void Constructor_WithCoordinatesAtRangeLimits_CreatesCheckpoint(double latitude, double longitude)
    {
        // Arrange
        var command = BuildCommand(latitude: latitude, longitude: longitude);

        // Act
        var checkpoint = new Checkpoint(command);

        // Assert
        Assert.Equal(latitude, checkpoint.Latitude);
        Assert.Equal(longitude, checkpoint.Longitude);
    }
}

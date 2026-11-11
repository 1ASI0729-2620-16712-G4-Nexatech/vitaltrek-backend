using System;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;
using VitalTrek.API.ExpeditionSetup.Domain.Model.Commands;
using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;
using Xunit;

namespace VitalTrek.API.Tests.ExpeditionSetup.Domain;

public class RouteTests
{
    private static CreateRouteCommand BuildCommand(
        int agencyId = 1,
        string? name = "Huayhuash Alpine Circuit",
        string? origin = "Llamac",
        string? destination = "Llamac",
        decimal distanceKm = 84.5m,
        int elevationGainMeters = 5240,
        RouteDifficulty difficulty = RouteDifficulty.Advanced,
        int estimatedDurationMinutes = 5760) =>
        new(agencyId, name!, origin!, destination!, distanceKm, elevationGainMeters, difficulty,
            estimatedDurationMinutes);

    [Fact]
    public void Constructor_WithValidCommand_CreatesRouteInDraftStatus()
    {
        // Arrange
        var command = BuildCommand();

        // Act
        var route = new Route(command);

        // Assert
        Assert.Equal(RouteStatus.Draft, route.Status);
    }

    [Fact]
    public void Constructor_WithValidCommand_LeavesRouteDisabled()
    {
        // Arrange
        var command = BuildCommand();

        // Act
        var route = new Route(command);

        // Assert
        Assert.False(route.IsEnabled);
    }

    [Fact]
    public void Constructor_WithValidCommand_AssignsProvidedValues()
    {
        // Arrange
        var command = BuildCommand();

        // Act
        var route = new Route(command);

        // Assert
        Assert.Equal(command.AgencyId, route.AgencyId);
        Assert.Equal(command.Name, route.Name);
        Assert.Equal(command.Origin, route.Origin);
        Assert.Equal(command.Destination, route.Destination);
        Assert.Equal(command.DistanceKm, route.DistanceKm);
        Assert.Equal(command.ElevationGainMeters, route.ElevationGainMeters);
        Assert.Equal(command.Difficulty, route.Difficulty);
        Assert.Equal(command.EstimatedDurationMinutes, route.EstimatedDurationMinutes);
    }

    [Fact]
    public void Constructor_WithSurroundingWhitespace_TrimsTextValues()
    {
        // Arrange
        var command = BuildCommand(
            name: "  Huayhuash Alpine Circuit  ",
            origin: "  Llamac  ",
            destination: "  Cuartelwain  ");

        // Act
        var route = new Route(command);

        // Assert
        Assert.Equal("Huayhuash Alpine Circuit", route.Name);
        Assert.Equal("Llamac", route.Origin);
        Assert.Equal("Cuartelwain", route.Destination);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveAgencyId_ThrowsArgumentException(int agencyId)
    {
        // Arrange
        var command = BuildCommand(agencyId: agencyId);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Route(command));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_ThrowsArgumentException(string? name)
    {
        // Arrange
        var command = BuildCommand(name: name);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Route(command));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrigin_ThrowsArgumentException(string? origin)
    {
        // Arrange
        var command = BuildCommand(origin: origin);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Route(command));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyDestination_ThrowsArgumentException(string? destination)
    {
        // Arrange
        var command = BuildCommand(destination: destination);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Route(command));
    }

    [Fact]
    public void Constructor_WithNameLongerThanMaximumLength_ThrowsArgumentException()
    {
        // Arrange
        var command = BuildCommand(name: new string('a', 121));

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Route(command));
    }

    [Fact]
    public void Constructor_WithNameAtMaximumLength_CreatesRoute()
    {
        // Arrange
        var name = new string('a', 120);
        var command = BuildCommand(name: name);

        // Act
        var route = new Route(command);

        // Assert
        Assert.Equal(name, route.Name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    public void Constructor_WithNonPositiveDistance_ThrowsArgumentException(double distanceKm)
    {
        // Arrange
        var command = BuildCommand(distanceKm: (decimal)distanceKm);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Route(command));
    }

    [Fact]
    public void Constructor_WithNegativeElevationGain_ThrowsArgumentException()
    {
        // Arrange
        var command = BuildCommand(elevationGainMeters: -1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Route(command));
    }

    [Fact]
    public void Constructor_WithZeroElevationGain_CreatesRoute()
    {
        // Arrange
        var command = BuildCommand(elevationGainMeters: 0);

        // Act
        var route = new Route(command);

        // Assert
        Assert.Equal(0, route.ElevationGainMeters);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public void Constructor_WithNonPositiveEstimatedDuration_ThrowsArgumentException(int minutes)
    {
        // Arrange
        var command = BuildCommand(estimatedDurationMinutes: minutes);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Route(command));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Enable_WithoutCheckpoints_ThrowsInvalidOperationException(int checkpointCount)
    {
        // Arrange
        var route = new Route(BuildCommand());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => route.Enable(checkpointCount));
    }

    [Fact]
    public void Enable_WithoutCheckpoints_KeepsRouteInDraftStatus()
    {
        // Arrange
        var route = new Route(BuildCommand());

        // Act
        Assert.Throws<InvalidOperationException>(() => route.Enable(0));

        // Assert
        Assert.Equal(RouteStatus.Draft, route.Status);
        Assert.False(route.IsEnabled);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Enable_WithAtLeastOneCheckpoint_SetsStatusToEnabled(int checkpointCount)
    {
        // Arrange
        var route = new Route(BuildCommand());

        // Act
        route.Enable(checkpointCount);

        // Assert
        Assert.Equal(RouteStatus.Enabled, route.Status);
        Assert.True(route.IsEnabled);
    }
}

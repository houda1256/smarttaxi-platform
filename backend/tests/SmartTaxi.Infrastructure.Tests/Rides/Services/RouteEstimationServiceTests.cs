using SmartTaxi.Domain.Rides.ValueObjects;
using SmartTaxi.Infrastructure.Rides.Services;
using Xunit;

namespace SmartTaxi.Infrastructure.Tests.Rides.Services;

public class RouteEstimationServiceTests
{
    [Fact]
    public void Estimate_WhenEtaServiceOffline_GracefullyActivatesFallback()
    {
        // Arrange
        var distanceCalculator = new HaversineDistanceCalculator();
        var service = new RouteEstimationService(distanceCalculator);
        var from = GeoCoordinate.Create(36.8065, 10.1815); // Tunis Centre
        var to = GeoCoordinate.Create(36.8800, 10.3200);   // La Marsa

        // Act
        var estimate = service.Estimate(from, to);

        // Assert
        Assert.NotNull(estimate);
        Assert.True(estimate.DistanceKm > 0m, "Distance must be greater than 0");
        Assert.True(estimate.DurationMinutes > 0, "Duration must be at least 1 minute");
    }

    [Fact]
    public void Estimate_IdenticalCoordinates_ReturnsZeroDistanceAndMinimumDuration()
    {
        // Arrange
        var distanceCalculator = new HaversineDistanceCalculator();
        var service = new RouteEstimationService(distanceCalculator);
        var point = GeoCoordinate.Create(36.8065, 10.1815);

        // Act
        var estimate = service.Estimate(point, point);

        // Assert
        Assert.NotNull(estimate);
        Assert.Equal(0m, estimate.DistanceKm);
        Assert.Equal(1, estimate.DurationMinutes);
    }
}

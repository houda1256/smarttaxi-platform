using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Domain.Tests.Rides.Entities;

public class RideLocationPointTests
{
    [Fact]
    public void Create_Succeeds_AndCopiesCoordinateFields()
    {
        var location = GeoCoordinate.Create(36.8065, 10.1815);
        var point = new RideLocationPoint(Guid.NewGuid(), Guid.NewGuid(), location, speed: 40, heading: 90, accuracy: 5, DateTime.UtcNow);

        Assert.Equal(location.Latitude, point.Latitude);
        Assert.Equal(location.Longitude, point.Longitude);
        Assert.Equal(40, point.Speed);
    }
}

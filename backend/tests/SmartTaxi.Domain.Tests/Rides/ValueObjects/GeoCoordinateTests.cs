using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Domain.Tests.Rides.ValueObjects;

public class GeoCoordinateTests
{
    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    public void TryCreate_WithOutOfRangeValues_ReturnsFalse(double latitude, double longitude)
    {
        var result = GeoCoordinate.TryCreate(latitude, longitude, out var coordinate, out var error);

        Assert.False(result);
        Assert.Null(coordinate);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryCreate_WithValidValues_Succeeds()
    {
        var result = GeoCoordinate.TryCreate(36.8065, 10.1815, out var coordinate, out var error);

        Assert.True(result);
        Assert.NotNull(coordinate);
        Assert.Null(error);
    }

    [Fact]
    public void Equality_WithSameCoordinates_AreEqual()
    {
        var a = GeoCoordinate.Create(36.8065, 10.1815);
        var b = GeoCoordinate.Create(36.8065, 10.1815);

        Assert.Equal(a, b);
    }
}

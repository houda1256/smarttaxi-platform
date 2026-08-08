using SmartTaxi.Domain.Fleet.UsageHistory.Entities;

namespace SmartTaxi.Domain.Tests.Fleet.UsageHistory.Entities;

public class VehicleUsageRecordTests
{
    [Fact]
    public void Constructor_WithValidRange_CreatesRecord()
    {
        var startedAt = DateTime.UtcNow.AddHours(-8);
        var record = new VehicleUsageRecord(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), startedAt, DateTime.UtcNow,
            1000, 1150, 12, 450.5m, 0, DateTime.UtcNow);

        Assert.Equal(150, record.MileageEnd - record.MileageStart);
        Assert.Equal(12, record.RideCount);
    }

    [Fact]
    public void Constructor_WithEndBeforeStart_Throws()
    {
        Assert.Throws<ArgumentException>(() => new VehicleUsageRecord(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddHours(-1),
            1000, 1100, 5, null, 0, DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_WithMileageEndBelowStart_Throws()
    {
        Assert.Throws<ArgumentException>(() => new VehicleUsageRecord(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddHours(-1), DateTime.UtcNow,
            1100, 1000, 5, null, 0, DateTime.UtcNow));
    }
}

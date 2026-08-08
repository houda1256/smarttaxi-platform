using SmartTaxi.Domain.Fleet.UsageHistory.Entities;

namespace SmartTaxi.Application.Fleet.UsageHistory;

public sealed record VehicleUsageRecordSummary(
    Guid Id, Guid VehicleId, Guid DriverId, Guid AssignmentId, DateTime StartedAt, DateTime EndedAt,
    int MileageStart, int MileageEnd, int RideCount, decimal? RevenueGenerated, int IncidentCount, DateTime CreatedAt)
{
    public static VehicleUsageRecordSummary FromEntity(VehicleUsageRecord record) => new(
        record.Id, record.VehicleId, record.DriverId, record.AssignmentId, record.StartedAt, record.EndedAt,
        record.MileageStart, record.MileageEnd, record.RideCount, record.RevenueGenerated, record.IncidentCount,
        record.CreatedAt);
}

using SmartTaxi.Application.Fleet.UsageHistory;

namespace SmartTaxi.API.Contracts.Fleet.UsageHistory;

public sealed record VehicleUsageRecordResponse(
    Guid Id, Guid VehicleId, Guid DriverId, Guid AssignmentId, DateTime StartedAt, DateTime EndedAt,
    int MileageStart, int MileageEnd, int RideCount, decimal? RevenueGenerated, int IncidentCount, DateTime CreatedAt)
{
    public static VehicleUsageRecordResponse FromSummary(VehicleUsageRecordSummary summary) => new(
        summary.Id, summary.VehicleId, summary.DriverId, summary.AssignmentId, summary.StartedAt, summary.EndedAt,
        summary.MileageStart, summary.MileageEnd, summary.RideCount, summary.RevenueGenerated, summary.IncidentCount,
        summary.CreatedAt);
}

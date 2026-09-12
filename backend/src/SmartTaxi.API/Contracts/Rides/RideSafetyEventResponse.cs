using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.API.Contracts.Rides;

public sealed record RideSafetyEventResponse(
    Guid Id, Guid RideId, Guid TriggeredByUserId, double Latitude, double Longitude, string Reason,
    DateTime TriggeredAt, string Status)
{
    public static RideSafetyEventResponse FromEntity(RideSafetyEvent safetyEvent) => new(
        safetyEvent.Id, safetyEvent.RideId, safetyEvent.TriggeredByUserId, safetyEvent.Latitude, safetyEvent.Longitude,
        safetyEvent.Reason, safetyEvent.TriggeredAt, safetyEvent.Status.ToString());
}

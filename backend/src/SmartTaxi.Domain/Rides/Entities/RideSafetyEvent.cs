using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Domain.Rides.Events;
using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Domain.Rides.Entities;

/// <summary>
/// The Ride module's own record of an SOS trigger. There is no Incident
/// module yet to escalate into — this entity is the complete, self-contained
/// record; a future Administration/Support module can subscribe to
/// RideSOSActivated to create a real Incident without this entity changing.
/// </summary>
public sealed class RideSafetyEvent : AggregateRoot
{
    public Guid RideId { get; private set; }
    public Guid TriggeredByUserId { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime TriggeredAt { get; private set; }
    public RideSafetyEventStatus Status { get; private set; }

    private RideSafetyEvent()
    {
    }

    private RideSafetyEvent(Guid rideId, Guid triggeredByUserId, GeoCoordinate location, string reason, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        RideId = rideId;
        TriggeredByUserId = triggeredByUserId;
        Latitude = location.Latitude;
        Longitude = location.Longitude;
        Reason = reason;
        TriggeredAt = utcNow;
        Status = RideSafetyEventStatus.Open;

        RaiseDomainEvent(new RideSOSActivated(Id, rideId, triggeredByUserId, utcNow));
    }

    public static RideSafetyEvent Trigger(Guid rideId, Guid triggeredByUserId, GeoCoordinate location, string reason, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Un motif est requis pour un signalement SOS.");
        }

        return new RideSafetyEvent(rideId, triggeredByUserId, location, reason, utcNow);
    }
}

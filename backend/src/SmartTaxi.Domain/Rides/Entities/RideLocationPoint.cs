using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Domain.Rides.Entities;

/// <summary>
/// Append-only telemetry row. Deliberately not an AggregateRoot — no
/// creation event is raised per point, since a real-time feed of these would
/// flood the (currently non-existent) event log; live updates are pushed
/// directly over SignalR instead. Retention is a documented Application-layer
/// policy (pruned to the active-ride window + a short post-completion tail),
/// not enforced here.
/// </summary>
public sealed class RideLocationPoint
{
    public Guid Id { get; private set; }
    public Guid RideId { get; private set; }
    public Guid DriverId { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public double? Speed { get; private set; }
    public double? Heading { get; private set; }
    public double? Accuracy { get; private set; }
    public DateTime RecordedAt { get; private set; }

    private RideLocationPoint()
    {
    }

    public RideLocationPoint(
        Guid rideId, Guid driverId, GeoCoordinate location, double? speed, double? heading, double? accuracy,
        DateTime recordedAt)
    {
        Id = Guid.NewGuid();
        RideId = rideId;
        DriverId = driverId;
        Latitude = location.Latitude;
        Longitude = location.Longitude;
        Speed = speed;
        Heading = heading;
        Accuracy = accuracy;
        RecordedAt = recordedAt;
    }
}

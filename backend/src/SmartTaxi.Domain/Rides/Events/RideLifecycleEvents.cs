using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Rides.Events;

/// <summary>
/// The remaining Ride lifecycle events from the master prompt's list.
/// Every one of these corresponds to an atomic repository-level status
/// transition (or a pure read/query result, for EligibleDriversFound), never
/// a domain entity mutation — so none of them are ever actually raised via
/// AggregateRoot.RaiseDomainEvent. They are defined here purely so the full
/// event catalog is documented in one place and typed for whenever a future
/// dispatcher/outbox is introduced, exactly like Fleet's transition events.
/// </summary>
public sealed record EligibleDriversFound(Guid RideId, int Count, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record DriverSelected(Guid RideId, Guid DriverId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record DriverRequestSent(Guid RideId, Guid DriverId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record DriverAcceptedRide(Guid RideId, Guid DriverId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record DriverRejectedRide(Guid RideId, Guid DriverId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record DriverResponseExpired(Guid RideId, Guid DriverId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record DriverEnRoute(Guid RideId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record DriverArrived(Guid RideId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record PassengerOnBoard(Guid RideId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record RideStarted(Guid RideId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record RideLocationUpdated(Guid RideId, double Latitude, double Longitude, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record RideCompleted(Guid RideId, decimal FinalFare, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record RideCancelled(Guid RideId, string CancelledByStatus, string? Reason, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record CustomerNoShowReported(Guid RideId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record DriverNoShowReported(Guid RideId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record SharedRideCustomerAccepted(Guid MatchId, Guid RideId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record SharedRideCustomerRejected(Guid MatchId, Guid RideId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record SharedRideDriverAccepted(Guid MatchId, Guid DriverId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record SharedRideExpired(Guid MatchId, DateTime OccurredAtUtc) : IDomainEvent;

using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.RoadsideAssistance.Events;

/// <summary>Genuinely raised (unit-testable), same convention as every other module's events — no dispatcher/outbox consumes these; INotificationDispatcher is the real integration point Application handlers call directly.</summary>
public sealed record RoadsidePartnerProfileRegistered(Guid RoadsidePartnerProfileId, Guid UserId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record RoadsideAssistanceRequested(Guid RequestId, Guid RequesterUserId, Guid VehicleId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record RoadsidePartnerSelected(Guid RequestId, Guid PartnerUserId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record RoadsidePartnerAccepted(Guid RequestId, Guid PartnerUserId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record RoadsidePartnerRejected(Guid RequestId, Guid PartnerUserId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record RoadsidePartnerArrived(Guid RequestId, Guid PartnerUserId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record RoadsideInterventionStarted(Guid RequestId, Guid VehicleId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record RoadsideInterventionCompleted(Guid RequestId, Guid VehicleId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record RoadsideAssistanceCancelled(Guid RequestId, Guid? CancelledByUserId, DateTime OccurredAtUtc) : IDomainEvent;

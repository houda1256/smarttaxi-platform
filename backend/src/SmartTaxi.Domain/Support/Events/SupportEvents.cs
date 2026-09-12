using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Domain.Support.Events;

/// <summary>
/// Genuinely raised (unit-testable), same convention as every other module's
/// Created event — no dispatcher/outbox consumes these; INotificationDispatcher
/// is the real integration point Application handlers call directly. Only
/// creation events are raised: every other lifecycle transition happens via
/// atomic repository-level ExecuteUpdateAsync calls that never load/mutate the
/// domain object, so there is no natural point to raise a domain event for
/// them (same convention as MaintenanceRequest/RoadsideAssistanceRequest).
/// </summary>
public sealed record SupportTicketCreated(Guid TicketId, Guid RequesterUserId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record SupportIncidentReported(Guid IncidentId, SupportIncidentType Type, DateTime OccurredAtUtc) : IDomainEvent;

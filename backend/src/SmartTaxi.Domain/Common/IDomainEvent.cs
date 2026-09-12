namespace SmartTaxi.Domain.Common;

/// <summary>
/// Marker interface for domain events raised by aggregates. Events are
/// collected on the aggregate (see AggregateRoot.DomainEvents) so they are
/// unit-testable; no dispatcher/outbox is wired up yet — that belongs to
/// whichever future module (Payments/Notifications) first needs to consume
/// them.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}

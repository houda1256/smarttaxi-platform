using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Domain.Notifications.Events;

/// <summary>Genuinely raised on creation/read (unit-testable) — same convention as every other module's Created event. No dispatcher/outbox consumes these yet (see IDomainEvent); Notification itself is the real integration point other modules call today (INotificationDispatcher).</summary>
public sealed record NotificationCreated(Guid NotificationId, Guid RecipientUserId, NotificationCategory Category, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record NotificationRead(Guid NotificationId, Guid RecipientUserId, DateTime OccurredAtUtc) : IDomainEvent;

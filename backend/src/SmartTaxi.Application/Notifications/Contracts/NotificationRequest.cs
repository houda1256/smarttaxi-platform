using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Notifications.Contracts;

/// <summary>
/// What a business handler hands to INotificationDispatcher — deliberately a
/// plain, Notifications-EF-free contract (Guid ids, a category, a template key,
/// a small variable dictionary) so business modules never take a dependency on
/// Notifications' persistence model. SourceType/SourceId are the idempotency
/// anchor (e.g. "Subscription"/subscription.Id) — the same business event
/// replayed for the same recipient/category never creates a duplicate
/// notification.
/// </summary>
public sealed record NotificationRequest(
    Guid RecipientUserId,
    NotificationCategory Category,
    string TemplateKey,
    IReadOnlyDictionary<string, string> Variables,
    bool IsMandatory,
    string SourceType,
    Guid SourceId);

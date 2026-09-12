using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.API.Contracts.Notifications;

/// <summary>Technical delivery history for ops monitoring — never includes recipient contact info (email/phone/device token), only ids and sanitized failure detail already stripped of provider internals (see NotificationDispatcher).</summary>
public sealed record NotificationDeliveryFailureResponse(
    Guid Id, Guid NotificationId, string Channel, int AttemptNumber, DateTime AttemptedAtUtc, string? FailureCode,
    string? FailureMessage, DateTime? NextAttemptAtUtc)
{
    public static NotificationDeliveryFailureResponse FromEntity(NotificationDeliveryAttempt attempt) => new(
        attempt.Id, attempt.NotificationId, attempt.Channel.ToString(), attempt.AttemptNumber, attempt.AttemptedAtUtc,
        attempt.FailureCode, attempt.FailureMessage, attempt.NextAttemptAtUtc);
}

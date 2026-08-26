namespace SmartTaxi.Application.Notifications.Abstractions;

/// <summary>Small, bounded, no background worker — see ProcessRetryableDeliveriesCommand for the explicit sweep that consumes NextAttemptAtUtc.</summary>
public interface INotificationRetryPolicy
{
    int MaxAttempts { get; }

    /// <summary>Null once attemptNumber has reached MaxAttempts — no infinite retry.</summary>
    DateTime? ComputeNextAttempt(int attemptNumber, DateTime utcNow);
}

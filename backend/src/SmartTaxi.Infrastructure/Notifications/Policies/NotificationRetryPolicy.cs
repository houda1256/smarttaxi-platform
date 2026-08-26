using Microsoft.Extensions.Options;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Infrastructure.Notifications.Options;

namespace SmartTaxi.Infrastructure.Notifications.Policies;

internal sealed class NotificationRetryPolicy : INotificationRetryPolicy
{
    private readonly int _retryDelayMinutes;

    public int MaxAttempts { get; }

    public NotificationRetryPolicy(IOptions<NotificationOptions> options)
    {
        MaxAttempts = options.Value.RetryMaxAttempts;
        _retryDelayMinutes = options.Value.RetryDelayMinutes;
    }

    /// <summary>Linear backoff (delay * attemptNumber) — simple and bounded; no infinite retry once MaxAttempts is reached.</summary>
    public DateTime? ComputeNextAttempt(int attemptNumber, DateTime utcNow) =>
        attemptNumber >= MaxAttempts ? null : utcNow.AddMinutes(_retryDelayMinutes * attemptNumber);
}

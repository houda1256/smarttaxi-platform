using SmartTaxi.Application.Notifications.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeNotificationRetryPolicy : INotificationRetryPolicy
{
    public int MaxAttempts { get; set; } = 3;

    public DateTime? ComputeNextAttempt(int attemptNumber, DateTime utcNow) =>
        attemptNumber >= MaxAttempts ? null : utcNow.AddMinutes(15);
}

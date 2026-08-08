using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRideServicePolicy : IRideServicePolicy
{
    public int CustomerNoShowWaitingMinutes { get; init; } = 5;
    public int RideShareTokenValidityHours { get; init; } = 12;
    public int ConversationReadOnlyAfterMinutes { get; init; } = 60;
    public TimeSpan LocationRetentionWindow { get; init; } = TimeSpan.FromHours(24);
    public int MinLocationUpdateIntervalSeconds { get; init; } = 3;
}

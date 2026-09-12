namespace SmartTaxi.Infrastructure.Rides.Options;

public sealed class RideServiceOptions
{
    public const string SectionName = "RideService";

    public int CustomerNoShowWaitingMinutes { get; init; } = 5;
    public int RideShareTokenValidityHours { get; init; } = 12;
    public int ConversationReadOnlyAfterMinutes { get; init; } = 60;
    public int LocationRetentionHours { get; init; } = 24;
    public int MinLocationUpdateIntervalSeconds { get; init; } = 3;
}

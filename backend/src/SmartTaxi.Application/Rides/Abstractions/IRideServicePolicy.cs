namespace SmartTaxi.Application.Rides.Abstractions;

/// <summary>Miscellaneous configurable Ride servicing timers that don't warrant their own dedicated policy interface.</summary>
public interface IRideServicePolicy
{
    int CustomerNoShowWaitingMinutes { get; }
    int RideShareTokenValidityHours { get; }
    int ConversationReadOnlyAfterMinutes { get; }
    TimeSpan LocationRetentionWindow { get; }
    int MinLocationUpdateIntervalSeconds { get; }
}

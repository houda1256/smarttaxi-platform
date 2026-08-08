using Microsoft.Extensions.Options;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Infrastructure.Rides.Options;

namespace SmartTaxi.Infrastructure.Rides.Policies;

internal sealed class RideServicePolicy : IRideServicePolicy
{
    public int CustomerNoShowWaitingMinutes { get; }
    public int RideShareTokenValidityHours { get; }
    public int ConversationReadOnlyAfterMinutes { get; }
    public TimeSpan LocationRetentionWindow { get; }
    public int MinLocationUpdateIntervalSeconds { get; }

    public RideServicePolicy(IOptions<RideServiceOptions> options)
    {
        var value = options.Value;
        CustomerNoShowWaitingMinutes = value.CustomerNoShowWaitingMinutes;
        RideShareTokenValidityHours = value.RideShareTokenValidityHours;
        ConversationReadOnlyAfterMinutes = value.ConversationReadOnlyAfterMinutes;
        LocationRetentionWindow = TimeSpan.FromHours(value.LocationRetentionHours);
        MinLocationUpdateIntervalSeconds = value.MinLocationUpdateIntervalSeconds;
    }
}

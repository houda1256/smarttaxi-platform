using Microsoft.Extensions.Options;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Infrastructure.Rides.Options;

namespace SmartTaxi.Infrastructure.Rides.Policies;

internal sealed class RideNegotiationPolicy : INegotiationPolicy
{
    public int MaxNegotiationRounds { get; }
    public int FareProposalExpiryMinutes { get; }

    public RideNegotiationPolicy(IOptions<RideNegotiationOptions> options)
    {
        var value = options.Value;
        MaxNegotiationRounds = value.MaxNegotiationRounds;
        FareProposalExpiryMinutes = value.FareProposalExpiryMinutes;
    }
}

namespace SmartTaxi.Application.Rides.Abstractions;

public interface INegotiationPolicy
{
    int MaxNegotiationRounds { get; }

    int FareProposalExpiryMinutes { get; }
}

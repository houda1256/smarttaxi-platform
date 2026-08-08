namespace SmartTaxi.Infrastructure.Rides.Options;

public sealed class RideNegotiationOptions
{
    public const string SectionName = "RideNegotiation";

    public int MaxNegotiationRounds { get; init; } = 5;

    public int FareProposalExpiryMinutes { get; init; } = 2;
}

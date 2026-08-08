using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeNegotiationPolicy : INegotiationPolicy
{
    public int MaxNegotiationRounds { get; init; } = 5;
    public int FareProposalExpiryMinutes { get; init; } = 2;
}

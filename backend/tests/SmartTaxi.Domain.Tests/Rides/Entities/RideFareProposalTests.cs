using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Domain.Tests.Rides.Entities;

public class RideFareProposalTests
{
    [Fact]
    public void CreateInitialProposal_Succeeds_WithRoundOneAndProposedStatus()
    {
        var proposal = RideFareProposal.CreateInitialProposal(
            Guid.NewGuid(), Guid.NewGuid(), 25m, "TND", DateTime.UtcNow, TimeSpan.FromMinutes(2));

        Assert.Equal(1, proposal.RoundNumber);
        Assert.Equal(FareProposalStatus.Proposed, proposal.Status);
    }

    [Fact]
    public void CreateCounterProposal_Succeeds_WithCounterProposedStatus()
    {
        var proposal = RideFareProposal.CreateCounterProposal(
            Guid.NewGuid(), Guid.NewGuid(), 30m, "TND", roundNumber: 2, DateTime.UtcNow, TimeSpan.FromMinutes(2));

        Assert.Equal(2, proposal.RoundNumber);
        Assert.Equal(FareProposalStatus.CounterProposed, proposal.Status);
    }

    [Fact]
    public void CreateCounterProposal_WithRoundOne_Throws()
    {
        Assert.Throws<ArgumentException>(() => RideFareProposal.CreateCounterProposal(
            Guid.NewGuid(), Guid.NewGuid(), 30m, "TND", roundNumber: 1, DateTime.UtcNow, TimeSpan.FromMinutes(2)));
    }

    [Fact]
    public void CreateInitialProposal_WithNonPositiveAmount_Throws()
    {
        Assert.Throws<ArgumentException>(() => RideFareProposal.CreateInitialProposal(
            Guid.NewGuid(), Guid.NewGuid(), 0m, "TND", DateTime.UtcNow, TimeSpan.FromMinutes(2)));
    }

    [Fact]
    public void IsExpired_AfterExpiresAt_ReturnsTrue()
    {
        var createdAt = DateTime.UtcNow;
        var proposal = RideFareProposal.CreateInitialProposal(Guid.NewGuid(), Guid.NewGuid(), 25m, "TND", createdAt, TimeSpan.FromMinutes(2));

        Assert.False(proposal.IsExpired(createdAt.AddMinutes(1)));
        Assert.True(proposal.IsExpired(createdAt.AddMinutes(3)));
    }
}

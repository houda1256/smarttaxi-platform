using SmartTaxi.Domain.Identity.Referrals.Entities;
using SmartTaxi.Domain.Identity.Referrals.Enums;
using SmartTaxi.Infrastructure.Identity.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

[Collection("SharedPostgres")]
public class ReferralRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public ReferralRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConcurrentAddAsync_ForTheSameReferee_OnlyOneSponsorWins()
    {
        var refereeId = Guid.NewGuid();
        var firstSponsor = new Referral(Guid.NewGuid(), refereeId, "SPONSOR-A", DateTime.UtcNow);
        var secondSponsor = new Referral(Guid.NewGuid(), refereeId, "SPONSOR-B", DateTime.UtcNow);

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();
        var repositoryA = new ReferralRepository(contextA);
        var repositoryB = new ReferralRepository(contextB);

        var results = await Task.WhenAll(
            repositoryA.AddAsync(firstSponsor, CancellationToken.None),
            repositoryB.AddAsync(secondSponsor, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var readRepository = new ReferralRepository(readContext);
        var winner = await readRepository.GetByRefereeUserIdAsync(refereeId, CancellationToken.None);
        Assert.NotNull(winner);
        Assert.True(winner!.ReferrerUserId == firstSponsor.ReferrerUserId || winner.ReferrerUserId == secondSponsor.ReferrerUserId);
    }

    [Fact]
    public async Task TryActivateAsync_ForPendingReferral_TransitionsToRewardEligible()
    {
        var referral = new Referral(Guid.NewGuid(), Guid.NewGuid(), "CODE", DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new ReferralRepository(writeContext).AddAsync(referral, CancellationToken.None);
        }

        await using var context = _fixture.CreateContext();
        var repository = new ReferralRepository(context);
        var activated = await repository.TryActivateAsync(referral.Id, DateTime.UtcNow, CancellationToken.None);

        Assert.True(activated);
        var reloaded = await repository.GetByIdAsync(referral.Id, CancellationToken.None);
        Assert.Equal(ReferralStatus.RewardEligible, reloaded!.Status);
    }
}

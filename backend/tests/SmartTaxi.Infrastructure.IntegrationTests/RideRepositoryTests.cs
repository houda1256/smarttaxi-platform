using Microsoft.EntityFrameworkCore;
using SmartTaxi.Domain.Rides;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Domain.Rides.ValueObjects;
using SmartTaxi.Infrastructure.Rides.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves against a real PostgreSQL database (not the in-memory Fakes used by
/// the Application test suite) that the Ride module's atomic guards and
/// unique constraints — the concurrency protections the master prompt
/// requires — actually hold under real concurrent access.
/// </summary>
[Collection("SharedPostgres")]
public class RideRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public RideRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static Ride NewRide(Guid customerId, RideType rideType = RideType.Immediate) => Ride.Create(
        customerId, rideType, "12 Avenue Habib Bourguiba", GeoCoordinate.Create(36.8, 10.18), "Aéroport Tunis-Carthage",
        GeoCoordinate.Create(36.85, 10.22), null, 1, 0, false, false, false, false, null, RidePaymentMethod.Cash, null,
        "TND", DateTime.UtcNow);

    [Fact]
    public async Task AddAndGetById_RoundTripsAllFieldsIncludingOwnedGeoCoordinates()
    {
        var ride = NewRide(Guid.NewGuid());

        await using (var writeContext = _fixture.CreateContext())
        {
            await new RideRepository(writeContext).AddAsync(ride, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new RideRepository(readContext).GetByIdAsync(ride.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(ride.RideNumber, reloaded!.RideNumber);
        Assert.Equal(ride.PickupLocation.Latitude, reloaded.PickupLocation.Latitude, precision: 6);
        Assert.Equal(ride.PickupLocation.Longitude, reloaded.PickupLocation.Longitude, precision: 6);
        Assert.Equal(ride.DestinationLocation.Latitude, reloaded.DestinationLocation.Latitude, precision: 6);
        Assert.Equal(RideStatus.Draft, reloaded.Status);
        Assert.Equal("TND", reloaded.Currency);
    }

    [Fact]
    public async Task ConcurrentTryTransitionAsync_SameFromStatus_OnlyOneSucceeds_AndWritesExactlyOneHistoryRow()
    {
        var ride = NewRide(Guid.NewGuid());

        await using (var writeContext = _fixture.CreateContext())
        {
            await new RideRepository(writeContext).AddAsync(ride, CancellationToken.None);
        }

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();
        var utcNow = DateTime.UtcNow;

        var results = await Task.WhenAll(
            new RideRepository(contextA).TryTransitionAsync(ride.Id, RideStatus.Draft, RideStatus.Searching, null, null, utcNow, CancellationToken.None),
            new RideRepository(contextB).TryTransitionAsync(ride.Id, RideStatus.Draft, RideStatus.Searching, null, null, utcNow, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new RideRepository(readContext).GetByIdAsync(ride.Id, CancellationToken.None);
        Assert.Equal(RideStatus.Searching, reloaded!.Status);

        var historyCount = await readContext.RideStatusHistories.CountAsync(h => h.RideId == ride.Id);
        Assert.Equal(1, historyCount);
    }

    [Fact]
    public async Task DriverReservationHold_ConcurrentTryAddAsync_ForSameDriver_OnlyOneSucceeds()
    {
        var driverId = Guid.NewGuid();
        var rideA = NewRide(Guid.NewGuid());
        var rideB = NewRide(Guid.NewGuid());
        var utcNow = DateTime.UtcNow;

        await using (var writeContext = _fixture.CreateContext())
        {
            var rideRepository = new RideRepository(writeContext);
            await rideRepository.AddAsync(rideA, CancellationToken.None);
            await rideRepository.AddAsync(rideB, CancellationToken.None);
        }

        var holdA = DriverReservationHold.CreateActive(rideA.Id, driverId, Guid.NewGuid(), utcNow, TimeSpan.FromSeconds(20));
        var holdB = DriverReservationHold.CreateActive(rideB.Id, driverId, Guid.NewGuid(), utcNow, TimeSpan.FromSeconds(20));

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new DriverReservationHoldRepository(contextA).TryAddAsync(holdA, CancellationToken.None),
            new DriverReservationHoldRepository(contextB).TryAddAsync(holdB, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var activeCount = await readContext.DriverReservationHolds
            .CountAsync(h => h.DriverId == driverId && h.Status == DriverReservationHoldStatus.Active);
        Assert.Equal(1, activeCount);
    }

    [Fact]
    public async Task RideRating_ConcurrentTryAddAsync_ForSameRideAndReviewer_OnlyOneSucceeds()
    {
        var ride = NewRide(Guid.NewGuid());
        var reviewerId = Guid.NewGuid();
        var reviewedUserId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        await using (var writeContext = _fixture.CreateContext())
        {
            await new RideRepository(writeContext).AddAsync(ride, CancellationToken.None);
        }

        var ratingA = RideRating.Submit(ride.Id, reviewerId, reviewedUserId, 5, "Excellent", null, utcNow);
        var ratingB = RideRating.Submit(ride.Id, reviewerId, reviewedUserId, 2, "Changed my mind", null, utcNow);

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new RideRatingRepository(contextA).TryAddAsync(ratingA, CancellationToken.None),
            new RideRatingRepository(contextB).TryAddAsync(ratingB, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var count = await readContext.RideRatings.CountAsync(r => r.RideId == ride.Id && r.ReviewerId == reviewerId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RideShareToken_DuplicateTokenHash_SecondInsertIsRejectedByUniqueIndex()
    {
        var rideA = NewRide(Guid.NewGuid());
        var rideB = NewRide(Guid.NewGuid());
        var sharedHash = $"hash-{Guid.NewGuid()}";
        var utcNow = DateTime.UtcNow;

        await using (var writeContext = _fixture.CreateContext())
        {
            var rideRepository = new RideRepository(writeContext);
            await rideRepository.AddAsync(rideA, CancellationToken.None);
            await rideRepository.AddAsync(rideB, CancellationToken.None);
        }

        var tokenA = RideShareToken.Create(rideA.Id, sharedHash, utcNow, TimeSpan.FromHours(12));
        var tokenB = RideShareToken.Create(rideB.Id, sharedHash, utcNow, TimeSpan.FromHours(12));

        await using var writeContext2 = _fixture.CreateContext();
        await new RideShareTokenRepository(writeContext2).AddAsync(tokenA, CancellationToken.None);

        await using var contextB = _fixture.CreateContext();

        await Assert.ThrowsAsync<DbUpdateException>(
            () => new RideShareTokenRepository(contextB).AddAsync(tokenB, CancellationToken.None));
    }

    [Fact]
    public async Task RideFareProposal_TryAddCounterProposalAsync_AfterPreviousAlreadyAccepted_Fails()
    {
        var ride = NewRide(Guid.NewGuid(), RideType.Negotiated);
        var customerId = ride.CustomerId;
        var utcNow = DateTime.UtcNow;

        await using (var writeContext = _fixture.CreateContext())
        {
            await new RideRepository(writeContext).AddAsync(ride, CancellationToken.None);
        }

        var initialProposal = RideFareProposal.CreateInitialProposal(ride.Id, customerId, 25m, "TND", utcNow, TimeSpan.FromMinutes(2));

        await using (var writeContext = _fixture.CreateContext())
        {
            await new RideFareProposalRepository(writeContext).AddAsync(initialProposal, CancellationToken.None);
        }

        await using (var acceptContext = _fixture.CreateContext())
        {
            var accepted = await new RideFareProposalRepository(acceptContext)
                .TryAcceptAsync(initialProposal.Id, utcNow, CancellationToken.None);
            Assert.True(accepted);
        }

        var counterProposal = RideFareProposal.CreateCounterProposal(
            ride.Id, Guid.NewGuid(), 30m, "TND", 2, utcNow, TimeSpan.FromMinutes(2));

        await using var counterContext = _fixture.CreateContext();
        var counterSucceeded = await new RideFareProposalRepository(counterContext)
            .TryAddCounterProposalAsync(counterProposal, initialProposal.Id, CancellationToken.None);

        Assert.False(counterSucceeded);

        await using var readContext = _fixture.CreateContext();
        var proposalCount = await readContext.RideFareProposals.CountAsync(p => p.RideId == ride.Id);
        Assert.Equal(1, proposalCount);
    }

    [Fact]
    public async Task SharedRideParticipant_DuplicateRideForSameMatch_IsRejectedByUniqueIndex()
    {
        var match = SharedRideMatch.Suggest(DateTime.UtcNow, TimeSpan.FromMinutes(5));
        var rideId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        await using (var writeContext = _fixture.CreateContext())
        {
            await new SharedRideMatchRepository(writeContext).AddAsync(match, CancellationToken.None);
        }

        var participantA = new SharedRideParticipant(match.Id, rideId, customerId, DateTime.UtcNow);
        var participantB = new SharedRideParticipant(match.Id, rideId, customerId, DateTime.UtcNow);

        await using (var writeContext2 = _fixture.CreateContext())
        {
            await new SharedRideParticipantRepository(writeContext2).AddAsync(participantA, CancellationToken.None);
        }

        await using var contextB = _fixture.CreateContext();

        await Assert.ThrowsAsync<DbUpdateException>(
            () => new SharedRideParticipantRepository(contextB).AddAsync(participantB, CancellationToken.None));
    }
}

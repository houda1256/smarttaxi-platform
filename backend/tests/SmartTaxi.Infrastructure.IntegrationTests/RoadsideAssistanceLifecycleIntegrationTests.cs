using Microsoft.EntityFrameworkCore;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;
using SmartTaxi.Infrastructure.Fleet.Repositories;
using SmartTaxi.Infrastructure.RoadsideAssistance.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>Proves, against real PostgreSQL, the core RoadsideAssistanceRequest lifecycle concurrency/security guarantees that don't involve cross-aggregate Fleet coordination (see RoadsideAssistanceFleetTransactionIntegrationTests for those).</summary>
[Collection("SharedPostgres")]
public class RoadsideAssistanceLifecycleIntegrationTests
{
    private readonly SharedPostgresFixture _fixture;

    public RoadsideAssistanceLifecycleIntegrationTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<Vehicle> CreateVehicleAsync(Guid ownerId)
    {
        var vehicle = Vehicle.Register(
            ownerId, null, "Toyota", "Corolla", 2022, "White", $"PLATE-{Guid.NewGuid():N}"[..12], null, 10000, FuelType.Petrol,
            TransmissionType.Manual, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);

        await using var context = _fixture.CreateContext();
        await new VehicleRepository(context).AddAsync(vehicle, CancellationToken.None);
        return vehicle;
    }

    private async Task<RoadsideAssistanceRequest> CreateRequestAsync(Guid vehicleId, Guid requesterUserId)
    {
        var request = RoadsideAssistanceRequest.Create(
            requesterUserId, RoadsideRequesterRole.TaxiOwner, vehicleId, null, RoadsideServiceType.Towing, RoadsideUrgency.High, "Panne moteur",
            36.8, 10.18, null, null, DateTime.UtcNow);

        await using var context = _fixture.CreateContext();
        await new RoadsideAssistanceRequestRepository(context).TryAddAsync(request, CancellationToken.None);
        return request;
    }

    [Fact]
    public async Task TwoConcurrentRequestsForSameVehicle_OnlyOneSucceeds()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var first = RoadsideAssistanceRequest.Create(
            vehicle.Id, RoadsideRequesterRole.TaxiOwner, vehicle.Id, null, RoadsideServiceType.Towing, RoadsideUrgency.High, "Premier problème",
            36.8, 10.18, null, null, DateTime.UtcNow);
        var second = RoadsideAssistanceRequest.Create(
            vehicle.Id, RoadsideRequesterRole.TaxiOwner, vehicle.Id, null, RoadsideServiceType.FuelDelivery, RoadsideUrgency.Low,
            "Second problème", 36.8, 10.18, null, null, DateTime.UtcNow);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new RoadsideAssistanceRequestRepository(context1).TryAddAsync(first, CancellationToken.None),
            new RoadsideAssistanceRequestRepository(context2).TryAddAsync(second, CancellationToken.None));

        Assert.Single(results, r => r);
        Assert.Single(results, r => !r);
    }

    [Fact]
    public async Task RequestForVehicle_AfterPreviousRequestCompleted_IsAllowed()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var completedRequest = await CreateRequestAsync(vehicle.Id, ownerId);

        await using (var context = _fixture.CreateContext())
        {
            await new RoadsideAssistanceRequestRepository(context).TryTransitionAsync(
                completedRequest.Id, [RoadsideRequestStatus.PartnersAvailable], RoadsideRequestStatus.Completed, null, null, 80m, null, null,
                false, DateTime.UtcNow, CancellationToken.None);
        }

        var newRequest = RoadsideAssistanceRequest.Create(
            vehicle.Id, RoadsideRequesterRole.TaxiOwner, vehicle.Id, null, RoadsideServiceType.BatteryJumpStart, RoadsideUrgency.Low,
            "Nouveau problème", 36.8, 10.18, null, null, DateTime.UtcNow);
        await using var context2 = _fixture.CreateContext();
        var added = await new RoadsideAssistanceRequestRepository(context2).TryAddAsync(newRequest, CancellationToken.None);

        Assert.True(added);
    }

    [Fact]
    public async Task TwoConcurrentPartnerSelections_OnlyOneSucceeds()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var request = await CreateRequestAsync(vehicle.Id, ownerId);
        var partnerAUserId = Guid.NewGuid();
        var partnerBUserId = Guid.NewGuid();

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new RoadsideAssistanceRequestRepository(context1).TrySelectPartnerAsync(request.Id, ownerId, partnerAUserId, DateTime.UtcNow, CancellationToken.None),
            new RoadsideAssistanceRequestRepository(context2).TrySelectPartnerAsync(request.Id, ownerId, partnerBUserId, DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r);
        Assert.Single(results, r => !r);
    }

    [Fact]
    public async Task DuplicatePartnerAccept_OnlyOneSucceeds()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var request = await CreateRequestAsync(vehicle.Id, ownerId);
        var partnerUserId = Guid.NewGuid();

        await using (var context = _fixture.CreateContext())
        {
            await new RoadsideAssistanceRequestRepository(context).TrySelectPartnerAsync(request.Id, ownerId, partnerUserId, DateTime.UtcNow, CancellationToken.None);
        }

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new RoadsideAssistanceRequestRepository(context1).TryRespondAsync(
                request.Id, partnerUserId, RoadsidePartnerResponse.Accepted, null, null, DateTime.UtcNow, CancellationToken.None),
            new RoadsideAssistanceRequestRepository(context2).TryRespondAsync(
                request.Id, partnerUserId, RoadsidePartnerResponse.Accepted, null, null, DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r);
        Assert.Single(results, r => !r);
    }

    [Fact]
    public async Task UnauthorizedPartner_CannotRespondToAnotherPartnersJob()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var request = await CreateRequestAsync(vehicle.Id, ownerId);
        var actualPartnerUserId = Guid.NewGuid();
        var impersonatingPartnerUserId = Guid.NewGuid();

        await using (var context = _fixture.CreateContext())
        {
            await new RoadsideAssistanceRequestRepository(context).TrySelectPartnerAsync(request.Id, ownerId, actualPartnerUserId, DateTime.UtcNow, CancellationToken.None);
        }

        await using var context2 = _fixture.CreateContext();
        var accepted = await new RoadsideAssistanceRequestRepository(context2).TryRespondAsync(
            request.Id, impersonatingPartnerUserId, RoadsidePartnerResponse.Accepted, null, null, DateTime.UtcNow, CancellationToken.None);

        Assert.False(accepted);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new RoadsideAssistanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(RoadsideRequestStatus.PendingPartnerResponse, reloaded!.Status);
    }

    [Fact]
    public async Task Reselection_AfterRejection_PreservesHistoryAndStartsNewCycle()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var request = await CreateRequestAsync(vehicle.Id, ownerId);
        var firstPartnerUserId = Guid.NewGuid();
        var secondPartnerUserId = Guid.NewGuid();

        await using (var context = _fixture.CreateContext())
        {
            var repository = new RoadsideAssistanceRequestRepository(context);
            await repository.TrySelectPartnerAsync(request.Id, ownerId, firstPartnerUserId, DateTime.UtcNow, CancellationToken.None);
            await repository.TryRespondAsync(
                request.Id, firstPartnerUserId, RoadsidePartnerResponse.Rejected, null, "Indisponible", DateTime.UtcNow, CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            await new RoadsideAssistanceRequestRepository(context).TryTransitionAsync(
                request.Id, [RoadsideRequestStatus.Rejected], RoadsideRequestStatus.PartnersAvailable, ownerId, null, null, null, null, true,
                DateTime.UtcNow, CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            var selected = await new RoadsideAssistanceRequestRepository(context).TrySelectPartnerAsync(
                request.Id, ownerId, secondPartnerUserId, DateTime.UtcNow, CancellationToken.None);
            Assert.True(selected);
        }

        await using var readContext = _fixture.CreateContext();
        var history = await readContext.RoadsidePartnerSelectionHistories
            .Where(h => h.RoadsideAssistanceRequestId == request.Id).OrderBy(h => h.CycleNumber).ToListAsync(CancellationToken.None);

        Assert.Equal(2, history.Count);
        Assert.Equal(RoadsidePartnerResponse.Rejected, history[0].Response);
        Assert.Equal("Indisponible", history[0].RejectionReason);
        Assert.Null(history[1].Response);
        Assert.Equal(secondPartnerUserId, history[1].SelectedPartnerUserId);
    }
}

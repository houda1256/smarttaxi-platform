using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.RoadsideAssistance.Contracts;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;
using SmartTaxi.Infrastructure.Fleet.Repositories;
using SmartTaxi.Infrastructure.RoadsideAssistance.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves, against real PostgreSQL, the approved-plan mandatory atomicity
/// design for the transitions that coordinate RoadsideAssistanceRequest with
/// Fleet's Vehicle.OperationalStatus — no compensation-based design, no
/// window where a vehicle can be left UnderRoadsideAssistance with no
/// corresponding job, no window where a completed job leaves the vehicle
/// permanently unreleased, and non-immobilizing service types never touch
/// Fleet at all.
/// </summary>
[Collection("SharedPostgres")]
public class RoadsideAssistanceFleetTransactionIntegrationTests
{
    private readonly SharedPostgresFixture _fixture;

    public RoadsideAssistanceFleetTransactionIntegrationTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<Vehicle> CreateActiveVehicleAsync(Guid ownerId)
    {
        var vehicle = Vehicle.Register(
            ownerId, null, "Toyota", "Corolla", 2022, "White", $"PLATE-{Guid.NewGuid():N}"[..12], null, 10000, FuelType.Petrol,
            TransmissionType.Manual, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);

        await using (var context = _fixture.CreateContext())
        {
            await new VehicleRepository(context).AddAsync(vehicle, CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            await new VehicleRepository(context).TryApproveAsync(vehicle.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
        }

        return vehicle;
    }

    private async Task<(RoadsideAssistanceRequest Request, Guid PartnerUserId)> CreateRequestAtPartnerArrivedAsync(
        Guid vehicleId, Guid ownerId, RoadsideServiceType serviceType)
    {
        var partnerUserId = Guid.NewGuid();
        var request = RoadsideAssistanceRequest.Create(
            ownerId, RoadsideRequesterRole.TaxiOwner, vehicleId, null, serviceType, RoadsideUrgency.High, "Panne", 36.8, 10.18, null, null,
            DateTime.UtcNow);

        await using (var context = _fixture.CreateContext())
        {
            var repository = new RoadsideAssistanceRequestRepository(context);
            await repository.TryAddAsync(request, CancellationToken.None);
            await repository.TrySelectPartnerAsync(request.Id, ownerId, partnerUserId, DateTime.UtcNow, CancellationToken.None);
            await repository.TryRespondAsync(
                request.Id, partnerUserId, RoadsidePartnerResponse.Accepted, null, null, DateTime.UtcNow, CancellationToken.None);
            await repository.TryTransitionAsync(
                request.Id, [RoadsideRequestStatus.Accepted], RoadsideRequestStatus.PartnerOnTheWay, null, partnerUserId, null, null, null,
                false, DateTime.UtcNow, CancellationToken.None);
            await repository.TryTransitionAsync(
                request.Id, [RoadsideRequestStatus.PartnerOnTheWay], RoadsideRequestStatus.PartnerArrived, null, partnerUserId, null, null,
                null, false, DateTime.UtcNow, CancellationToken.None);
        }

        return (request, partnerUserId);
    }

    // ---- Start atomicity ----

    [Fact]
    public async Task Start_ImmobilizingServiceType_BothTransitionTogether()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        var (request, partnerUserId) = await CreateRequestAtPartnerArrivedAsync(vehicle.Id, ownerId, RoadsideServiceType.Towing);

        await using var context = _fixture.CreateContext();
        var vehicleRepository = new VehicleRepository(context);
        var requestRepository = new RoadsideAssistanceRequestRepository(context);
        var workStartRepository = new RoadsideWorkStartRepository(context, requestRepository, vehicleRepository);

        var outcome = await workStartRepository.TryStartAsync(request.Id, partnerUserId, vehicle.Id, DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(RoadsideWorkStartResult.Started, outcome);

        await using var readContext = _fixture.CreateContext();
        var reloadedRequest = await new RoadsideAssistanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        var reloadedVehicle = await new VehicleRepository(readContext).GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(RoadsideRequestStatus.InProgress, reloadedRequest!.Status);
        Assert.Equal(VehicleOperationalStatus.UnderRoadsideAssistance, reloadedVehicle!.OperationalStatus);
    }

    [Fact]
    public async Task Start_VehicleAlreadySuspended_RollsBackTheWholeTransaction()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);

        await using (var context = _fixture.CreateContext())
        {
            await new VehicleRepository(context).TrySuspendAsync(vehicle.Id, DateTime.UtcNow, CancellationToken.None);
        }

        var (request, partnerUserId) = await CreateRequestAtPartnerArrivedAsync(vehicle.Id, ownerId, RoadsideServiceType.Towing);

        await using var context2 = _fixture.CreateContext();
        var workStartRepository = new RoadsideWorkStartRepository(
            context2, new RoadsideAssistanceRequestRepository(context2), new VehicleRepository(context2));

        var outcome = await workStartRepository.TryStartAsync(request.Id, partnerUserId, vehicle.Id, DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(RoadsideWorkStartResult.VehicleNotEligible, outcome);

        await using var readContext = _fixture.CreateContext();
        var reloadedRequest = await new RoadsideAssistanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        var reloadedVehicle = await new VehicleRepository(readContext).GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(RoadsideRequestStatus.PartnerArrived, reloadedRequest!.Status); // NOT stranded in InProgress
        Assert.Equal(VehicleOperationalStatus.Suspended, reloadedVehicle!.OperationalStatus);
    }

    [Fact]
    public async Task Start_TwoConcurrentAttempts_OnlyOneSucceeds()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        var (request, partnerUserId) = await CreateRequestAtPartnerArrivedAsync(vehicle.Id, ownerId, RoadsideServiceType.Towing);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var repo1 = new RoadsideWorkStartRepository(context1, new RoadsideAssistanceRequestRepository(context1), new VehicleRepository(context1));
        var repo2 = new RoadsideWorkStartRepository(context2, new RoadsideAssistanceRequestRepository(context2), new VehicleRepository(context2));

        var results = await Task.WhenAll(
            repo1.TryStartAsync(request.Id, partnerUserId, vehicle.Id, DateTime.UtcNow, CancellationToken.None),
            repo2.TryStartAsync(request.Id, partnerUserId, vehicle.Id, DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r == RoadsideWorkStartResult.Started);

        await using var readContext = _fixture.CreateContext();
        var reloadedVehicle = await new VehicleRepository(readContext).GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(VehicleOperationalStatus.UnderRoadsideAssistance, reloadedVehicle!.OperationalStatus); // never double-transitioned
    }

    [Fact]
    public async Task Start_NonImmobilizingServiceType_NeverTouchesFleet()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        var (request, partnerUserId) = await CreateRequestAtPartnerArrivedAsync(vehicle.Id, ownerId, RoadsideServiceType.BatteryJumpStart);

        await using var context = _fixture.CreateContext();
        var requestRepository = new RoadsideAssistanceRequestRepository(context);
        var transitioned = await requestRepository.TryTransitionAsync(
            request.Id, [RoadsideRequestStatus.PartnerArrived], RoadsideRequestStatus.InProgress, null, partnerUserId, null, null, null, false,
            DateTime.UtcNow, CancellationToken.None);

        Assert.True(transitioned);

        await using var readContext = _fixture.CreateContext();
        var reloadedVehicle = await new VehicleRepository(readContext).GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(VehicleOperationalStatus.Active, reloadedVehicle!.OperationalStatus); // Fleet never touched
    }

    // ---- Completion atomicity ----

    private async Task<(RoadsideAssistanceRequest Request, Vehicle Vehicle, Guid PartnerUserId)> CreateRequestInProgressAsync(RoadsideServiceType serviceType)
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        var (request, partnerUserId) = await CreateRequestAtPartnerArrivedAsync(vehicle.Id, ownerId, serviceType);

        await using var context = _fixture.CreateContext();
        var workStartRepository = new RoadsideWorkStartRepository(
            context, new RoadsideAssistanceRequestRepository(context), new VehicleRepository(context));
        await workStartRepository.TryStartAsync(request.Id, partnerUserId, vehicle.Id, DateTime.UtcNow, CancellationToken.None);

        return (request, vehicle, partnerUserId);
    }

    [Fact]
    public async Task Complete_ImmobilizingServiceType_CommitsRequestAndReleasesVehicleTogether()
    {
        var (request, vehicle, partnerUserId) = await CreateRequestInProgressAsync(RoadsideServiceType.Towing);

        await using var context = _fixture.CreateContext();
        var completionRepository = new RoadsideCompletionRepository(
            context, new RoadsideAssistanceRequestRepository(context), new VehicleRepository(context));

        var outcome = await completionRepository.TryCompleteAsync(request.Id, partnerUserId, vehicle.Id, 80m, DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(RoadsideCompletionResult.Completed, outcome);

        await using var readContext = _fixture.CreateContext();
        var reloadedRequest = await new RoadsideAssistanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        var reloadedVehicle = await new VehicleRepository(readContext).GetByIdAsync(vehicle.Id, CancellationToken.None);

        Assert.Equal(RoadsideRequestStatus.Completed, reloadedRequest!.Status);
        Assert.Equal(80m, reloadedRequest.FinalCost);
        Assert.Equal(VehicleOperationalStatus.Active, reloadedVehicle!.OperationalStatus);
    }

    [Fact]
    public async Task Complete_VehicleIndependentlySuspended_StillCompletesWithoutReactivating()
    {
        var (request, vehicle, partnerUserId) = await CreateRequestInProgressAsync(RoadsideServiceType.Towing);

        // Simulated independent admin action while the job is still open.
        await using (var context = _fixture.CreateContext())
        {
            await context.Vehicles.Where(v => v.Id == vehicle.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(v => v.OperationalStatus, VehicleOperationalStatus.Suspended), CancellationToken.None);
        }

        await using var context2 = _fixture.CreateContext();
        var completionRepository = new RoadsideCompletionRepository(
            context2, new RoadsideAssistanceRequestRepository(context2), new VehicleRepository(context2));

        var outcome = await completionRepository.TryCompleteAsync(request.Id, partnerUserId, vehicle.Id, 80m, DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(RoadsideCompletionResult.Completed, outcome); // succeeds regardless

        await using var readContext = _fixture.CreateContext();
        var reloadedRequest = await new RoadsideAssistanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        var reloadedVehicle = await new VehicleRepository(readContext).GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(RoadsideRequestStatus.Completed, reloadedRequest!.Status);
        Assert.Equal(VehicleOperationalStatus.Suspended, reloadedVehicle!.OperationalStatus); // never silently reactivated
    }

    [Fact]
    public async Task Complete_TwoConcurrentAttempts_OnlyOneSucceeds()
    {
        var (request, vehicle, partnerUserId) = await CreateRequestInProgressAsync(RoadsideServiceType.Towing);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var repo1 = new RoadsideCompletionRepository(context1, new RoadsideAssistanceRequestRepository(context1), new VehicleRepository(context1));
        var repo2 = new RoadsideCompletionRepository(context2, new RoadsideAssistanceRequestRepository(context2), new VehicleRepository(context2));

        var results = await Task.WhenAll(
            repo1.TryCompleteAsync(request.Id, partnerUserId, vehicle.Id, 80m, DateTime.UtcNow, CancellationToken.None),
            repo2.TryCompleteAsync(request.Id, partnerUserId, vehicle.Id, 90m, DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r == RoadsideCompletionResult.Completed);
    }

    // ---- Force-cancel ----

    [Fact]
    public async Task ForceCancel_FromInProgressImmobilizing_ReleasesVehicle()
    {
        var (request, vehicle, _) = await CreateRequestInProgressAsync(RoadsideServiceType.Towing);

        await using var context = _fixture.CreateContext();
        var forceCancelRepository = new RoadsideForceCancelRepository(
            context, new RoadsideAssistanceRequestRepository(context), new VehicleRepository(context));

        var succeeded = await forceCancelRepository.TryForceCancelAsync(request.Id, Guid.NewGuid(), "Litige", DateTime.UtcNow, CancellationToken.None);

        Assert.True(succeeded);

        await using var readContext = _fixture.CreateContext();
        var reloadedRequest = await new RoadsideAssistanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        var reloadedVehicle = await new VehicleRepository(readContext).GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(RoadsideRequestStatus.Cancelled, reloadedRequest!.Status);
        Assert.Equal(VehicleOperationalStatus.Active, reloadedVehicle!.OperationalStatus);
    }

    [Fact]
    public async Task ForceCancelVsCompletion_ConcurrentRace_OnlyOneWins()
    {
        var (request, vehicle, partnerUserId) = await CreateRequestInProgressAsync(RoadsideServiceType.Towing);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var completionRepository = new RoadsideCompletionRepository(context1, new RoadsideAssistanceRequestRepository(context1), new VehicleRepository(context1));
        var forceCancelRepository = new RoadsideForceCancelRepository(context2, new RoadsideAssistanceRequestRepository(context2), new VehicleRepository(context2));

        var completionTask = completionRepository.TryCompleteAsync(request.Id, partnerUserId, vehicle.Id, 80m, DateTime.UtcNow, CancellationToken.None);
        var cancelTask = forceCancelRepository.TryForceCancelAsync(request.Id, Guid.NewGuid(), "Litige", DateTime.UtcNow, CancellationToken.None);

        await Task.WhenAll(completionTask, cancelTask);
        var completionOutcome = await completionTask;
        var cancelOutcome = await cancelTask;

        await using var readContext = _fixture.CreateContext();
        var reloadedRequest = await new RoadsideAssistanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);

        // Exactly one of the two terminal outcomes won — never both, never neither.
        Assert.True(reloadedRequest!.Status is RoadsideRequestStatus.Completed or RoadsideRequestStatus.Cancelled);
        var bothWon = completionOutcome == RoadsideCompletionResult.Completed && cancelOutcome;
        Assert.False(bothWon);
    }
}

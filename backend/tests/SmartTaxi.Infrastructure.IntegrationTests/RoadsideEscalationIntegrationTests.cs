using SmartTaxi.Application.RoadsideAssistance.Contracts;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;
using SmartTaxi.Infrastructure.Fleet.Repositories;
using SmartTaxi.Infrastructure.Maintenance.Repositories;
using SmartTaxi.Infrastructure.RoadsideAssistance.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves, against real PostgreSQL, the approved-plan (Q4) escalation
/// idempotency guarantee: one transaction guards
/// RoadsideAssistanceRequest.EscalatedMaintenanceRequestId IS NULL AND
/// Status == Completed, creates the MaintenanceRequest via Module 9's own
/// public seam, and sets the reference — a retry never creates a second
/// MaintenanceRequest, and Maintenance's own one-active-request-per-vehicle
/// constraint rolls the whole escalation back cleanly.
/// </summary>
[Collection("SharedPostgres")]
public class RoadsideEscalationIntegrationTests
{
    private readonly SharedPostgresFixture _fixture;

    public RoadsideEscalationIntegrationTests(SharedPostgresFixture fixture)
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

    private async Task<RoadsideAssistanceRequest> CreateCompletedRequestAsync(Guid ownerId, Guid vehicleId)
    {
        var request = RoadsideAssistanceRequest.Create(
            ownerId, RoadsideRequesterRole.TaxiOwner, vehicleId, null, RoadsideServiceType.Towing, RoadsideUrgency.High, "Panne", 36.8, 10.18,
            null, null, DateTime.UtcNow);

        await using var context = _fixture.CreateContext();
        var repository = new RoadsideAssistanceRequestRepository(context);
        await repository.TryAddAsync(request, CancellationToken.None);
        await repository.TryTransitionAsync(
            request.Id, [RoadsideRequestStatus.PartnersAvailable], RoadsideRequestStatus.Completed, null, null, 150m, null, null, false,
            DateTime.UtcNow, CancellationToken.None);
        return request;
    }

    [Fact]
    public async Task Escalate_EligibleRequest_CreatesMaintenanceRequestAndSetsReference()
    {
        var ownerId = Guid.NewGuid();
        var garageUserId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var request = await CreateCompletedRequestAsync(ownerId, vehicle.Id);

        await using var context = _fixture.CreateContext();
        var escalationRepository = new RoadsideEscalationRepository(context, new MaintenanceRequestRepository(context));

        var result = await escalationRepository.TryEscalateAsync(
            request.Id, ownerId, garageUserId, "Transféré depuis assistance routière", DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(RoadsideEscalationOutcome.Escalated, result.Outcome);

        await using var readContext = _fixture.CreateContext();
        var reloadedRequest = await new RoadsideAssistanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(result.MaintenanceRequestId, reloadedRequest!.EscalatedMaintenanceRequestId);

        var maintenanceRequest = await new MaintenanceRequestRepository(readContext).GetByIdAsync(result.MaintenanceRequestId!.Value, CancellationToken.None);
        Assert.NotNull(maintenanceRequest);
        Assert.Equal(ownerId, maintenanceRequest!.OwnerUserId);
    }

    [Fact]
    public async Task Escalate_TwoConcurrentAttempts_CreatesExactlyOneMaintenanceRequest()
    {
        var ownerId = Guid.NewGuid();
        var garageUserId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var request = await CreateCompletedRequestAsync(ownerId, vehicle.Id);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var repo1 = new RoadsideEscalationRepository(context1, new MaintenanceRequestRepository(context1));
        var repo2 = new RoadsideEscalationRepository(context2, new MaintenanceRequestRepository(context2));

        var results = await Task.WhenAll(
            repo1.TryEscalateAsync(request.Id, ownerId, garageUserId, "desc", DateTime.UtcNow, CancellationToken.None),
            repo2.TryEscalateAsync(request.Id, ownerId, garageUserId, "desc", DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r.Outcome == RoadsideEscalationOutcome.Escalated);

        await using var readContext = _fixture.CreateContext();
        var maintenanceRequests = await new MaintenanceRequestRepository(readContext).GetForVehicleAsync(vehicle.Id, CancellationToken.None);
        Assert.Single(maintenanceRequests); // never double-created
    }

    [Fact]
    public async Task Escalate_RetryAfterSuccess_IsIdempotentAndReturnsSameMaintenanceRequestId()
    {
        var ownerId = Guid.NewGuid();
        var garageUserId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var request = await CreateCompletedRequestAsync(ownerId, vehicle.Id);

        await using var context1 = _fixture.CreateContext();
        var first = await new RoadsideEscalationRepository(context1, new MaintenanceRequestRepository(context1)).TryEscalateAsync(
            request.Id, ownerId, garageUserId, "desc", DateTime.UtcNow, CancellationToken.None);

        await using var context2 = _fixture.CreateContext();
        var second = await new RoadsideEscalationRepository(context2, new MaintenanceRequestRepository(context2)).TryEscalateAsync(
            request.Id, ownerId, garageUserId, "desc", DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(RoadsideEscalationOutcome.Escalated, first.Outcome);
        Assert.Equal(RoadsideEscalationOutcome.AlreadyEscalated, second.Outcome);
        Assert.Equal(first.MaintenanceRequestId, second.MaintenanceRequestId);

        await using var readContext = _fixture.CreateContext();
        var maintenanceRequests = await new MaintenanceRequestRepository(readContext).GetForVehicleAsync(vehicle.Id, CancellationToken.None);
        Assert.Single(maintenanceRequests);
    }

    [Fact]
    public async Task Escalate_VehicleAlreadyHasActiveMaintenanceRequest_RollsBackAndReturnsConflict()
    {
        var ownerId = Guid.NewGuid();
        var garageUserId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);

        var existingMaintenanceRequest = MaintenanceRequest.Create(vehicle.Id, ownerId, Guid.NewGuid(), "Entretien prévu", DateTime.UtcNow);
        await using (var context = _fixture.CreateContext())
        {
            await new MaintenanceRequestRepository(context).TryAddAsync(existingMaintenanceRequest, CancellationToken.None);
        }

        var request = await CreateCompletedRequestAsync(ownerId, vehicle.Id);

        await using var escalationContext = _fixture.CreateContext();
        var result = await new RoadsideEscalationRepository(escalationContext, new MaintenanceRequestRepository(escalationContext)).TryEscalateAsync(
            request.Id, ownerId, garageUserId, "desc", DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(RoadsideEscalationOutcome.MaintenanceConflict, result.Outcome);

        await using var readContext = _fixture.CreateContext();
        var reloadedRequest = await new RoadsideAssistanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Null(reloadedRequest!.EscalatedMaintenanceRequestId);

        var maintenanceRequests = await new MaintenanceRequestRepository(readContext).GetForVehicleAsync(vehicle.Id, CancellationToken.None);
        Assert.Single(maintenanceRequests); // still only the pre-existing one
    }
}

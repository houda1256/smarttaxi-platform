using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.Enums;
using SmartTaxi.Infrastructure.Fleet.Repositories;
using SmartTaxi.Infrastructure.Maintenance.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>Proves, against real PostgreSQL, the core MaintenanceRequest lifecycle concurrency/security guarantees that don't involve cross-aggregate Fleet coordination (see MaintenanceFleetTransactionIntegrationTests for those).</summary>
[Collection("SharedPostgres")]
public class MaintenanceLifecycleIntegrationTests
{
    private readonly SharedPostgresFixture _fixture;

    public MaintenanceLifecycleIntegrationTests(SharedPostgresFixture fixture)
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

    private async Task<MaintenanceRequest> CreateRequestAsync(Guid vehicleId, Guid ownerId, Guid garageUserId)
    {
        var request = MaintenanceRequest.Create(vehicleId, ownerId, garageUserId, "Bruit suspect", DateTime.UtcNow);
        await using var context = _fixture.CreateContext();
        await new MaintenanceRequestRepository(context).TryAddAsync(request, CancellationToken.None);
        return request;
    }

    [Fact]
    public async Task TwoRequestsForTheSameVehicle_OnlyOneSucceeds()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var first = MaintenanceRequest.Create(vehicle.Id, ownerId, Guid.NewGuid(), "Premier problème", DateTime.UtcNow);
        var second = MaintenanceRequest.Create(vehicle.Id, ownerId, Guid.NewGuid(), "Second problème", DateTime.UtcNow);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new MaintenanceRequestRepository(context1).TryAddAsync(first, CancellationToken.None),
            new MaintenanceRequestRepository(context2).TryAddAsync(second, CancellationToken.None));

        Assert.Single(results, r => r);
        Assert.Single(results, r => !r);

        await using var readContext = _fixture.CreateContext();
        var forVehicle = await new MaintenanceRequestRepository(readContext).GetForVehicleAsync(vehicle.Id, CancellationToken.None);
        Assert.Single(forVehicle);
    }

    [Fact]
    public async Task RequestForVehicle_AfterPreviousRequestCompleted_IsAllowed()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var completedRequest = await CreateRequestAsync(vehicle.Id, ownerId, Guid.NewGuid());

        await using (var context = _fixture.CreateContext())
        {
            await new MaintenanceRequestRepository(context).TryTransitionAsync(
                completedRequest.Id, [MaintenanceRequestStatus.PendingGarageResponse], MaintenanceRequestStatus.Completed, null, null, null,
                100m, null, null, DateTime.UtcNow, CancellationToken.None);
        }

        var newRequest = MaintenanceRequest.Create(vehicle.Id, ownerId, Guid.NewGuid(), "Nouveau problème", DateTime.UtcNow);
        await using var context2 = _fixture.CreateContext();
        var added = await new MaintenanceRequestRepository(context2).TryAddAsync(newRequest, CancellationToken.None);

        Assert.True(added);
    }

    [Fact]
    public async Task TwoConcurrentGarageAcceptances_OnlyOneSucceeds()
    {
        var garageUserId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(Guid.NewGuid());
        var request = await CreateRequestAsync(vehicle.Id, Guid.NewGuid(), garageUserId);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new MaintenanceRequestRepository(context1).TryTransitionAsync(
                request.Id, [MaintenanceRequestStatus.PendingGarageResponse], MaintenanceRequestStatus.QuotePending, garageUserId, null, null,
                null, null, null, DateTime.UtcNow, CancellationToken.None),
            new MaintenanceRequestRepository(context2).TryTransitionAsync(
                request.Id, [MaintenanceRequestStatus.PendingGarageResponse], MaintenanceRequestStatus.QuotePending, garageUserId, null, null,
                null, null, null, DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r);
        Assert.Single(results, r => !r);
    }

    [Fact]
    public async Task UnauthorizedGarage_CannotRespondToAnotherGaragesRequest()
    {
        var actualGarageUserId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(Guid.NewGuid());
        var request = await CreateRequestAsync(vehicle.Id, Guid.NewGuid(), actualGarageUserId);
        var impersonatingGarageUserId = Guid.NewGuid();

        await using var context = _fixture.CreateContext();
        var transitioned = await new MaintenanceRequestRepository(context).TryTransitionAsync(
            request.Id, [MaintenanceRequestStatus.PendingGarageResponse], MaintenanceRequestStatus.QuotePending, impersonatingGarageUserId,
            null, null, null, null, null, DateTime.UtcNow, CancellationToken.None);

        Assert.False(transitioned);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new MaintenanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(MaintenanceRequestStatus.PendingGarageResponse, reloaded!.Status);
    }
}

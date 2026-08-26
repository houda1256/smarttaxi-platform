using SmartTaxi.Application.Common;
using SmartTaxi.Application.Maintenance.Commands.StartMaintenanceWork;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.Enums;

namespace SmartTaxi.Application.Tests.Maintenance.Commands;

/// <summary>Application-layer proof of the mandatory atomicity design (§2 of the approved plan delta) using the fakes' composed in-memory transaction simulation — the real cross-table guarantee is additionally proven against Postgres in Infrastructure.IntegrationTests.</summary>
public class StartMaintenanceWorkCommandHandlerTests
{
    private readonly FakeMaintenanceRequestRepository _requestRepository = new();
    private readonly FakeVehicleRepository _vehicleRepository = new();
    private readonly FakeMaintenanceWorkStartRepository _workStartRepository;
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly StartMaintenanceWorkCommandHandler _handler;

    public StartMaintenanceWorkCommandHandlerTests()
    {
        _workStartRepository = new FakeMaintenanceWorkStartRepository(_requestRepository, _vehicleRepository);
        _handler = new StartMaintenanceWorkCommandHandler(_requestRepository, _workStartRepository, _notificationDispatcher);
    }

    private async Task<Vehicle> CreateActiveVehicleAsync(Guid ownerId)
    {
        var vehicle = Vehicle.Register(
            ownerId, null, "Toyota", "Corolla", 2022, "White", $"PLATE-{Guid.NewGuid():N}"[..12], null, 10000, FuelType.Petrol,
            TransmissionType.Manual, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);
        await _vehicleRepository.TryApproveAsync(vehicle.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
        return vehicle;
    }

    private async Task<MaintenanceRequest> CreateRequestAtVehicleReceivedAsync(Guid ownerId, Guid garageUserId, Guid vehicleId)
    {
        var request = MaintenanceRequest.Create(vehicleId, ownerId, garageUserId, "Bruit suspect", DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [MaintenanceRequestStatus.PendingGarageResponse], MaintenanceRequestStatus.QuotePending, garageUserId, null, null, null,
            null, null, DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [MaintenanceRequestStatus.QuotePending], MaintenanceRequestStatus.QuoteSubmitted, garageUserId, null, 100m, null, null,
            null, DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [MaintenanceRequestStatus.QuoteSubmitted], MaintenanceRequestStatus.QuoteAccepted, null, ownerId, null, null, null, null,
            DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [MaintenanceRequestStatus.QuoteAccepted], MaintenanceRequestStatus.VehicleReceived, garageUserId, null, null, null, null,
            null, DateTime.UtcNow, CancellationToken.None);
        return request;
    }

    [Fact]
    public async Task Handle_EligibleRequestAndActiveVehicle_BothTransitionTogether()
    {
        var ownerId = Guid.NewGuid();
        var garageUserId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        var request = await CreateRequestAtVehicleReceivedAsync(ownerId, garageUserId, vehicle.Id);

        var result = await _handler.Handle(new StartMaintenanceWorkCommand(request.Id, garageUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloadedRequest = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        var reloadedVehicle = await _vehicleRepository.GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(MaintenanceRequestStatus.InProgress, reloadedRequest!.Status);
        Assert.NotNull(reloadedRequest.WorkStartedAtUtc);
        Assert.Equal(VehicleOperationalStatus.UnderMaintenance, reloadedVehicle!.OperationalStatus);
    }

    [Fact]
    public async Task Handle_VehicleNotActive_RollsBackRequestTransitionToo()
    {
        var ownerId = Guid.NewGuid();
        var garageUserId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        await _vehicleRepository.TrySuspendAsync(vehicle.Id, DateTime.UtcNow, CancellationToken.None); // independently suspended before start
        var request = await CreateRequestAtVehicleReceivedAsync(ownerId, garageUserId, vehicle.Id);

        var result = await _handler.Handle(new StartMaintenanceWorkCommand(request.Id, garageUserId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);

        // Neither side changed — the request must still be exactly where it was, not stranded in InProgress.
        var reloadedRequest = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        var reloadedVehicle = await _vehicleRepository.GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(MaintenanceRequestStatus.VehicleReceived, reloadedRequest!.Status);
        Assert.Equal(VehicleOperationalStatus.Suspended, reloadedVehicle!.OperationalStatus);
    }

    [Fact]
    public async Task Handle_RequestNotYetAtVehicleReceived_ReturnsConflictAndNeverTouchesVehicle()
    {
        var ownerId = Guid.NewGuid();
        var garageUserId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        var request = MaintenanceRequest.Create(vehicle.Id, ownerId, garageUserId, "Bruit suspect", DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None); // still PendingGarageResponse

        var result = await _handler.Handle(new StartMaintenanceWorkCommand(request.Id, garageUserId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        var reloadedVehicle = await _vehicleRepository.GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(VehicleOperationalStatus.Active, reloadedVehicle!.OperationalStatus);
    }

    [Fact]
    public async Task Handle_AnotherGarage_ReturnsConflict()
    {
        var ownerId = Guid.NewGuid();
        var garageUserId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        var request = await CreateRequestAtVehicleReceivedAsync(ownerId, garageUserId, vehicle.Id);

        var result = await _handler.Handle(new StartMaintenanceWorkCommand(request.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}

using SmartTaxi.Application.Common;
using SmartTaxi.Application.Maintenance.Commands.ForceCancelMaintenanceRequest;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.Enums;

namespace SmartTaxi.Application.Tests.Maintenance.Commands;

public class ForceCancelMaintenanceRequestCommandHandlerTests
{
    private readonly FakeMaintenanceRequestRepository _requestRepository = new();
    private readonly FakeVehicleRepository _vehicleRepository = new();
    private readonly FakeMaintenanceForceCancelRepository _forceCancelRepository;
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly ForceCancelMaintenanceRequestCommandHandler _handler;

    public ForceCancelMaintenanceRequestCommandHandlerTests()
    {
        _forceCancelRepository = new FakeMaintenanceForceCancelRepository(_requestRepository, _vehicleRepository);
        _handler = new ForceCancelMaintenanceRequestCommandHandler(_requestRepository, _forceCancelRepository, _notificationDispatcher);
    }

    private async Task<Vehicle> CreateActiveVehicleAsync()
    {
        var vehicle = Vehicle.Register(
            Guid.NewGuid(), null, "Toyota", "Corolla", 2022, "White", $"PLATE-{Guid.NewGuid():N}"[..12], null, 10000, FuelType.Petrol,
            TransmissionType.Manual, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);
        await _vehicleRepository.TryApproveAsync(vehicle.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
        return vehicle;
    }

    [Fact]
    public async Task Handle_FromInProgress_CancelsAndReleasesVehicle()
    {
        var vehicle = await CreateActiveVehicleAsync();
        var garageUserId = Guid.NewGuid();
        var request = MaintenanceRequest.Create(vehicle.Id, Guid.NewGuid(), garageUserId, "Bruit suspect", DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [MaintenanceRequestStatus.PendingGarageResponse], MaintenanceRequestStatus.InProgress, garageUserId, null, null, null,
            null, null, DateTime.UtcNow, CancellationToken.None);
        await _vehicleRepository.TryMarkUnderMaintenanceAsync(vehicle.Id, DateTime.UtcNow, CancellationToken.None);

        var adminUserId = Guid.NewGuid();
        var result = await _handler.Handle(new ForceCancelMaintenanceRequestCommand(request.Id, adminUserId, "Litige client"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloadedRequest = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(MaintenanceRequestStatus.Cancelled, reloadedRequest!.Status);
        Assert.Equal(adminUserId, reloadedRequest.CancelledByUserId);
        Assert.Equal("Litige client", reloadedRequest.CancellationReason);

        var reloadedVehicle = await _vehicleRepository.GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(VehicleOperationalStatus.Active, reloadedVehicle!.OperationalStatus);
    }

    [Fact]
    public async Task Handle_FromEarlyState_CancelsWithoutTouchingVehicle()
    {
        var vehicle = await CreateActiveVehicleAsync();
        var request = MaintenanceRequest.Create(vehicle.Id, Guid.NewGuid(), Guid.NewGuid(), "Bruit suspect", DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None); // still PendingGarageResponse — vehicle never entered maintenance

        var result = await _handler.Handle(new ForceCancelMaintenanceRequestCommand(request.Id, Guid.NewGuid(), "Doublon"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloadedVehicle = await _vehicleRepository.GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(VehicleOperationalStatus.Active, reloadedVehicle!.OperationalStatus);
    }

    [Fact]
    public async Task Handle_FromCompleted_ReturnsConflict()
    {
        var vehicle = await CreateActiveVehicleAsync();
        var request = MaintenanceRequest.Create(vehicle.Id, Guid.NewGuid(), Guid.NewGuid(), "Bruit suspect", DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [MaintenanceRequestStatus.PendingGarageResponse], MaintenanceRequestStatus.Completed, null, null, null, 100m, null,
            null, DateTime.UtcNow, CancellationToken.None);

        var result = await _handler.Handle(new ForceCancelMaintenanceRequestCommand(request.Id, Guid.NewGuid(), "reason"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithoutReason_ReturnsValidationError()
    {
        var vehicle = await CreateActiveVehicleAsync();
        var request = MaintenanceRequest.Create(vehicle.Id, Guid.NewGuid(), Guid.NewGuid(), "Bruit suspect", DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);

        var result = await _handler.Handle(new ForceCancelMaintenanceRequestCommand(request.Id, Guid.NewGuid(), ""), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }
}

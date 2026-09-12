using SmartTaxi.Application.Common;
using SmartTaxi.Application.Maintenance.Commands.CreateMaintenanceRequest;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Application.Tests.Maintenance.Commands;

public class CreateMaintenanceRequestCommandHandlerTests
{
    private readonly FakeMaintenanceRequestRepository _requestRepository = new();
    private readonly FakeVehicleRepository _vehicleRepository = new();
    private readonly FakeGarageProfileRepository _garageProfileRepository = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly CreateMaintenanceRequestCommandHandler _handler;

    public CreateMaintenanceRequestCommandHandlerTests()
    {
        _handler = new CreateMaintenanceRequestCommandHandler(_requestRepository, _vehicleRepository, _garageProfileRepository, _notificationDispatcher);
    }

    private async Task<Vehicle> CreateVehicleAsync(Guid ownerId)
    {
        var vehicle = Vehicle.Register(
            ownerId, null, "Toyota", "Corolla", 2022, "White", $"PLATE-{Guid.NewGuid():N}"[..12], null, 10000, FuelType.Petrol,
            TransmissionType.Manual, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);
        return vehicle;
    }

    private async Task<Guid> CreateActiveGarageAsync()
    {
        var garageUserId = Guid.NewGuid();
        var garage = GarageProfile.Register(garageUserId, "Garage Central", null, "12 rue X", "Tunis", null, null, DateTime.UtcNow);
        await _garageProfileRepository.TryAddAsync(garage, CancellationToken.None);
        return garageUserId;
    }

    [Fact]
    public async Task Handle_OwnerRequestingForOwnVehicle_Succeeds()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var garageUserId = await CreateActiveGarageAsync();

        var result = await _handler.Handle(
            new CreateMaintenanceRequestCommand(ownerId, vehicle.Id, garageUserId, "Bruit suspect"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_notificationDispatcher.DispatchedRequests);
    }

    [Fact]
    public async Task Handle_VehicleOwnedByAnotherOwner_ReturnsForbidden()
    {
        var actualOwnerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(actualOwnerId);
        var garageUserId = await CreateActiveGarageAsync();
        var impersonatorId = Guid.NewGuid();

        var result = await _handler.Handle(
            new CreateMaintenanceRequestCommand(impersonatorId, vehicle.Id, garageUserId, "Bruit suspect"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Empty(_notificationDispatcher.DispatchedRequests);
    }

    [Fact]
    public async Task Handle_UnknownVehicle_ReturnsNotFound()
    {
        var garageUserId = await CreateActiveGarageAsync();

        var result = await _handler.Handle(
            new CreateMaintenanceRequestCommand(Guid.NewGuid(), Guid.NewGuid(), garageUserId, "Bruit suspect"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_UnknownGarage_ReturnsNotFound()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);

        var result = await _handler.Handle(
            new CreateMaintenanceRequestCommand(ownerId, vehicle.Id, Guid.NewGuid(), "Bruit suspect"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_VehicleAlreadyHasActiveRequest_ReturnsConflict()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var garageUserId = await CreateActiveGarageAsync();

        var first = await _handler.Handle(
            new CreateMaintenanceRequestCommand(ownerId, vehicle.Id, garageUserId, "Bruit suspect"), CancellationToken.None);
        Assert.True(first.IsSuccess);

        var second = await _handler.Handle(
            new CreateMaintenanceRequestCommand(ownerId, vehicle.Id, garageUserId, "Autre problème"), CancellationToken.None);

        Assert.False(second.IsSuccess);
        Assert.Equal(ErrorType.Conflict, second.ErrorType);
    }
}

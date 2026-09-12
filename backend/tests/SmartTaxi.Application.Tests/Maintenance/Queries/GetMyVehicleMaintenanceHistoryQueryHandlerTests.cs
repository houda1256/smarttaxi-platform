using SmartTaxi.Application.Common;
using SmartTaxi.Application.Maintenance.Queries.GetMyVehicleMaintenanceHistory;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.Application.Tests.Maintenance.Queries;

public class GetMyVehicleMaintenanceHistoryQueryHandlerTests
{
    private readonly FakeVehicleRepository _vehicleRepository = new();
    private readonly FakeMaintenanceRecordRepository _recordRepository = new();
    private readonly GetMyVehicleMaintenanceHistoryQueryHandler _handler;

    public GetMyVehicleMaintenanceHistoryQueryHandlerTests()
    {
        _handler = new GetMyVehicleMaintenanceHistoryQueryHandler(_vehicleRepository, _recordRepository);
    }

    private async Task<Vehicle> CreateVehicleAsync(Guid ownerId)
    {
        var vehicle = Vehicle.Register(
            ownerId, null, "Toyota", "Corolla", 2022, "White", $"PLATE-{Guid.NewGuid():N}"[..12], null, 10000, FuelType.Petrol,
            TransmissionType.Manual, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);
        return vehicle;
    }

    [Fact]
    public async Task Handle_OwnVehicle_Succeeds()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);

        var result = await _handler.Handle(new GetMyVehicleMaintenanceHistoryQuery(vehicle.Id, ownerId), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_AnotherOwnersVehicle_ReturnsForbidden()
    {
        var actualOwnerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(actualOwnerId);

        var result = await _handler.Handle(new GetMyVehicleMaintenanceHistoryQuery(vehicle.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Handle_UnknownVehicle_ReturnsNotFound()
    {
        var result = await _handler.Handle(new GetMyVehicleMaintenanceHistoryQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}

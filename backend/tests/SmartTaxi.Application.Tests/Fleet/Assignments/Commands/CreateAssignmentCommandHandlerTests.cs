using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Assignments.Commands.CreateAssignment;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Assignments.Enums;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.Application.Tests.Fleet.Assignments.Commands;

public class CreateAssignmentCommandHandlerTests
{
    private readonly FakeVehicleRepository _vehicleRepository = new();
    private readonly FakeDriverProfileRepository _driverRepository = new();
    private readonly FakeDriverVehicleAssignmentRepository _assignmentRepository = new();
    private readonly CreateAssignmentCommandHandler _handler;

    public CreateAssignmentCommandHandlerTests()
    {
        _handler = new CreateAssignmentCommandHandler(_vehicleRepository, _driverRepository, _assignmentRepository);
    }

    private async Task<(Guid OwnerId, Guid VehicleId, Guid DriverId)> SetupAsync()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = Vehicle.Register(
            ownerId, null, "Toyota", "Corolla", 2022, "White", "AA-123-BB", null, 0,
            FuelType.Petrol, TransmissionType.Automatic, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);

        var driver = DriverProfile.Create(Guid.NewGuid(), "LIC1", DateTime.UtcNow.AddYears(1), null, true, DateTime.UtcNow);
        await _driverRepository.AddAsync(driver, CancellationToken.None);

        return (ownerId, vehicle.Id, driver.Id);
    }

    [Fact]
    public async Task Handle_ForNonOverlappingSlot_CreatesDraftAssignment()
    {
        var (ownerId, vehicleId, driverId) = await SetupAsync();

        var result = await _handler.Handle(
            new CreateAssignmentCommand(ownerId, driverId, vehicleId, new DateOnly(2026, 1, 1), null, null, null, DaysOfWeek.All),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ForVehicleNotOwnedByRequester_ReturnsNotFound()
    {
        var (_, vehicleId, driverId) = await SetupAsync();

        var result = await _handler.Handle(
            new CreateAssignmentCommand(Guid.NewGuid(), driverId, vehicleId, new DateOnly(2026, 1, 1), null, null, null, DaysOfWeek.All),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenDriverAlreadyHasOverlappingAssignment_ReturnsConflict()
    {
        var (ownerId, vehicleId, driverId) = await SetupAsync();
        await _handler.Handle(
            new CreateAssignmentCommand(ownerId, driverId, vehicleId, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), null, null, DaysOfWeek.All),
            CancellationToken.None);

        var otherVehicle = Vehicle.Register(
            ownerId, null, "Honda", "Civic", 2023, "Black", "CC-456-DD", null, 0,
            FuelType.Diesel, TransmissionType.Manual, 4, false, true, VehicleCategory.Comfort, null, DateTime.UtcNow);
        await _vehicleRepository.AddAsync(otherVehicle, CancellationToken.None);

        var result = await _handler.Handle(
            new CreateAssignmentCommand(ownerId, driverId, otherVehicle.Id, new DateOnly(2026, 1, 15), new DateOnly(2026, 2, 15), null, null, DaysOfWeek.All),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenVehicleAlreadyHasOverlappingAssignment_ReturnsConflict()
    {
        var (ownerId, vehicleId, driverId) = await SetupAsync();
        await _handler.Handle(
            new CreateAssignmentCommand(ownerId, driverId, vehicleId, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), null, null, DaysOfWeek.All),
            CancellationToken.None);

        var otherDriver = DriverProfile.Create(Guid.NewGuid(), "LIC2", DateTime.UtcNow.AddYears(1), null, true, DateTime.UtcNow);
        await _driverRepository.AddAsync(otherDriver, CancellationToken.None);

        var result = await _handler.Handle(
            new CreateAssignmentCommand(ownerId, otherDriver.Id, vehicleId, new DateOnly(2026, 1, 15), new DateOnly(2026, 2, 15), null, null, DaysOfWeek.All),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithNonOverlappingDaysOfWeek_Succeeds()
    {
        var (ownerId, vehicleId, driverId) = await SetupAsync();
        await _handler.Handle(
            new CreateAssignmentCommand(ownerId, driverId, vehicleId, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), null, null, DaysOfWeek.Monday),
            CancellationToken.None);

        var otherDriver = DriverProfile.Create(Guid.NewGuid(), "LIC2", DateTime.UtcNow.AddYears(1), null, true, DateTime.UtcNow);
        await _driverRepository.AddAsync(otherDriver, CancellationToken.None);

        var result = await _handler.Handle(
            new CreateAssignmentCommand(ownerId, otherDriver.Id, vehicleId, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), null, null, DaysOfWeek.Tuesday),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}

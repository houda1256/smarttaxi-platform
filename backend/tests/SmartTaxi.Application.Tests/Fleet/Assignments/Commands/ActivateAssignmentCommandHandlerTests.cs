using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Assignments.Commands.ActivateAssignment;
using SmartTaxi.Application.Fleet.Assignments.Commands.ApproveAssignment;
using SmartTaxi.Application.Fleet.Assignments.Commands.CreateAssignment;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Assignments.Enums;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.Application.Tests.Fleet.Assignments.Commands;

public class ActivateAssignmentCommandHandlerTests
{
    private readonly FakeVehicleRepository _vehicleRepository = new();
    private readonly FakeDriverProfileRepository _driverRepository = new();
    private readonly FakeDriverVehicleAssignmentRepository _assignmentRepository = new();
    private readonly CreateAssignmentCommandHandler _createHandler;
    private readonly ApproveAssignmentCommandHandler _approveHandler;
    private readonly ActivateAssignmentCommandHandler _activateHandler;

    public ActivateAssignmentCommandHandlerTests()
    {
        _createHandler = new CreateAssignmentCommandHandler(_vehicleRepository, _driverRepository, _assignmentRepository);
        _approveHandler = new ApproveAssignmentCommandHandler(_assignmentRepository);
        _activateHandler = new ActivateAssignmentCommandHandler(_assignmentRepository, _driverRepository);
    }

    private async Task<(Guid OwnerId, Guid AssignmentId, Guid DriverId, Guid VehicleId)> CreateAndApproveAsync()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = Vehicle.Register(
            ownerId, null, "Toyota", "Corolla", 2022, "White", "AA-123-BB", null, 0,
            FuelType.Petrol, TransmissionType.Automatic, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);

        var driver = DriverProfile.Create(Guid.NewGuid(), "LIC1", DateTime.UtcNow.AddYears(1), null, true, DateTime.UtcNow);
        await _driverRepository.AddAsync(driver, CancellationToken.None);

        var createResult = await _createHandler.Handle(
            new CreateAssignmentCommand(ownerId, driver.Id, vehicle.Id, new DateOnly(2026, 1, 1), null, null, null, DaysOfWeek.All),
            CancellationToken.None);
        await _approveHandler.Handle(new ApproveAssignmentCommand(ownerId, createResult.Value), CancellationToken.None);

        return (ownerId, createResult.Value, driver.Id, vehicle.Id);
    }

    [Fact]
    public async Task Handle_ForPendingApprovalAssignment_ActivatesAndAssignsDriverToVehicle()
    {
        var (ownerId, assignmentId, driverId, vehicleId) = await CreateAndApproveAsync();

        var result = await _activateHandler.Handle(new ActivateAssignmentCommand(ownerId, assignmentId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var driver = await _driverRepository.GetByIdAsync(driverId, CancellationToken.None);
        Assert.Equal(vehicleId, driver!.CurrentVehicleId);
    }

    [Fact]
    public async Task Handle_ForAlreadyActiveAssignment_ReturnsConflict()
    {
        var (ownerId, assignmentId, _, _) = await CreateAndApproveAsync();
        await _activateHandler.Handle(new ActivateAssignmentCommand(ownerId, assignmentId), CancellationToken.None);

        var result = await _activateHandler.Handle(new ActivateAssignmentCommand(ownerId, assignmentId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Handle_ForDraftAssignment_ReturnsConflictBecauseNotYetApproved()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = Vehicle.Register(
            ownerId, null, "Toyota", "Corolla", 2022, "White", "AA-999-ZZ", null, 0,
            FuelType.Petrol, TransmissionType.Automatic, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);
        var driver = DriverProfile.Create(Guid.NewGuid(), "LIC2", DateTime.UtcNow.AddYears(1), null, true, DateTime.UtcNow);
        await _driverRepository.AddAsync(driver, CancellationToken.None);
        var createResult = await _createHandler.Handle(
            new CreateAssignmentCommand(ownerId, driver.Id, vehicle.Id, new DateOnly(2026, 1, 1), null, null, null, DaysOfWeek.All),
            CancellationToken.None);

        var result = await _activateHandler.Handle(new ActivateAssignmentCommand(ownerId, createResult.Value), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}

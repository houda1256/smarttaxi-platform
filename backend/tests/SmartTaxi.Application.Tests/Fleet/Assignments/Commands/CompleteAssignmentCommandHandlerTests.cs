using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Assignments.Commands.ActivateAssignment;
using SmartTaxi.Application.Fleet.Assignments.Commands.ApproveAssignment;
using SmartTaxi.Application.Fleet.Assignments.Commands.CompleteAssignment;
using SmartTaxi.Application.Fleet.Assignments.Commands.CreateAssignment;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Assignments.Enums;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.Application.Tests.Fleet.Assignments.Commands;

public class CompleteAssignmentCommandHandlerTests
{
    private readonly FakeVehicleRepository _vehicleRepository = new();
    private readonly FakeDriverProfileRepository _driverRepository = new();
    private readonly FakeDriverVehicleAssignmentRepository _assignmentRepository = new();
    private readonly FakeVehicleUsageRecordRepository _usageRecordRepository = new();
    private readonly CreateAssignmentCommandHandler _createHandler;
    private readonly ApproveAssignmentCommandHandler _approveHandler;
    private readonly ActivateAssignmentCommandHandler _activateHandler;
    private readonly CompleteAssignmentCommandHandler _completeHandler;

    public CompleteAssignmentCommandHandlerTests()
    {
        _createHandler = new CreateAssignmentCommandHandler(_vehicleRepository, _driverRepository, _assignmentRepository);
        _approveHandler = new ApproveAssignmentCommandHandler(_assignmentRepository);
        _activateHandler = new ActivateAssignmentCommandHandler(_assignmentRepository, _driverRepository);
        _completeHandler = new CompleteAssignmentCommandHandler(_assignmentRepository, _driverRepository, _usageRecordRepository);
    }

    private async Task<(Guid OwnerId, Guid AssignmentId, Guid DriverId, Guid VehicleId)> CreateActivatedAssignmentAsync()
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
        await _activateHandler.Handle(new ActivateAssignmentCommand(ownerId, createResult.Value), CancellationToken.None);

        return (ownerId, createResult.Value, driver.Id, vehicle.Id);
    }

    [Fact]
    public async Task Handle_ForActiveAssignment_WritesUsageRecordAndUnassignsDriver()
    {
        var (ownerId, assignmentId, driverId, vehicleId) = await CreateActivatedAssignmentAsync();

        var result = await _completeHandler.Handle(
            new CompleteAssignmentCommand(ownerId, assignmentId, 10000, 10250, 12, 350.50m, 0), CancellationToken.None);

        Assert.True(result.IsSuccess);

        var records = await _usageRecordRepository.GetForVehicleAsync(vehicleId, CancellationToken.None);
        var record = Assert.Single(records);
        Assert.Equal(driverId, record.DriverId);
        Assert.Equal(assignmentId, record.AssignmentId);
        Assert.Equal(10000, record.MileageStart);
        Assert.Equal(10250, record.MileageEnd);
        Assert.Equal(12, record.RideCount);

        var driver = await _driverRepository.GetByIdAsync(driverId, CancellationToken.None);
        Assert.Null(driver!.CurrentVehicleId);
    }

    [Fact]
    public async Task Handle_WithMileageEndBeforeStart_ReturnsValidationErrorAndDoesNotComplete()
    {
        var (ownerId, assignmentId, _, _) = await CreateActivatedAssignmentAsync();

        var result = await _completeHandler.Handle(
            new CompleteAssignmentCommand(ownerId, assignmentId, 500, 100), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Empty(_usageRecordRepository.Records);
    }

    [Fact]
    public async Task Handle_ForNonActiveAssignment_ReturnsConflict()
    {
        var (ownerId, assignmentId, _, _) = await CreateActivatedAssignmentAsync();
        await _completeHandler.Handle(new CompleteAssignmentCommand(ownerId, assignmentId, 0, 100), CancellationToken.None);

        var result = await _completeHandler.Handle(new CompleteAssignmentCommand(ownerId, assignmentId, 100, 200), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}

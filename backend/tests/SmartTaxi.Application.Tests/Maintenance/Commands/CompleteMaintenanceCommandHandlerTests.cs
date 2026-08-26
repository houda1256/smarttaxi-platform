using SmartTaxi.Application.Common;
using SmartTaxi.Application.Maintenance.Commands.CompleteMaintenance;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Expenses.Enums;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.Enums;

namespace SmartTaxi.Application.Tests.Maintenance.Commands;

/// <summary>Application-layer proof of the mandatory completion atomicity design (§3 of the approved plan delta), including the explicit "never reactivate an independently-Suspended vehicle" business rule. The real cross-table guarantee is additionally proven against Postgres in Infrastructure.IntegrationTests.</summary>
public class CompleteMaintenanceCommandHandlerTests
{
    private readonly FakeMaintenanceRequestRepository _requestRepository = new();
    private readonly FakeVehicleRepository _vehicleRepository = new();
    private readonly FakeMaintenanceRecordRepository _recordRepository = new();
    private readonly FakeFleetExpenseRepository _fleetExpenseRepository = new();
    private readonly FakeMaintenanceCompletionRepository _completionRepository;
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly CompleteMaintenanceCommandHandler _handler;

    public CompleteMaintenanceCommandHandlerTests()
    {
        _completionRepository = new FakeMaintenanceCompletionRepository(_requestRepository, _vehicleRepository, _recordRepository, _fleetExpenseRepository);
        _handler = new CompleteMaintenanceCommandHandler(_requestRepository, _completionRepository, _vehicleRepository, _notificationDispatcher);
    }

    private async Task<Vehicle> CreateVehicleUnderMaintenanceAsync(Guid ownerId)
    {
        var vehicle = Vehicle.Register(
            ownerId, null, "Toyota", "Corolla", 2022, "White", $"PLATE-{Guid.NewGuid():N}"[..12], null, 10000, FuelType.Petrol,
            TransmissionType.Manual, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);
        await _vehicleRepository.TryApproveAsync(vehicle.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
        await _vehicleRepository.TryMarkUnderMaintenanceAsync(vehicle.Id, DateTime.UtcNow, CancellationToken.None);
        return vehicle;
    }

    private async Task<MaintenanceRequest> CreateRequestInProgressAsync(Guid ownerId, Guid garageUserId, Guid vehicleId)
    {
        var request = MaintenanceRequest.Create(vehicleId, ownerId, garageUserId, "Bruit suspect", DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [MaintenanceRequestStatus.PendingGarageResponse], MaintenanceRequestStatus.InProgress, garageUserId, null, null, null,
            null, null, DateTime.UtcNow, CancellationToken.None);
        return request;
    }

    private static List<MaintenanceRecordLineInput> SampleLines() => [new("Vidange", false, 1, 80m), new("Filtre", true, 1, 20m)];

    [Fact]
    public async Task Handle_EligibleRequest_CompletesRecordsExpenseAndReleasesVehicle()
    {
        var ownerId = Guid.NewGuid();
        var garageUserId = Guid.NewGuid();
        var vehicle = await CreateVehicleUnderMaintenanceAsync(ownerId);
        var request = await CreateRequestInProgressAsync(ownerId, garageUserId, vehicle.Id);

        var result = await _handler.Handle(
            new CompleteMaintenanceCommand(request.Id, garageUserId, 100m, SampleLines(), "RAS", null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);

        var reloadedRequest = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(MaintenanceRequestStatus.Completed, reloadedRequest!.Status);
        Assert.Equal(100m, reloadedRequest.FinalCost);

        var history = await _recordRepository.GetForVehicleAsync(vehicle.Id, CancellationToken.None);
        var record = Assert.Single(history);
        Assert.Equal(2, record.Lines.Count);
        Assert.Equal(10000, record.MileageAtCompletion); // best-effort snapshot of Vehicle.CurrentMileage

        var expense = Assert.Single(_fleetExpenseRepository.GetAllForTest());
        Assert.Equal(ExpenseCategory.Maintenance, expense.Category);
        Assert.Equal(vehicle.Id, expense.VehicleId);
        Assert.Equal(100m, expense.Money.Amount);

        var reloadedVehicle = await _vehicleRepository.GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(VehicleOperationalStatus.Active, reloadedVehicle!.OperationalStatus);
    }

    [Fact]
    public async Task Handle_VehicleIndependentlySuspendedBeforeCompletion_StillCompletesWithoutReactivating()
    {
        var ownerId = Guid.NewGuid();
        var garageUserId = Guid.NewGuid();
        var vehicle = await CreateVehicleUnderMaintenanceAsync(ownerId);
        var request = await CreateRequestInProgressAsync(ownerId, garageUserId, vehicle.Id);

        // Simulated independent admin action: the vehicle is pulled from UnderMaintenance and suspended for
        // an unrelated reason (e.g. an expired document) while the job is still open.
        typeof(Vehicle).GetProperty(nameof(Vehicle.OperationalStatus))!.SetValue(vehicle, VehicleOperationalStatus.Suspended);

        var result = await _handler.Handle(
            new CompleteMaintenanceCommand(request.Id, garageUserId, 100m, SampleLines(), null, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess); // completion succeeds regardless — the maintenance work itself genuinely finished
        var reloadedRequest = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(MaintenanceRequestStatus.Completed, reloadedRequest!.Status);

        var reloadedVehicle = await _vehicleRepository.GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(VehicleOperationalStatus.Suspended, reloadedVehicle!.OperationalStatus); // never silently reactivated

        Assert.Single(await _recordRepository.GetForVehicleAsync(vehicle.Id, CancellationToken.None));
        Assert.Single(_fleetExpenseRepository.GetAllForTest());
    }

    [Fact]
    public async Task Handle_FleetExpenseInsertFails_RollsBackRequestTransitionAndRecordToo()
    {
        var ownerId = Guid.NewGuid();
        var garageUserId = Guid.NewGuid();
        var vehicle = await CreateVehicleUnderMaintenanceAsync(ownerId);
        var request = await CreateRequestInProgressAsync(ownerId, garageUserId, vehicle.Id);
        _completionRepository.ThrowOnFleetExpenseInsert = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(
            new CompleteMaintenanceCommand(request.Id, garageUserId, 100m, SampleLines(), null, null, null), CancellationToken.None));

        var reloadedRequest = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(MaintenanceRequestStatus.InProgress, reloadedRequest!.Status); // never left Completed with no record
        Assert.Empty(await _recordRepository.GetForVehicleAsync(vehicle.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NonPositiveFinalCost_ReturnsValidationError()
    {
        var ownerId = Guid.NewGuid();
        var garageUserId = Guid.NewGuid();
        var vehicle = await CreateVehicleUnderMaintenanceAsync(ownerId);
        var request = await CreateRequestInProgressAsync(ownerId, garageUserId, vehicle.Id);

        var result = await _handler.Handle(
            new CompleteMaintenanceCommand(request.Id, garageUserId, 0m, SampleLines(), null, null, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_DuplicateCompletion_SecondAttemptReturnsConflict()
    {
        var ownerId = Guid.NewGuid();
        var garageUserId = Guid.NewGuid();
        var vehicle = await CreateVehicleUnderMaintenanceAsync(ownerId);
        var request = await CreateRequestInProgressAsync(ownerId, garageUserId, vehicle.Id);

        var first = await _handler.Handle(
            new CompleteMaintenanceCommand(request.Id, garageUserId, 100m, SampleLines(), null, null, null), CancellationToken.None);
        Assert.True(first.IsSuccess);

        var second = await _handler.Handle(
            new CompleteMaintenanceCommand(request.Id, garageUserId, 100m, SampleLines(), null, null, null), CancellationToken.None);

        Assert.False(second.IsSuccess);
        Assert.Equal(ErrorType.Conflict, second.ErrorType);
        Assert.Single(await _recordRepository.GetForVehicleAsync(vehicle.Id, CancellationToken.None)); // only one record ever created
    }
}

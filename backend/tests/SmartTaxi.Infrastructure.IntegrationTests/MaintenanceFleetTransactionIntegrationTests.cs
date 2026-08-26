using Microsoft.EntityFrameworkCore;
using SmartTaxi.Domain.Fleet.Expenses.Entities;
using SmartTaxi.Domain.Fleet.Expenses.Enums;
using SmartTaxi.Domain.Fleet.Expenses.ValueObjects;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.Enums;
using SmartTaxi.Domain.Maintenance.ValueObjects;
using SmartTaxi.Infrastructure.Fleet.Repositories;
using SmartTaxi.Infrastructure.Maintenance.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves, against real PostgreSQL, the Module 9 mandatory atomicity design
/// (approved plan §2/§3) for the transitions that coordinate MaintenanceRequest
/// with Fleet's Vehicle.OperationalStatus — the exact guarantee the user's
/// architectural correction required: no compensation-based design, no window
/// where a vehicle can be left UnderMaintenance with no corresponding job, or
/// a completed job leaving the vehicle permanently unreleased.
/// </summary>
[Collection("SharedPostgres")]
public class MaintenanceFleetTransactionIntegrationTests
{
    private readonly SharedPostgresFixture _fixture;

    public MaintenanceFleetTransactionIntegrationTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<Vehicle> CreateActiveVehicleAsync(Guid ownerId)
    {
        var vehicle = Vehicle.Register(
            ownerId, null, "Toyota", "Corolla", 2022, "White", $"PLATE-{Guid.NewGuid():N}"[..12], null, 10000, FuelType.Petrol,
            TransmissionType.Manual, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);

        await using (var context = _fixture.CreateContext())
        {
            await new VehicleRepository(context).AddAsync(vehicle, CancellationToken.None);
        }

        await using (var context = _fixture.CreateContext())
        {
            await new VehicleRepository(context).TryApproveAsync(vehicle.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
        }

        return vehicle;
    }

    private async Task<MaintenanceRequest> CreateRequestAtStatusAsync(Guid vehicleId, Guid ownerId, Guid garageUserId, MaintenanceRequestStatus status)
    {
        var request = MaintenanceRequest.Create(vehicleId, ownerId, garageUserId, "Bruit suspect", DateTime.UtcNow);

        await using (var context = _fixture.CreateContext())
        {
            await new MaintenanceRequestRepository(context).TryAddAsync(request, CancellationToken.None);
        }

        if (status != MaintenanceRequestStatus.PendingGarageResponse)
        {
            await using var context = _fixture.CreateContext();
            await new MaintenanceRequestRepository(context).TryTransitionAsync(
                request.Id, [MaintenanceRequestStatus.PendingGarageResponse], status, garageUserId, null, null, null, null, null,
                DateTime.UtcNow, CancellationToken.None);
        }

        return request;
    }

    // ---- Start atomicity ----

    [Fact]
    public async Task Start_EligibleRequestAndActiveVehicle_BothTransitionTogether()
    {
        var ownerId = Guid.NewGuid();
        var garageUserId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        var request = await CreateRequestAtStatusAsync(vehicle.Id, ownerId, garageUserId, MaintenanceRequestStatus.VehicleReceived);

        await using var context = _fixture.CreateContext();
        var vehicleRepository = new VehicleRepository(context);
        var requestRepository = new MaintenanceRequestRepository(context);
        var workStartRepository = new MaintenanceWorkStartRepository(context, requestRepository, vehicleRepository);

        var outcome = await workStartRepository.TryStartAsync(request.Id, garageUserId, vehicle.Id, DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(Application.Maintenance.Contracts.MaintenanceWorkStartResult.Started, outcome);

        await using var readContext = _fixture.CreateContext();
        var reloadedRequest = await new MaintenanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        var reloadedVehicle = await new VehicleRepository(readContext).GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(MaintenanceRequestStatus.InProgress, reloadedRequest!.Status);
        Assert.Equal(VehicleOperationalStatus.UnderMaintenance, reloadedVehicle!.OperationalStatus);
    }

    [Fact]
    public async Task Start_VehicleAlreadySuspended_RollsBackTheWholeTransaction()
    {
        var ownerId = Guid.NewGuid();
        var garageUserId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);

        await using (var context = _fixture.CreateContext())
        {
            await new VehicleRepository(context).TrySuspendAsync(vehicle.Id, DateTime.UtcNow, CancellationToken.None);
        }

        var request = await CreateRequestAtStatusAsync(vehicle.Id, ownerId, garageUserId, MaintenanceRequestStatus.VehicleReceived);

        await using var context2 = _fixture.CreateContext();
        var vehicleRepository = new VehicleRepository(context2);
        var requestRepository = new MaintenanceRequestRepository(context2);
        var workStartRepository = new MaintenanceWorkStartRepository(context2, requestRepository, vehicleRepository);

        var outcome = await workStartRepository.TryStartAsync(request.Id, garageUserId, vehicle.Id, DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(Application.Maintenance.Contracts.MaintenanceWorkStartResult.VehicleNotEligible, outcome);

        await using var readContext = _fixture.CreateContext();
        var reloadedRequest = await new MaintenanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        var reloadedVehicle = await new VehicleRepository(readContext).GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(MaintenanceRequestStatus.VehicleReceived, reloadedRequest!.Status); // NOT stranded in InProgress
        Assert.Equal(VehicleOperationalStatus.Suspended, reloadedVehicle!.OperationalStatus);
    }

    [Fact]
    public async Task Start_TwoConcurrentAttempts_OnlyOneSucceeds()
    {
        var ownerId = Guid.NewGuid();
        var garageUserId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        var request = await CreateRequestAtStatusAsync(vehicle.Id, ownerId, garageUserId, MaintenanceRequestStatus.VehicleReceived);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var repo1 = new MaintenanceWorkStartRepository(context1, new MaintenanceRequestRepository(context1), new VehicleRepository(context1));
        var repo2 = new MaintenanceWorkStartRepository(context2, new MaintenanceRequestRepository(context2), new VehicleRepository(context2));

        var results = await Task.WhenAll(
            repo1.TryStartAsync(request.Id, garageUserId, vehicle.Id, DateTime.UtcNow, CancellationToken.None),
            repo2.TryStartAsync(request.Id, garageUserId, vehicle.Id, DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r == Application.Maintenance.Contracts.MaintenanceWorkStartResult.Started);

        await using var readContext = _fixture.CreateContext();
        var reloadedVehicle = await new VehicleRepository(readContext).GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(VehicleOperationalStatus.UnderMaintenance, reloadedVehicle!.OperationalStatus); // never double-transitioned
    }

    // ---- Completion atomicity ----

    private async Task<(MaintenanceRequest Request, Vehicle Vehicle)> CreateRequestInProgressAsync()
    {
        var ownerId = Guid.NewGuid();
        var garageUserId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);

        await using (var context = _fixture.CreateContext())
        {
            await new VehicleRepository(context).TryMarkUnderMaintenanceAsync(vehicle.Id, DateTime.UtcNow, CancellationToken.None);
        }

        var request = await CreateRequestAtStatusAsync(vehicle.Id, ownerId, garageUserId, MaintenanceRequestStatus.InProgress);
        return (request, vehicle);
    }

    private static (MaintenanceRecord Record, FleetExpense Expense) BuildCompletionArtifacts(MaintenanceRequest request)
    {
        var record = MaintenanceRecord.Create(
            request.Id, request.VehicleId, request.OwnerUserId, request.GarageUserId, DateOnly.FromDateTime(DateTime.UtcNow), 10000,
            [new MaintenanceRecordLine("Vidange", false, 1, 100m)], 100m, null, null, null, DateTime.UtcNow);

        var expense = FleetExpense.Create(
            request.OwnerUserId, null, request.VehicleId, null, ExpenseCategory.Maintenance, Money.Create(100m, "TND"),
            DateOnly.FromDateTime(DateTime.UtcNow), $"Maintenance request {request.Id}", null, request.OwnerUserId, DateTime.UtcNow);

        return (record, expense);
    }

    [Fact]
    public async Task Complete_EligibleRequest_CommitsRequestRecordExpenseAndReleasesVehicleTogether()
    {
        var (request, vehicle) = await CreateRequestInProgressAsync();
        var (record, expense) = BuildCompletionArtifacts(request);

        await using var context = _fixture.CreateContext();
        var completionRepository = new MaintenanceCompletionRepository(
            context, new MaintenanceRequestRepository(context), new VehicleRepository(context), new FleetExpenseRepository(context));

        var outcome = await completionRepository.TryCompleteAsync(
            request.Id, request.GarageUserId, vehicle.Id, record, expense, DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(Application.Maintenance.Contracts.MaintenanceCompletionResult.Completed, outcome);

        await using var readContext = _fixture.CreateContext();
        var reloadedRequest = await new MaintenanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        var reloadedVehicle = await new VehicleRepository(readContext).GetByIdAsync(vehicle.Id, CancellationToken.None);
        var records = await new MaintenanceRecordRepository(readContext).GetForVehicleAsync(vehicle.Id, CancellationToken.None);
        var expenseCount = await readContext.FleetExpenses.CountAsync(e => e.VehicleId == vehicle.Id, CancellationToken.None);

        Assert.Equal(MaintenanceRequestStatus.Completed, reloadedRequest!.Status);
        Assert.Equal(VehicleOperationalStatus.Active, reloadedVehicle!.OperationalStatus);
        Assert.Single(records);
        Assert.Equal(1, expenseCount);
    }

    [Fact]
    public async Task Complete_VehicleIndependentlySuspended_StillCompletesWithoutReactivating()
    {
        var (request, vehicle) = await CreateRequestInProgressAsync();

        // Simulated independent admin action while the job is still open.
        await using (var context = _fixture.CreateContext())
        {
            await context.Vehicles.Where(v => v.Id == vehicle.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(v => v.OperationalStatus, VehicleOperationalStatus.Suspended), CancellationToken.None);
        }

        var (record, expense) = BuildCompletionArtifacts(request);

        await using var context2 = _fixture.CreateContext();
        var completionRepository = new MaintenanceCompletionRepository(
            context2, new MaintenanceRequestRepository(context2), new VehicleRepository(context2), new FleetExpenseRepository(context2));

        var outcome = await completionRepository.TryCompleteAsync(
            request.Id, request.GarageUserId, vehicle.Id, record, expense, DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(Application.Maintenance.Contracts.MaintenanceCompletionResult.Completed, outcome); // succeeds regardless

        await using var readContext = _fixture.CreateContext();
        var reloadedRequest = await new MaintenanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        var reloadedVehicle = await new VehicleRepository(readContext).GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(MaintenanceRequestStatus.Completed, reloadedRequest!.Status);
        Assert.Equal(VehicleOperationalStatus.Suspended, reloadedVehicle!.OperationalStatus); // never silently reactivated
    }

    [Fact]
    public async Task Complete_TwoConcurrentAttempts_OnlyOneSucceeds()
    {
        var (request, vehicle) = await CreateRequestInProgressAsync();
        var (record1, expense1) = BuildCompletionArtifacts(request);
        var (record2, expense2) = BuildCompletionArtifacts(request);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var repo1 = new MaintenanceCompletionRepository(
            context1, new MaintenanceRequestRepository(context1), new VehicleRepository(context1), new FleetExpenseRepository(context1));
        var repo2 = new MaintenanceCompletionRepository(
            context2, new MaintenanceRequestRepository(context2), new VehicleRepository(context2), new FleetExpenseRepository(context2));

        var results = await Task.WhenAll(
            repo1.TryCompleteAsync(request.Id, request.GarageUserId, vehicle.Id, record1, expense1, DateTime.UtcNow, CancellationToken.None),
            repo2.TryCompleteAsync(request.Id, request.GarageUserId, vehicle.Id, record2, expense2, DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, r => r == Application.Maintenance.Contracts.MaintenanceCompletionResult.Completed);

        await using var readContext = _fixture.CreateContext();
        var records = await new MaintenanceRecordRepository(readContext).GetForVehicleAsync(vehicle.Id, CancellationToken.None);
        Assert.Single(records); // never double-recorded
    }

    // ---- Force-cancel ----

    [Fact]
    public async Task ForceCancel_FromInProgress_ReleasesVehicle()
    {
        var (request, vehicle) = await CreateRequestInProgressAsync();

        await using var context = _fixture.CreateContext();
        var forceCancelRepository = new MaintenanceForceCancelRepository(context, new MaintenanceRequestRepository(context), new VehicleRepository(context));

        var succeeded = await forceCancelRepository.TryForceCancelAsync(
            request.Id, Guid.NewGuid(), vehicle.Id, "Litige", DateTime.UtcNow, CancellationToken.None);

        Assert.True(succeeded);

        await using var readContext = _fixture.CreateContext();
        var reloadedRequest = await new MaintenanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);
        var reloadedVehicle = await new VehicleRepository(readContext).GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(MaintenanceRequestStatus.Cancelled, reloadedRequest!.Status);
        Assert.Equal(VehicleOperationalStatus.Active, reloadedVehicle!.OperationalStatus);
    }

    [Fact]
    public async Task ForceCancelVsCompletion_ConcurrentRace_OnlyOneWins()
    {
        var (request, vehicle) = await CreateRequestInProgressAsync();
        var (record, expense) = BuildCompletionArtifacts(request);

        await using var context1 = _fixture.CreateContext();
        await using var context2 = _fixture.CreateContext();

        var completionRepository = new MaintenanceCompletionRepository(
            context1, new MaintenanceRequestRepository(context1), new VehicleRepository(context1), new FleetExpenseRepository(context1));
        var forceCancelRepository = new MaintenanceForceCancelRepository(context2, new MaintenanceRequestRepository(context2), new VehicleRepository(context2));

        var completionTask = completionRepository.TryCompleteAsync(
            request.Id, request.GarageUserId, vehicle.Id, record, expense, DateTime.UtcNow, CancellationToken.None);
        var cancelTask = forceCancelRepository.TryForceCancelAsync(request.Id, Guid.NewGuid(), vehicle.Id, "Litige", DateTime.UtcNow, CancellationToken.None);

        await Task.WhenAll(completionTask, cancelTask);
        var completionOutcome = await completionTask;
        var cancelOutcome = await cancelTask;

        await using var readContext = _fixture.CreateContext();
        var reloadedRequest = await new MaintenanceRequestRepository(readContext).GetByIdAsync(request.Id, CancellationToken.None);

        // Exactly one of the two terminal outcomes won — never both, never neither.
        Assert.True(reloadedRequest!.Status is MaintenanceRequestStatus.Completed or MaintenanceRequestStatus.Cancelled);
        var bothWon = completionOutcome == Application.Maintenance.Contracts.MaintenanceCompletionResult.Completed && cancelOutcome;
        Assert.False(bothWon);
    }
}

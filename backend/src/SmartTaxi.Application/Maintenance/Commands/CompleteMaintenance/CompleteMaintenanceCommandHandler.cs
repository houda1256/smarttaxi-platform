using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Application.Maintenance.Contracts;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Fleet.Expenses.Entities;
using SmartTaxi.Domain.Fleet.Expenses.Enums;
using SmartTaxi.Domain.Fleet.Expenses.ValueObjects;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.ValueObjects;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Maintenance.Commands.CompleteMaintenance;

/// <summary>
/// Mandatory atomicity design (approved plan correction): constructs the
/// MaintenanceRecord (immutable digital-maintenance-book entry) and the
/// authoritative FleetExpense (Fleet's own entity, Category=Maintenance —
/// never a second expense system), then hands both pre-built entities to
/// IMaintenanceCompletionRepository, whose single transaction persists them
/// together with the request's own Completed transition and a best-effort
/// Fleet release — see that interface's doc comment for the explicit
/// "never reactivate an independently-Suspended vehicle" business rule.
/// MileageAtCompletion is read once, best-effort, from Fleet's own
/// Vehicle.CurrentMileage — never trusted as authoritative (Module 9 audit
/// finding: no automatic mileage-update pipeline exists).
/// </summary>
public sealed class CompleteMaintenanceCommandHandler : ICommandHandler<CompleteMaintenanceCommand, Result>
{
    private const string NotFoundError = "Demande de maintenance introuvable.";
    private const string NotEligibleError = "Cette demande n'est pas en cours d'intervention.";
    private const string InvalidCostError = "Le coût final doit être strictement positif.";
    private const string Currency = "TND";

    private readonly IMaintenanceRequestRepository _requestRepository;
    private readonly IMaintenanceCompletionRepository _completionRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public CompleteMaintenanceCommandHandler(
        IMaintenanceRequestRepository requestRepository, IMaintenanceCompletionRepository completionRepository,
        IVehicleRepository vehicleRepository, INotificationDispatcher notificationDispatcher)
    {
        _requestRepository = requestRepository;
        _completionRepository = completionRepository;
        _vehicleRepository = vehicleRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(CompleteMaintenanceCommand command, CancellationToken cancellationToken)
    {
        if (command.FinalCost <= 0)
        {
            return Result.Failure(InvalidCostError, ErrorType.Validation);
        }

        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var utcNow = DateTime.UtcNow;
        var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken);

        MaintenanceRecord record;
        FleetExpense expense;

        try
        {
            var lines = command.Lines.Select(line => new MaintenanceRecordLine(line.Description, line.IsPart, line.Quantity, line.UnitCost)).ToList();

            record = MaintenanceRecord.Create(
                request.Id, request.VehicleId, request.OwnerUserId, command.GarageUserId, DateOnly.FromDateTime(utcNow),
                vehicle?.CurrentMileage, lines, command.FinalCost, command.Notes, command.NextRecommendedServiceDate, command.WarrantyInfo,
                utcNow);

            expense = FleetExpense.Create(
                request.OwnerUserId, fleetId: null, vehicleId: request.VehicleId, driverId: null, ExpenseCategory.Maintenance,
                Money.Create(command.FinalCost, Currency), DateOnly.FromDateTime(utcNow), $"Maintenance request {request.Id}",
                receiptReference: null, request.OwnerUserId, utcNow);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message, ErrorType.Validation);
        }

        var outcome = await _completionRepository.TryCompleteAsync(
            request.Id, command.GarageUserId, request.VehicleId, record, expense, utcNow, cancellationToken);

        if (outcome != MaintenanceCompletionResult.Completed)
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                request.OwnerUserId, NotificationCategory.Maintenance, "maintenance.completed",
                new Dictionary<string, string> { ["FinalCost"] = command.FinalCost.ToString("F2") }, IsMandatory: false,
                SourceType: "MaintenanceRequest", SourceId: request.Id),
            cancellationToken);

        return Result.Success();
    }
}

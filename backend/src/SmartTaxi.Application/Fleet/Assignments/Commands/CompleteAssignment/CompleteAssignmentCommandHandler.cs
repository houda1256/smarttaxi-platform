using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Assignments.Abstractions;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Fleet.UsageHistory.Abstractions;
using SmartTaxi.Domain.Fleet.Assignments.Enums;
using SmartTaxi.Domain.Fleet.UsageHistory.Entities;

namespace SmartTaxi.Application.Fleet.Assignments.Commands.CompleteAssignment;

/// <summary>
/// Writes the closing VehicleUsageRecord for this assignment's usage period.
/// StartedAt uses the assignment's CreatedAt as an approximation of when the
/// driver actually started using the vehicle — there is no separate
/// "ActivatedAt" clock-in timestamp on the assignment today.
/// </summary>
public sealed class CompleteAssignmentCommandHandler : ICommandHandler<CompleteAssignmentCommand, Result>
{
    private const string NotFoundError = "Affectation introuvable.";
    private const string NotActiveError = "Seule une affectation active peut être terminée.";

    private readonly IDriverVehicleAssignmentRepository _repository;
    private readonly IDriverProfileRepository _driverRepository;
    private readonly IVehicleUsageRecordRepository _usageRecordRepository;

    public CompleteAssignmentCommandHandler(
        IDriverVehicleAssignmentRepository repository, IDriverProfileRepository driverRepository,
        IVehicleUsageRecordRepository usageRecordRepository)
    {
        _repository = repository;
        _driverRepository = driverRepository;
        _usageRecordRepository = usageRecordRepository;
    }

    public async Task<Result> Handle(CompleteAssignmentCommand command, CancellationToken cancellationToken)
    {
        var assignment = await _repository.GetByIdAsync(command.AssignmentId, cancellationToken);

        if (assignment is null || assignment.OwnerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (assignment.Status != AssignmentStatus.Active)
        {
            return Result.Failure(NotActiveError, ErrorType.Conflict);
        }

        if (command.MileageEnd < command.MileageStart)
        {
            return Result.Failure(
                "Le kilométrage de fin ne peut pas être inférieur au kilométrage de début.", ErrorType.Validation);
        }

        var utcNow = DateTime.UtcNow;

        var completed = await _repository.TryCompleteAsync(assignment.Id, utcNow, cancellationToken);

        if (!completed)
        {
            return Result.Failure(NotActiveError, ErrorType.Conflict);
        }

        var usageRecord = new VehicleUsageRecord(
            assignment.VehicleId, assignment.DriverId, assignment.Id, assignment.CreatedAt, utcNow,
            command.MileageStart, command.MileageEnd, command.RideCount, command.RevenueGenerated,
            command.IncidentCount, utcNow);

        await _usageRecordRepository.AddAsync(usageRecord, cancellationToken);

        var driver = await _driverRepository.GetByIdAsync(assignment.DriverId, cancellationToken);

        if (driver is not null && driver.CurrentVehicleId == assignment.VehicleId)
        {
            driver.UnassignVehicle(utcNow);
            await _driverRepository.UpdateAsync(driver, cancellationToken);
        }

        return Result.Success();
    }
}

using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Assignments.Abstractions;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Domain.Fleet.Assignments.Enums;

namespace SmartTaxi.Application.Fleet.Assignments.Commands.ActivateAssignment;

/// <summary>
/// Race-safety: the atomic ExecuteUpdateAsync guard (WHERE Status =
/// PendingApproval) is what makes "two concurrent activations of the SAME
/// assignment cannot both succeed" true — proven in the Postgres integration
/// test. Overlap-with-*other* assignments is checked application-side just
/// before the atomic call; closing that residual cross-assignment race with a
/// DB-level exclusion constraint is a documented follow-up, not implemented here.
/// </summary>
public sealed class ActivateAssignmentCommandHandler : ICommandHandler<ActivateAssignmentCommand, Result>
{
    private const string NotFoundError = "Affectation introuvable.";
    private const string NotPendingApprovalError = "Cette affectation n'est pas en attente d'approbation.";
    private const string OverlapError = "Une autre affectation active chevauche cette période.";

    private readonly IDriverVehicleAssignmentRepository _repository;
    private readonly IDriverProfileRepository _driverRepository;

    public ActivateAssignmentCommandHandler(IDriverVehicleAssignmentRepository repository, IDriverProfileRepository driverRepository)
    {
        _repository = repository;
        _driverRepository = driverRepository;
    }

    public async Task<Result> Handle(ActivateAssignmentCommand command, CancellationToken cancellationToken)
    {
        var assignment = await _repository.GetByIdAsync(command.AssignmentId, cancellationToken);

        if (assignment is null || assignment.OwnerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (assignment.Status != AssignmentStatus.PendingApproval)
        {
            return Result.Failure(NotPendingApprovalError, ErrorType.Conflict);
        }

        var otherDriverAssignments = await _repository.GetActiveOrPendingForDriverAsync(
            assignment.DriverId, assignment.Id, cancellationToken);

        if (otherDriverAssignments.Any(other => other.Status == AssignmentStatus.Active && other.OverlapsWith(assignment)))
        {
            return Result.Failure(OverlapError, ErrorType.Conflict);
        }

        var otherVehicleAssignments = await _repository.GetActiveOrPendingForVehicleAsync(
            assignment.VehicleId, assignment.Id, cancellationToken);

        if (otherVehicleAssignments.Any(other => other.Status == AssignmentStatus.Active && other.OverlapsWith(assignment)))
        {
            return Result.Failure(OverlapError, ErrorType.Conflict);
        }

        var activated = await _repository.TryActivateAsync(assignment.Id, DateTime.UtcNow, cancellationToken);

        if (!activated)
        {
            return Result.Failure(NotPendingApprovalError, ErrorType.Conflict);
        }

        var driver = await _driverRepository.GetByIdAsync(assignment.DriverId, cancellationToken);

        if (driver is not null)
        {
            driver.AssignVehicle(assignment.VehicleId, DateTime.UtcNow);
            await _driverRepository.UpdateAsync(driver, cancellationToken);
        }

        return Result.Success();
    }
}

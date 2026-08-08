using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Assignments.Abstractions;
using SmartTaxi.Domain.Fleet.Assignments.Enums;

namespace SmartTaxi.Application.Fleet.Assignments.Commands.SuspendAssignment;

public sealed class SuspendAssignmentCommandHandler : ICommandHandler<SuspendAssignmentCommand, Result>
{
    private const string NotFoundError = "Affectation introuvable.";
    private const string NotActiveError = "Seule une affectation active peut être suspendue.";

    private readonly IDriverVehicleAssignmentRepository _repository;

    public SuspendAssignmentCommandHandler(IDriverVehicleAssignmentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(SuspendAssignmentCommand command, CancellationToken cancellationToken)
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

        var suspended = await _repository.TrySuspendAsync(assignment.Id, DateTime.UtcNow, cancellationToken);

        if (!suspended)
        {
            return Result.Failure(NotActiveError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}

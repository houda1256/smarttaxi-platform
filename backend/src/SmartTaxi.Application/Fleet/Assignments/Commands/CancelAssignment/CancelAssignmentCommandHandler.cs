using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Assignments.Abstractions;
using SmartTaxi.Domain.Fleet.Assignments.Enums;

namespace SmartTaxi.Application.Fleet.Assignments.Commands.CancelAssignment;

public sealed class CancelAssignmentCommandHandler : ICommandHandler<CancelAssignmentCommand, Result>
{
    private const string NotFoundError = "Affectation introuvable.";
    private const string TerminalStateError = "Cette affectation est déjà terminée ou annulée.";

    private readonly IDriverVehicleAssignmentRepository _repository;

    public CancelAssignmentCommandHandler(IDriverVehicleAssignmentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(CancelAssignmentCommand command, CancellationToken cancellationToken)
    {
        var assignment = await _repository.GetByIdAsync(command.AssignmentId, cancellationToken);

        if (assignment is null || assignment.OwnerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (assignment.Status is AssignmentStatus.Completed or AssignmentStatus.Cancelled)
        {
            return Result.Failure(TerminalStateError, ErrorType.Conflict);
        }

        var cancelled = await _repository.TryCancelAsync(assignment.Id, DateTime.UtcNow, cancellationToken);

        if (!cancelled)
        {
            return Result.Failure(TerminalStateError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}

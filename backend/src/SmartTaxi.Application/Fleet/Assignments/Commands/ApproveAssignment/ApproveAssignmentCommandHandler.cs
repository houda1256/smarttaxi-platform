using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Assignments.Abstractions;
using SmartTaxi.Domain.Fleet.Assignments.Enums;

namespace SmartTaxi.Application.Fleet.Assignments.Commands.ApproveAssignment;

/// <summary>Owner approval — required before a Draft assignment can ever be activated, per the vehicle-belongs-to-a-Taxi-Owner rule.</summary>
public sealed class ApproveAssignmentCommandHandler : ICommandHandler<ApproveAssignmentCommand, Result>
{
    private const string NotFoundError = "Affectation introuvable.";
    private const string NotDraftError = "Cette affectation n'est plus à l'état de brouillon.";

    private readonly IDriverVehicleAssignmentRepository _repository;

    public ApproveAssignmentCommandHandler(IDriverVehicleAssignmentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(ApproveAssignmentCommand command, CancellationToken cancellationToken)
    {
        var assignment = await _repository.GetByIdAsync(command.AssignmentId, cancellationToken);

        if (assignment is null || assignment.OwnerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (assignment.Status != AssignmentStatus.Draft)
        {
            return Result.Failure(NotDraftError, ErrorType.Conflict);
        }

        var approved = await _repository.TryApproveAsync(assignment.Id, DateTime.UtcNow, cancellationToken);

        if (!approved)
        {
            return Result.Failure(NotDraftError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}

using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Domain.Maintenance.Enums;

namespace SmartTaxi.Application.Maintenance.Commands.ResumeMaintenanceWork;

public sealed class ResumeMaintenanceWorkCommandHandler : ICommandHandler<ResumeMaintenanceWorkCommand, Result>
{
    private const string NotFoundError = "Demande de maintenance introuvable.";
    private const string NotEligibleError = "Cette demande n'est pas en attente de pièces.";

    private static readonly MaintenanceRequestStatus[] AllowedFromStatuses = [MaintenanceRequestStatus.WaitingForParts];

    private readonly IMaintenanceRequestRepository _requestRepository;

    public ResumeMaintenanceWorkCommandHandler(IMaintenanceRequestRepository requestRepository)
    {
        _requestRepository = requestRepository;
    }

    public async Task<Result> Handle(ResumeMaintenanceWorkCommand command, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var transitioned = await _requestRepository.TryTransitionAsync(
            request.Id, AllowedFromStatuses, MaintenanceRequestStatus.InProgress, requiredGarageUserId: command.GarageUserId,
            requiredOwnerUserId: null, estimatedCost: null, finalCost: null, reason: null, cancelledByUserId: null, DateTime.UtcNow,
            cancellationToken);

        return transitioned ? Result.Success() : Result.Failure(NotEligibleError, ErrorType.Conflict);
    }
}

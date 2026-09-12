using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Domain.Maintenance.Enums;

namespace SmartTaxi.Application.Maintenance.Commands.MarkWaitingForParts;

/// <summary>Pure Maintenance-side toggle — the vehicle stays Fleet-UnderMaintenance the whole time, no Fleet call needed here (unlike Start/Complete/ForceCancel).</summary>
public sealed class MarkWaitingForPartsCommandHandler : ICommandHandler<MarkWaitingForPartsCommand, Result>
{
    private const string NotFoundError = "Demande de maintenance introuvable.";
    private const string NotEligibleError = "Cette demande n'est pas en cours d'intervention.";

    private static readonly MaintenanceRequestStatus[] AllowedFromStatuses = [MaintenanceRequestStatus.InProgress];

    private readonly IMaintenanceRequestRepository _requestRepository;

    public MarkWaitingForPartsCommandHandler(IMaintenanceRequestRepository requestRepository)
    {
        _requestRepository = requestRepository;
    }

    public async Task<Result> Handle(MarkWaitingForPartsCommand command, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var transitioned = await _requestRepository.TryTransitionAsync(
            request.Id, AllowedFromStatuses, MaintenanceRequestStatus.WaitingForParts, requiredGarageUserId: command.GarageUserId,
            requiredOwnerUserId: null, estimatedCost: null, finalCost: null, reason: null, cancelledByUserId: null, DateTime.UtcNow,
            cancellationToken);

        return transitioned ? Result.Success() : Result.Failure(NotEligibleError, ErrorType.Conflict);
    }
}

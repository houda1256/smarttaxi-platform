using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Domain.Maintenance.Enums;

namespace SmartTaxi.Application.Maintenance.Commands.MarkVehicleReceived;

/// <summary>Purely a Maintenance-side bookkeeping transition — the vehicle only enters Fleet's UnderMaintenance status once work actually starts (see StartMaintenanceWorkCommandHandler), not merely at physical receipt.</summary>
public sealed class MarkVehicleReceivedCommandHandler : ICommandHandler<MarkVehicleReceivedCommand, Result>
{
    private const string NotFoundError = "Demande de maintenance introuvable.";
    private const string NotEligibleError = "Cette demande n'est pas en attente de réception du véhicule.";

    private static readonly MaintenanceRequestStatus[] AllowedFromStatuses = [MaintenanceRequestStatus.QuoteAccepted];

    private readonly IMaintenanceRequestRepository _requestRepository;

    public MarkVehicleReceivedCommandHandler(IMaintenanceRequestRepository requestRepository)
    {
        _requestRepository = requestRepository;
    }

    public async Task<Result> Handle(MarkVehicleReceivedCommand command, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var transitioned = await _requestRepository.TryTransitionAsync(
            request.Id, AllowedFromStatuses, MaintenanceRequestStatus.VehicleReceived, requiredGarageUserId: command.GarageUserId,
            requiredOwnerUserId: null, estimatedCost: null, finalCost: null, reason: null, cancelledByUserId: null, DateTime.UtcNow,
            cancellationToken);

        return transitioned ? Result.Success() : Result.Failure(NotEligibleError, ErrorType.Conflict);
    }
}

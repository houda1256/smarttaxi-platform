using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Commands.CloseSupportIncident;

/// <summary>Resolved -&gt; Closed. Callable by any admin — incidents have no requester side, so there is no ownership rule to enforce here (unlike SupportTicket.Close).</summary>
public sealed class CloseSupportIncidentCommandHandler : ICommandHandler<CloseSupportIncidentCommand, Result>
{
    private const string NotFoundError = "Incident introuvable.";
    private const string NotEligibleError = "Cet incident n'est pas résolu.";

    private static readonly SupportIncidentStatus[] AllowedFromStatuses = [SupportIncidentStatus.Resolved];

    private readonly ISupportIncidentRepository _incidentRepository;

    public CloseSupportIncidentCommandHandler(ISupportIncidentRepository incidentRepository)
    {
        _incidentRepository = incidentRepository;
    }

    public async Task<Result> Handle(CloseSupportIncidentCommand command, CancellationToken cancellationToken)
    {
        var incident = await _incidentRepository.GetByIdAsync(command.IncidentId, cancellationToken);

        if (incident is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var transitioned = await _incidentRepository.TryTransitionAsync(
            command.IncidentId, AllowedFromStatuses, SupportIncidentStatus.Closed, requiredAdminUserId: null, resolution: null,
            DateTime.UtcNow, cancellationToken);

        return transitioned ? Result.Success() : Result.Failure(NotEligibleError, ErrorType.Conflict);
    }
}

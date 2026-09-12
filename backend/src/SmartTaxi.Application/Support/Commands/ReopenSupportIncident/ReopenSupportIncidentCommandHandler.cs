using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Commands.ReopenSupportIncident;

/// <summary>Closed -&gt; Reopened. Callable by any admin.</summary>
public sealed class ReopenSupportIncidentCommandHandler : ICommandHandler<ReopenSupportIncidentCommand, Result>
{
    private const string NotFoundError = "Incident introuvable.";
    private const string NotEligibleError = "Cet incident n'est pas clôturé.";

    private static readonly SupportIncidentStatus[] AllowedFromStatuses = [SupportIncidentStatus.Closed];

    private readonly ISupportIncidentRepository _incidentRepository;

    public ReopenSupportIncidentCommandHandler(ISupportIncidentRepository incidentRepository)
    {
        _incidentRepository = incidentRepository;
    }

    public async Task<Result> Handle(ReopenSupportIncidentCommand command, CancellationToken cancellationToken)
    {
        var incident = await _incidentRepository.GetByIdAsync(command.IncidentId, cancellationToken);

        if (incident is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var transitioned = await _incidentRepository.TryTransitionAsync(
            command.IncidentId, AllowedFromStatuses, SupportIncidentStatus.Reopened, requiredAdminUserId: null, resolution: null,
            DateTime.UtcNow, cancellationToken);

        return transitioned ? Result.Success() : Result.Failure(NotEligibleError, ErrorType.Conflict);
    }
}

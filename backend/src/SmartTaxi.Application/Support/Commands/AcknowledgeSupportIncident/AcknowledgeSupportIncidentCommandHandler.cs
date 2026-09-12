using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Support.Abstractions;

namespace SmartTaxi.Application.Support.Commands.AcknowledgeSupportIncident;

/// <summary>Reported -&gt; Acknowledged, folding in first assignment (TryAcknowledgeAsync sets AssignedAdminUserId to the caller atomically).</summary>
public sealed class AcknowledgeSupportIncidentCommandHandler : ICommandHandler<AcknowledgeSupportIncidentCommand, Result>
{
    private const string NotFoundError = "Incident introuvable.";
    private const string NotEligibleError = "Cet incident n'est plus disponible pour un premier accusé de réception.";

    private readonly ISupportIncidentRepository _incidentRepository;

    public AcknowledgeSupportIncidentCommandHandler(ISupportIncidentRepository incidentRepository)
    {
        _incidentRepository = incidentRepository;
    }

    public async Task<Result> Handle(AcknowledgeSupportIncidentCommand command, CancellationToken cancellationToken)
    {
        var incident = await _incidentRepository.GetByIdAsync(command.IncidentId, cancellationToken);

        if (incident is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var acknowledged = await _incidentRepository.TryAcknowledgeAsync(
            command.IncidentId, command.AdminUserId, DateTime.UtcNow, cancellationToken);

        return acknowledged ? Result.Success() : Result.Failure(NotEligibleError, ErrorType.Conflict);
    }
}

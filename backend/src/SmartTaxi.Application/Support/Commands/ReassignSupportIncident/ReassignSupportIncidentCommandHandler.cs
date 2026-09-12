using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Support.Abstractions;

namespace SmartTaxi.Application.Support.Commands.ReassignSupportIncident;

/// <summary>Admin override, callable by any admin at any non-terminal status.</summary>
public sealed class ReassignSupportIncidentCommandHandler : ICommandHandler<ReassignSupportIncidentCommand, Result>
{
    private const string NotFoundError = "Incident introuvable.";
    private const string NotEligibleError = "Cet incident ne peut pas être réaffecté dans son état actuel.";

    private readonly ISupportIncidentRepository _incidentRepository;

    public ReassignSupportIncidentCommandHandler(ISupportIncidentRepository incidentRepository)
    {
        _incidentRepository = incidentRepository;
    }

    public async Task<Result> Handle(ReassignSupportIncidentCommand command, CancellationToken cancellationToken)
    {
        var incident = await _incidentRepository.GetByIdAsync(command.IncidentId, cancellationToken);

        if (incident is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var reassigned = await _incidentRepository.TryReassignAsync(
            command.IncidentId, command.NewAdminUserId, DateTime.UtcNow, cancellationToken);

        return reassigned ? Result.Success() : Result.Failure(NotEligibleError, ErrorType.Conflict);
    }
}

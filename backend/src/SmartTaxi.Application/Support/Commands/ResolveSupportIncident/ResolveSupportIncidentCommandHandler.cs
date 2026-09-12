using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Commands.ResolveSupportIncident;

/// <summary>Investigating -&gt; Resolved. Only the assigned admin may resolve.</summary>
public sealed class ResolveSupportIncidentCommandHandler : ICommandHandler<ResolveSupportIncidentCommand, Result>
{
    private const string NotFoundError = "Incident introuvable.";
    private const string NotEligibleError = "Cet incident n'est pas en cours d'investigation par cet administrateur.";
    private const string ResolutionRequiredError = "Une résolution est requise.";

    private static readonly SupportIncidentStatus[] AllowedFromStatuses = [SupportIncidentStatus.Investigating];

    private readonly ISupportIncidentRepository _incidentRepository;

    public ResolveSupportIncidentCommandHandler(ISupportIncidentRepository incidentRepository)
    {
        _incidentRepository = incidentRepository;
    }

    public async Task<Result> Handle(ResolveSupportIncidentCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Resolution))
        {
            return Result.Failure(ResolutionRequiredError, ErrorType.Validation);
        }

        var incident = await _incidentRepository.GetByIdAsync(command.IncidentId, cancellationToken);

        if (incident is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var transitioned = await _incidentRepository.TryTransitionAsync(
            command.IncidentId, AllowedFromStatuses, SupportIncidentStatus.Resolved, command.AdminUserId, command.Resolution,
            DateTime.UtcNow, cancellationToken);

        return transitioned ? Result.Success() : Result.Failure(NotEligibleError, ErrorType.Conflict);
    }
}

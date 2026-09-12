using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Commands.InvestigateSupportIncident;

/// <summary>Acknowledged -&gt; Investigating. Only the assigned admin may start investigating.</summary>
public sealed class InvestigateSupportIncidentCommandHandler : ICommandHandler<InvestigateSupportIncidentCommand, Result>
{
    private const string NotFoundError = "Incident introuvable.";
    private const string NotEligibleError = "Cet incident n'est pas en attente d'investigation par cet administrateur.";

    private static readonly SupportIncidentStatus[] AllowedFromStatuses = [SupportIncidentStatus.Acknowledged];

    private readonly ISupportIncidentRepository _incidentRepository;

    public InvestigateSupportIncidentCommandHandler(ISupportIncidentRepository incidentRepository)
    {
        _incidentRepository = incidentRepository;
    }

    public async Task<Result> Handle(InvestigateSupportIncidentCommand command, CancellationToken cancellationToken)
    {
        var incident = await _incidentRepository.GetByIdAsync(command.IncidentId, cancellationToken);

        if (incident is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var transitioned = await _incidentRepository.TryTransitionAsync(
            command.IncidentId, AllowedFromStatuses, SupportIncidentStatus.Investigating, command.AdminUserId, resolution: null,
            DateTime.UtcNow, cancellationToken);

        return transitioned ? Result.Success() : Result.Failure(NotEligibleError, ErrorType.Conflict);
    }
}

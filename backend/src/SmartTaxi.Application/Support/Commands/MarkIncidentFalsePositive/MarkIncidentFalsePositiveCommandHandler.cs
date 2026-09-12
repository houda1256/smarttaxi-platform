using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Commands.MarkIncidentFalsePositive;

/// <summary>Reported/Acknowledged/Investigating -&gt; FalsePositive. Terminal, no reopen path (approved design): a genuinely wrong report is re-reported, not resurrected.</summary>
public sealed class MarkIncidentFalsePositiveCommandHandler : ICommandHandler<MarkIncidentFalsePositiveCommand, Result>
{
    private const string NotFoundError = "Incident introuvable.";
    private const string NotEligibleError = "Cet incident ne peut plus être marqué comme faux positif.";

    private static readonly SupportIncidentStatus[] AllowedFromStatuses =
    [
        SupportIncidentStatus.Reported, SupportIncidentStatus.Acknowledged, SupportIncidentStatus.Investigating
    ];

    private readonly ISupportIncidentRepository _incidentRepository;

    public MarkIncidentFalsePositiveCommandHandler(ISupportIncidentRepository incidentRepository)
    {
        _incidentRepository = incidentRepository;
    }

    public async Task<Result> Handle(MarkIncidentFalsePositiveCommand command, CancellationToken cancellationToken)
    {
        var incident = await _incidentRepository.GetByIdAsync(command.IncidentId, cancellationToken);

        if (incident is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var transitioned = await _incidentRepository.TryTransitionAsync(
            command.IncidentId, AllowedFromStatuses, SupportIncidentStatus.FalsePositive, requiredAdminUserId: null, resolution: null,
            DateTime.UtcNow, cancellationToken);

        return transitioned ? Result.Success() : Result.Failure(NotEligibleError, ErrorType.Conflict);
    }
}

using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Support.Abstractions;

namespace SmartTaxi.Application.Support.Commands.EscalateTicketToIncident;

/// <summary>Delegates entirely to ISupportTicketEscalationRepository, whose single atomic transaction guards SupportTicket.EscalatedIncidentId IS NULL and creates the SupportIncident — see that interface's own remarks for the full idempotency argument.</summary>
public sealed class EscalateTicketToIncidentCommandHandler : ICommandHandler<EscalateTicketToIncidentCommand, Result<Guid>>
{
    private const string NotFoundError = "Ticket introuvable.";

    private readonly ISupportTicketEscalationRepository _escalationRepository;

    public EscalateTicketToIncidentCommandHandler(ISupportTicketEscalationRepository escalationRepository)
    {
        _escalationRepository = escalationRepository;
    }

    public async Task<Result<Guid>> Handle(EscalateTicketToIncidentCommand command, CancellationToken cancellationToken)
    {
        var incidentId = await _escalationRepository.TryEscalateAsync(
            command.TicketId, command.AdminUserId, command.IncidentType, command.Severity, command.TitleOverride, DateTime.UtcNow,
            cancellationToken);

        return incidentId is null
            ? Result<Guid>.Failure(NotFoundError, ErrorType.NotFound)
            : Result<Guid>.Success(incidentId.Value);
    }
}

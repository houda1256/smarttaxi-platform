using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Commands.EscalateTicketToIncident;

public sealed record EscalateTicketToIncidentCommand(
    Guid TicketId, Guid AdminUserId, SupportIncidentType IncidentType, SupportIncidentSeverity Severity, string? TitleOverride)
    : ICommand<Result<Guid>>;

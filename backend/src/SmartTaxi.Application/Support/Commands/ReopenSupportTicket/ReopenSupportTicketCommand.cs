using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Support.Commands.ReopenSupportTicket;

public sealed record ReopenSupportTicketCommand(Guid TicketId, Guid CallerUserId) : ICommand<Result>;

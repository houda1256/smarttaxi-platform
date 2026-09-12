using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Support.Commands.CloseSupportTicket;

public sealed record CloseSupportTicketCommand(Guid TicketId, Guid CallerUserId) : ICommand<Result>;

using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Support.Commands.StartSupportTicket;

public sealed record StartSupportTicketCommand(Guid TicketId, Guid AdminUserId) : ICommand<Result>;

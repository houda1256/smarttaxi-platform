using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Support.Commands.AssignSupportTicket;

public sealed record AssignSupportTicketCommand(Guid TicketId, Guid AdminUserId) : ICommand<Result>;

using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Support.Commands.ResolveSupportTicket;

public sealed record ResolveSupportTicketCommand(Guid TicketId, Guid AdminUserId, string Resolution) : ICommand<Result>;

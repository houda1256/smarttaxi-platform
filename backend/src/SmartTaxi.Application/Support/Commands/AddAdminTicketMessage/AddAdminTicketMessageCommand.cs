using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Support.Commands.AddAdminTicketMessage;

public sealed record AddAdminTicketMessageCommand(Guid TicketId, Guid AdminUserId, string Body) : ICommand<Result>;

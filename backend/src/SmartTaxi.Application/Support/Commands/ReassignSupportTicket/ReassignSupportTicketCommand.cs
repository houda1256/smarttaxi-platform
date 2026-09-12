using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Support.Commands.ReassignSupportTicket;

public sealed record ReassignSupportTicketCommand(Guid TicketId, Guid NewAdminUserId) : ICommand<Result>;

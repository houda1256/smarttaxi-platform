using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Support.Commands.AddTicketMessage;

public sealed record AddTicketMessageCommand(Guid TicketId, Guid RequesterUserId, string Body) : ICommand<Result>;

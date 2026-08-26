using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Support.Commands.MarkWaitingForCustomer;

public sealed record MarkWaitingForCustomerCommand(Guid TicketId, Guid AdminUserId) : ICommand<Result>;

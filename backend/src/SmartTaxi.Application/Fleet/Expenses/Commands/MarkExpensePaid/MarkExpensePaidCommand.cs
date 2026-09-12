using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Expenses.Commands.MarkExpensePaid;

public sealed record MarkExpensePaidCommand(Guid RequestingUserId, Guid ExpenseId) : ICommand<Result>;

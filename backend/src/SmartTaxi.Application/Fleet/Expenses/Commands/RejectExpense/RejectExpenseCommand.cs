using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Expenses.Commands.RejectExpense;

public sealed record RejectExpenseCommand(Guid RequestingUserId, Guid ExpenseId) : ICommand<Result>;

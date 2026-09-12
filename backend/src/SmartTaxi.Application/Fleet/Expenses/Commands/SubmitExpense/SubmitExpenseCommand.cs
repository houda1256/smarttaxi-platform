using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Expenses.Commands.SubmitExpense;

public sealed record SubmitExpenseCommand(Guid RequestingUserId, Guid ExpenseId) : ICommand<Result>;

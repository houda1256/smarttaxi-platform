using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Expenses.Commands.ApproveExpense;

public sealed record ApproveExpenseCommand(Guid RequestingUserId, Guid ExpenseId) : ICommand<Result>;

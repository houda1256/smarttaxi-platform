using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Expenses.Abstractions;
using SmartTaxi.Domain.Fleet.Expenses.Enums;

namespace SmartTaxi.Application.Fleet.Expenses.Commands.MarkExpensePaid;

public sealed class MarkExpensePaidCommandHandler : ICommandHandler<MarkExpensePaidCommand, Result>
{
    private const string NotFoundError = "Dépense introuvable.";
    private const string NotApprovedError = "Seule une dépense approuvée peut être marquée comme payée.";

    private readonly IFleetExpenseRepository _repository;

    public MarkExpensePaidCommandHandler(IFleetExpenseRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(MarkExpensePaidCommand command, CancellationToken cancellationToken)
    {
        var expense = await _repository.GetByIdAsync(command.ExpenseId, cancellationToken);

        if (expense is null || expense.OwnerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (expense.Status != ExpenseStatus.Approved)
        {
            return Result.Failure(NotApprovedError, ErrorType.Conflict);
        }

        var paid = await _repository.TryMarkPaidAsync(expense.Id, DateTime.UtcNow, cancellationToken);

        if (!paid)
        {
            return Result.Failure(NotApprovedError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}

using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Expenses.Abstractions;
using SmartTaxi.Domain.Fleet.Expenses.Enums;

namespace SmartTaxi.Application.Fleet.Expenses.Commands.ApproveExpense;

public sealed class ApproveExpenseCommandHandler : ICommandHandler<ApproveExpenseCommand, Result>
{
    private const string NotFoundError = "Dépense introuvable.";
    private const string NotSubmittedError = "Cette dépense n'est pas en attente d'approbation.";

    private readonly IFleetExpenseRepository _repository;

    public ApproveExpenseCommandHandler(IFleetExpenseRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(ApproveExpenseCommand command, CancellationToken cancellationToken)
    {
        var expense = await _repository.GetByIdAsync(command.ExpenseId, cancellationToken);

        if (expense is null || expense.OwnerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (expense.Status != ExpenseStatus.Submitted)
        {
            return Result.Failure(NotSubmittedError, ErrorType.Conflict);
        }

        var approved = await _repository.TryApproveAsync(expense.Id, DateTime.UtcNow, cancellationToken);

        if (!approved)
        {
            return Result.Failure(NotSubmittedError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}

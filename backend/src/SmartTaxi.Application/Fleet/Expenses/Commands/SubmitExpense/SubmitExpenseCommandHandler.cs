using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Expenses.Abstractions;
using SmartTaxi.Domain.Fleet.Expenses.Enums;

namespace SmartTaxi.Application.Fleet.Expenses.Commands.SubmitExpense;

public sealed class SubmitExpenseCommandHandler : ICommandHandler<SubmitExpenseCommand, Result>
{
    private const string NotFoundError = "Dépense introuvable.";
    private const string NotDraftError = "Cette dépense n'est pas à l'état de brouillon.";

    private readonly IFleetExpenseRepository _repository;

    public SubmitExpenseCommandHandler(IFleetExpenseRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(SubmitExpenseCommand command, CancellationToken cancellationToken)
    {
        var expense = await _repository.GetByIdAsync(command.ExpenseId, cancellationToken);

        if (expense is null || expense.OwnerId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (expense.Status != ExpenseStatus.Draft)
        {
            return Result.Failure(NotDraftError, ErrorType.Conflict);
        }

        var submitted = await _repository.TrySubmitAsync(expense.Id, DateTime.UtcNow, cancellationToken);

        if (!submitted)
        {
            return Result.Failure(NotDraftError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}

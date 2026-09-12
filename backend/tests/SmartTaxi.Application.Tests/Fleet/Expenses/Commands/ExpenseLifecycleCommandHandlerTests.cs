using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Expenses.Commands.ApproveExpense;
using SmartTaxi.Application.Fleet.Expenses.Commands.CreateExpense;
using SmartTaxi.Application.Fleet.Expenses.Commands.MarkExpensePaid;
using SmartTaxi.Application.Fleet.Expenses.Commands.RejectExpense;
using SmartTaxi.Application.Fleet.Expenses.Commands.SubmitExpense;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Expenses.Enums;

namespace SmartTaxi.Application.Tests.Fleet.Expenses.Commands;

public class ExpenseLifecycleCommandHandlerTests
{
    private readonly FakeFleetRepository _fleetRepository = new();
    private readonly FakeFleetExpenseRepository _repository = new();
    private readonly CreateExpenseCommandHandler _createHandler;
    private readonly SubmitExpenseCommandHandler _submitHandler;
    private readonly ApproveExpenseCommandHandler _approveHandler;
    private readonly RejectExpenseCommandHandler _rejectHandler;
    private readonly MarkExpensePaidCommandHandler _markPaidHandler;

    public ExpenseLifecycleCommandHandlerTests()
    {
        _createHandler = new CreateExpenseCommandHandler(_fleetRepository, _repository);
        _submitHandler = new SubmitExpenseCommandHandler(_repository);
        _approveHandler = new ApproveExpenseCommandHandler(_repository);
        _rejectHandler = new RejectExpenseCommandHandler(_repository);
        _markPaidHandler = new MarkExpensePaidCommandHandler(_repository);
    }

    private async Task<(Guid OwnerId, Guid ExpenseId)> CreateExpenseAsync()
    {
        var ownerId = Guid.NewGuid();
        var result = await _createHandler.Handle(
            new CreateExpenseCommand(ownerId, null, null, null, ExpenseCategory.Fuel, 50m, "MAD", new DateOnly(2026, 1, 1), null, null),
            CancellationToken.None);
        return (ownerId, result.Value);
    }

    [Fact]
    public async Task Handle_FullLifecycle_SubmitApproveMarkPaid_Succeeds()
    {
        var (ownerId, expenseId) = await CreateExpenseAsync();

        var submitResult = await _submitHandler.Handle(new SubmitExpenseCommand(ownerId, expenseId), CancellationToken.None);
        Assert.True(submitResult.IsSuccess);

        var approveResult = await _approveHandler.Handle(new ApproveExpenseCommand(ownerId, expenseId), CancellationToken.None);
        Assert.True(approveResult.IsSuccess);

        var markPaidResult = await _markPaidHandler.Handle(new MarkExpensePaidCommand(ownerId, expenseId), CancellationToken.None);
        Assert.True(markPaidResult.IsSuccess);

        var expense = await _repository.GetByIdAsync(expenseId, CancellationToken.None);
        Assert.Equal(ExpenseStatus.Paid, expense!.Status);
    }

    [Fact]
    public async Task Handle_Reject_FromSubmitted_SetsRejectedStatus()
    {
        var (ownerId, expenseId) = await CreateExpenseAsync();
        await _submitHandler.Handle(new SubmitExpenseCommand(ownerId, expenseId), CancellationToken.None);

        var result = await _rejectHandler.Handle(new RejectExpenseCommand(ownerId, expenseId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var expense = await _repository.GetByIdAsync(expenseId, CancellationToken.None);
        Assert.Equal(ExpenseStatus.Rejected, expense!.Status);
    }

    [Fact]
    public async Task Handle_MarkPaid_BeforeApproval_ReturnsConflict()
    {
        var (ownerId, expenseId) = await CreateExpenseAsync();
        await _submitHandler.Handle(new SubmitExpenseCommand(ownerId, expenseId), CancellationToken.None);

        var result = await _markPaidHandler.Handle(new MarkExpensePaidCommand(ownerId, expenseId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Handle_ByNonOwner_ReturnsNotFound()
    {
        var (_, expenseId) = await CreateExpenseAsync();

        var result = await _submitHandler.Handle(new SubmitExpenseCommand(Guid.NewGuid(), expenseId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}

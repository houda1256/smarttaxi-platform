using SmartTaxi.Application.Fleet.Expenses.Commands.CreateExpense;
using SmartTaxi.Application.Fleet.Expenses.Queries.ListExpenses;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Expenses.Enums;
using SmartTaxi.Domain.Fleet.Fleets.Entities;

namespace SmartTaxi.Application.Tests.Fleet.Expenses.Queries;

public class ListExpensesQueryHandlerTests
{
    private readonly FakeFleetRepository _fleetRepository = new();
    private readonly FakeFleetExpenseRepository _repository = new();
    private readonly CreateExpenseCommandHandler _createHandler;
    private readonly ListExpensesQueryHandler _handler;

    public ListExpensesQueryHandlerTests()
    {
        _createHandler = new CreateExpenseCommandHandler(_fleetRepository, _repository);
        _handler = new ListExpensesQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_FiltersByCategoryAndPaginates()
    {
        var ownerId = Guid.NewGuid();

        for (var i = 0; i < 3; i++)
        {
            await _createHandler.Handle(
                new CreateExpenseCommand(ownerId, null, null, null, ExpenseCategory.Fuel, 10m + i, "MAD", new DateOnly(2026, 1, 1 + i), null, null),
                CancellationToken.None);
        }

        await _createHandler.Handle(
            new CreateExpenseCommand(ownerId, null, null, null, ExpenseCategory.Maintenance, 500m, "MAD", new DateOnly(2026, 1, 10), null, null),
            CancellationToken.None);

        var page1 = await _handler.Handle(
            new ListExpensesQuery(ownerId, null, null, null, ExpenseCategory.Fuel, null, null, null, 1, 2),
            CancellationToken.None);

        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);

        var page2 = await _handler.Handle(
            new ListExpensesQuery(ownerId, null, null, null, ExpenseCategory.Fuel, null, null, null, 2, 2),
            CancellationToken.None);

        Assert.Single(page2.Items);
    }

    [Fact]
    public async Task Handle_ForDifferentOwner_ReturnsNoResults()
    {
        var ownerId = Guid.NewGuid();
        await _createHandler.Handle(
            new CreateExpenseCommand(ownerId, null, null, null, ExpenseCategory.Fuel, 10m, "MAD", new DateOnly(2026, 1, 1), null, null),
            CancellationToken.None);

        var result = await _handler.Handle(
            new ListExpensesQuery(Guid.NewGuid(), null, null, null, null, null, null, null, 1, 20),
            CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }
}

using SmartTaxi.Domain.Fleet.Expenses.Entities;
using SmartTaxi.Domain.Fleet.Expenses.Enums;
using SmartTaxi.Domain.Fleet.Expenses.Events;
using SmartTaxi.Domain.Fleet.Expenses.ValueObjects;

namespace SmartTaxi.Domain.Tests.Fleet.Expenses.Entities;

public class FleetExpenseTests
{
    [Fact]
    public void Create_SetsDraftStatusAndRaisesEvent()
    {
        var ownerId = Guid.NewGuid();
        var money = Money.Create(150m, "MAD");

        var expense = FleetExpense.Create(
            ownerId, Guid.NewGuid(), null, null, ExpenseCategory.Fuel, money,
            DateOnly.FromDateTime(DateTime.UtcNow), "Fuel top-up", null, Guid.NewGuid(), DateTime.UtcNow);

        Assert.Equal(ExpenseStatus.Draft, expense.Status);
        Assert.Equal(150m, expense.Money.Amount);
        Assert.Single(expense.DomainEvents);
        Assert.IsType<FleetExpenseCreated>(expense.DomainEvents.Single());
    }
}

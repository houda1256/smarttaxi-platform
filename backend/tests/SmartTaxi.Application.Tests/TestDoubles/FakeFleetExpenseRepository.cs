using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Expenses.Abstractions;
using SmartTaxi.Domain.Fleet.Expenses.Entities;
using SmartTaxi.Domain.Fleet.Expenses.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeFleetExpenseRepository : IFleetExpenseRepository
{
    private readonly Dictionary<Guid, FleetExpense> _expensesById = new();

    public Task AddAsync(FleetExpense expense, CancellationToken cancellationToken)
    {
        _expensesById[expense.Id] = expense;
        return Task.CompletedTask;
    }

    public Task<FleetExpense?> GetByIdAsync(Guid expenseId, CancellationToken cancellationToken) =>
        Task.FromResult(_expensesById.GetValueOrDefault(expenseId));

    public IReadOnlyCollection<FleetExpense> GetAllForTest() => _expensesById.Values.ToList();

    public Task<PagedResult<FleetExpense>> SearchAsync(
        FleetExpenseFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _expensesById.Values.Where(e => e.OwnerId == filter.OwnerId);

        if (filter.FleetId is not null)
        {
            query = query.Where(e => e.FleetId == filter.FleetId);
        }

        if (filter.VehicleId is not null)
        {
            query = query.Where(e => e.VehicleId == filter.VehicleId);
        }

        if (filter.DriverId is not null)
        {
            query = query.Where(e => e.DriverId == filter.DriverId);
        }

        if (filter.Category is not null)
        {
            query = query.Where(e => e.Category == filter.Category);
        }

        if (filter.Status is not null)
        {
            query = query.Where(e => e.Status == filter.Status);
        }

        if (filter.FromDate is not null)
        {
            query = query.Where(e => e.ExpenseDate >= filter.FromDate);
        }

        if (filter.ToDate is not null)
        {
            query = query.Where(e => e.ExpenseDate <= filter.ToDate);
        }

        var all = query.OrderByDescending(e => e.ExpenseDate).ToList();
        var page = all.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult(new PagedResult<FleetExpense>(page, all.Count, pageNumber, pageSize));
    }

    public Task<bool> TrySubmitAsync(Guid expenseId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(expenseId, ExpenseStatus.Draft, ExpenseStatus.Submitted);

    public Task<bool> TryApproveAsync(Guid expenseId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(expenseId, ExpenseStatus.Submitted, ExpenseStatus.Approved);

    public Task<bool> TryRejectAsync(Guid expenseId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(expenseId, ExpenseStatus.Submitted, ExpenseStatus.Rejected);

    public Task<bool> TryMarkPaidAsync(Guid expenseId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(expenseId, ExpenseStatus.Approved, ExpenseStatus.Paid);

    private Task<bool> TryTransition(Guid expenseId, ExpenseStatus from, ExpenseStatus to)
    {
        if (!_expensesById.TryGetValue(expenseId, out var expense) || expense.Status != from)
        {
            return Task.FromResult(false);
        }

        typeof(FleetExpense).GetProperty(nameof(FleetExpense.Status))!.SetValue(expense, to);
        return Task.FromResult(true);
    }
}

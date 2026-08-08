using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Expenses.Abstractions;
using SmartTaxi.Domain.Fleet.Expenses.Entities;
using SmartTaxi.Domain.Fleet.Expenses.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Fleet.Repositories;

internal sealed class FleetExpenseRepository : IFleetExpenseRepository
{
    private readonly ApplicationDbContext _context;

    public FleetExpenseRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(FleetExpense expense, CancellationToken cancellationToken)
    {
        await _context.FleetExpenses.AddAsync(expense, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<FleetExpense?> GetByIdAsync(Guid expenseId, CancellationToken cancellationToken) =>
        _context.FleetExpenses.FirstOrDefaultAsync(expense => expense.Id == expenseId, cancellationToken);

    public async Task<PagedResult<FleetExpense>> SearchAsync(
        FleetExpenseFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.FleetExpenses.Where(expense => expense.OwnerId == filter.OwnerId);

        if (filter.FleetId is not null)
        {
            query = query.Where(expense => expense.FleetId == filter.FleetId);
        }

        if (filter.VehicleId is not null)
        {
            query = query.Where(expense => expense.VehicleId == filter.VehicleId);
        }

        if (filter.DriverId is not null)
        {
            query = query.Where(expense => expense.DriverId == filter.DriverId);
        }

        if (filter.Category is not null)
        {
            query = query.Where(expense => expense.Category == filter.Category);
        }

        if (filter.Status is not null)
        {
            query = query.Where(expense => expense.Status == filter.Status);
        }

        if (filter.FromDate is not null)
        {
            query = query.Where(expense => expense.ExpenseDate >= filter.FromDate);
        }

        if (filter.ToDate is not null)
        {
            query = query.Where(expense => expense.ExpenseDate <= filter.ToDate);
        }

        query = query.OrderByDescending(expense => expense.ExpenseDate);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<FleetExpense>(items, totalCount, pageNumber, pageSize);
    }

    public Task<bool> TrySubmitAsync(Guid expenseId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(expenseId, ExpenseStatus.Draft, ExpenseStatus.Submitted, cancellationToken);

    public Task<bool> TryApproveAsync(Guid expenseId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(expenseId, ExpenseStatus.Submitted, ExpenseStatus.Approved, cancellationToken);

    public Task<bool> TryRejectAsync(Guid expenseId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(expenseId, ExpenseStatus.Submitted, ExpenseStatus.Rejected, cancellationToken);

    public Task<bool> TryMarkPaidAsync(Guid expenseId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(expenseId, ExpenseStatus.Approved, ExpenseStatus.Paid, cancellationToken);

    private async Task<bool> TryTransitionAsync(
        Guid expenseId, ExpenseStatus from, ExpenseStatus to, CancellationToken cancellationToken)
    {
        var rows = await _context.FleetExpenses
            .Where(expense => expense.Id == expenseId && expense.Status == from)
            .ExecuteUpdateAsync(setters => setters.SetProperty(expense => expense.Status, to), cancellationToken);

        return rows == 1;
    }
}

using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Fleet.Expenses.Entities;

namespace SmartTaxi.Application.Fleet.Expenses.Abstractions;

public interface IFleetExpenseRepository
{
    Task AddAsync(FleetExpense expense, CancellationToken cancellationToken);

    Task<FleetExpense?> GetByIdAsync(Guid expenseId, CancellationToken cancellationToken);

    Task<PagedResult<FleetExpense>> SearchAsync(
        FleetExpenseFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<bool> TrySubmitAsync(Guid expenseId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryApproveAsync(Guid expenseId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryRejectAsync(Guid expenseId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryMarkPaidAsync(Guid expenseId, DateTime utcNow, CancellationToken cancellationToken);
}

using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Expenses.Abstractions;

namespace SmartTaxi.Application.Fleet.Expenses.Queries.ListExpenses;

public sealed class ListExpensesQueryHandler : IQueryHandler<ListExpensesQuery, PagedResult<FleetExpenseSummary>>
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly IFleetExpenseRepository _repository;

    public ListExpensesQueryHandler(IFleetExpenseRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<FleetExpenseSummary>> Handle(ListExpensesQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > MaxPageSize ? DefaultPageSize : query.PageSize;

        var filter = new FleetExpenseFilter(
            query.OwnerId, query.FleetId, query.VehicleId, query.DriverId, query.Category, query.Status,
            query.FromDate, query.ToDate);

        var result = await _repository.SearchAsync(filter, pageNumber, pageSize, cancellationToken);

        return new PagedResult<FleetExpenseSummary>(
            result.Items.Select(FleetExpenseSummary.FromEntity).ToList(), result.TotalCount, pageNumber, pageSize);
    }
}

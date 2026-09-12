using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Analytics.Entities;

namespace SmartTaxi.Application.Analytics.Queries.GetScheduledReports;

public sealed class GetScheduledReportsQueryHandler : IQueryHandler<GetScheduledReportsQuery, PagedResult<ScheduledReportDefinition>>
{
    private readonly IScheduledReportRepository _repository;

    public GetScheduledReportsQueryHandler(IScheduledReportRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<ScheduledReportDefinition>> Handle(GetScheduledReportsQuery query, CancellationToken cancellationToken) =>
        _repository.GetAllAsync(query.PageNumber, query.PageSize, cancellationToken);
}

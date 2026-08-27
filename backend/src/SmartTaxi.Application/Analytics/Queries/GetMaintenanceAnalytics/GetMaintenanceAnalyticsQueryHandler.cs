using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Queries.GetMaintenanceAnalytics;

public sealed class GetMaintenanceAnalyticsQueryHandler : IQueryHandler<GetMaintenanceAnalyticsQuery, Result<MaintenanceAnalyticsSummary>>
{
    private const string InvalidRangeError = "La période demandée est invalide (FromUtc doit précéder ToUtc, sur au plus 366 jours).";

    private readonly IMaintenanceAnalyticsReader _reader;

    public GetMaintenanceAnalyticsQueryHandler(IMaintenanceAnalyticsReader reader)
    {
        _reader = reader;
    }

    public async Task<Result<MaintenanceAnalyticsSummary>> Handle(GetMaintenanceAnalyticsQuery query, CancellationToken cancellationToken)
    {
        if (!AnalyticsDateRange.IsValid(query.FromUtc, query.ToUtc))
        {
            return Result<MaintenanceAnalyticsSummary>.Failure(InvalidRangeError, ErrorType.Validation);
        }

        var result = await _reader.GetSummaryAsync(query.FromUtc, query.ToUtc, cancellationToken);
        return Result<MaintenanceAnalyticsSummary>.Success(result);
    }
}

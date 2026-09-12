using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Queries.GetFleetAnalytics;

public sealed class GetFleetAnalyticsQueryHandler : IQueryHandler<GetFleetAnalyticsQuery, Result<FleetAnalyticsSummary>>
{
    private const string InvalidRangeError = "La période demandée est invalide (FromUtc doit précéder ToUtc, sur au plus 366 jours).";

    private readonly IFleetAnalyticsReader _reader;

    public GetFleetAnalyticsQueryHandler(IFleetAnalyticsReader reader)
    {
        _reader = reader;
    }

    public async Task<Result<FleetAnalyticsSummary>> Handle(GetFleetAnalyticsQuery query, CancellationToken cancellationToken)
    {
        if (!AnalyticsDateRange.IsValid(query.FromUtc, query.ToUtc))
        {
            return Result<FleetAnalyticsSummary>.Failure(InvalidRangeError, ErrorType.Validation);
        }

        var result = await _reader.GetSummaryAsync(query.FromUtc, query.ToUtc, cancellationToken);
        return Result<FleetAnalyticsSummary>.Success(result);
    }
}

using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Queries.GetRideAnalytics;

public sealed class GetRideAnalyticsQueryHandler : IQueryHandler<GetRideAnalyticsQuery, Result<RideAnalyticsSummary>>
{
    private const string InvalidRangeError = "La période demandée est invalide (FromUtc doit précéder ToUtc, sur au plus 366 jours).";

    private readonly IRideAnalyticsReader _reader;

    public GetRideAnalyticsQueryHandler(IRideAnalyticsReader reader)
    {
        _reader = reader;
    }

    public async Task<Result<RideAnalyticsSummary>> Handle(GetRideAnalyticsQuery query, CancellationToken cancellationToken)
    {
        if (!AnalyticsDateRange.IsValid(query.FromUtc, query.ToUtc))
        {
            return Result<RideAnalyticsSummary>.Failure(InvalidRangeError, ErrorType.Validation);
        }

        var result = await _reader.GetSummaryAsync(query.FromUtc, query.ToUtc, cancellationToken);
        return Result<RideAnalyticsSummary>.Success(result);
    }
}

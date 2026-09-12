using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Queries.GetRoadsideAnalytics;

public sealed class GetRoadsideAnalyticsQueryHandler : IQueryHandler<GetRoadsideAnalyticsQuery, Result<RoadsideAnalyticsSummary>>
{
    private const string InvalidRangeError = "La période demandée est invalide (FromUtc doit précéder ToUtc, sur au plus 366 jours).";

    private readonly IRoadsideAnalyticsReader _reader;

    public GetRoadsideAnalyticsQueryHandler(IRoadsideAnalyticsReader reader)
    {
        _reader = reader;
    }

    public async Task<Result<RoadsideAnalyticsSummary>> Handle(GetRoadsideAnalyticsQuery query, CancellationToken cancellationToken)
    {
        if (!AnalyticsDateRange.IsValid(query.FromUtc, query.ToUtc))
        {
            return Result<RoadsideAnalyticsSummary>.Failure(InvalidRangeError, ErrorType.Validation);
        }

        var result = await _reader.GetSummaryAsync(query.FromUtc, query.ToUtc, cancellationToken);
        return Result<RoadsideAnalyticsSummary>.Success(result);
    }
}

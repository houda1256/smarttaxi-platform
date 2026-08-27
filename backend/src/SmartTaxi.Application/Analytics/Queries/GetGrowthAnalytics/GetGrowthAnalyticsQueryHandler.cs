using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Queries.GetGrowthAnalytics;

public sealed class GetGrowthAnalyticsQueryHandler : IQueryHandler<GetGrowthAnalyticsQuery, Result<GrowthAnalyticsResult>>
{
    private const string InvalidRangeError = "La période demandée est invalide (FromUtc doit précéder ToUtc, sur au plus 366 jours).";

    private readonly IGrowthAnalyticsReader _reader;

    public GetGrowthAnalyticsQueryHandler(IGrowthAnalyticsReader reader)
    {
        _reader = reader;
    }

    public async Task<Result<GrowthAnalyticsResult>> Handle(GetGrowthAnalyticsQuery query, CancellationToken cancellationToken)
    {
        if (!AnalyticsDateRange.IsValid(query.FromUtc, query.ToUtc))
        {
            return Result<GrowthAnalyticsResult>.Failure(InvalidRangeError, ErrorType.Validation);
        }

        var result = await _reader.GetGrowthAsync(query.FromUtc, query.ToUtc, cancellationToken);
        return Result<GrowthAnalyticsResult>.Success(result);
    }
}

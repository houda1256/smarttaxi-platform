using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Queries.GetAdvertisingAnalytics;

public sealed class GetAdvertisingAnalyticsQueryHandler : IQueryHandler<GetAdvertisingAnalyticsQuery, Result<AdvertisingAnalyticsSummary>>
{
    private const string InvalidRangeError = "La période demandée est invalide (FromUtc doit précéder ToUtc, sur au plus 366 jours).";

    private readonly IAdvertisingAnalyticsReader _reader;

    public GetAdvertisingAnalyticsQueryHandler(IAdvertisingAnalyticsReader reader)
    {
        _reader = reader;
    }

    public async Task<Result<AdvertisingAnalyticsSummary>> Handle(GetAdvertisingAnalyticsQuery query, CancellationToken cancellationToken)
    {
        if (!AnalyticsDateRange.IsValid(query.FromUtc, query.ToUtc))
        {
            return Result<AdvertisingAnalyticsSummary>.Failure(InvalidRangeError, ErrorType.Validation);
        }

        var result = await _reader.GetSummaryAsync(query.FromUtc, query.ToUtc, cancellationToken);
        return Result<AdvertisingAnalyticsSummary>.Success(result);
    }
}

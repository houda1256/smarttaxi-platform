using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Queries.GetSubscriptionAnalytics;

public sealed class GetSubscriptionAnalyticsQueryHandler : IQueryHandler<GetSubscriptionAnalyticsQuery, Result<SubscriptionAnalyticsSummary>>
{
    private const string InvalidRangeError = "La période demandée est invalide (FromUtc doit précéder ToUtc, sur au plus 366 jours).";

    private readonly ISubscriptionAnalyticsReader _reader;

    public GetSubscriptionAnalyticsQueryHandler(ISubscriptionAnalyticsReader reader)
    {
        _reader = reader;
    }

    public async Task<Result<SubscriptionAnalyticsSummary>> Handle(GetSubscriptionAnalyticsQuery query, CancellationToken cancellationToken)
    {
        if (!AnalyticsDateRange.IsValid(query.FromUtc, query.ToUtc))
        {
            return Result<SubscriptionAnalyticsSummary>.Failure(InvalidRangeError, ErrorType.Validation);
        }

        var result = await _reader.GetSummaryAsync(query.FromUtc, query.ToUtc, cancellationToken);
        return Result<SubscriptionAnalyticsSummary>.Success(result);
    }
}

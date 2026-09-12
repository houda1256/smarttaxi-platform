using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Queries.GetSupportAnalytics;

public sealed class GetSupportAnalyticsQueryHandler : IQueryHandler<GetSupportAnalyticsQuery, Result<SupportAnalyticsSummary>>
{
    private const string InvalidRangeError = "La période demandée est invalide (FromUtc doit précéder ToUtc, sur au plus 366 jours).";

    private readonly ISupportAnalyticsReader _reader;

    public GetSupportAnalyticsQueryHandler(ISupportAnalyticsReader reader)
    {
        _reader = reader;
    }

    public async Task<Result<SupportAnalyticsSummary>> Handle(GetSupportAnalyticsQuery query, CancellationToken cancellationToken)
    {
        if (!AnalyticsDateRange.IsValid(query.FromUtc, query.ToUtc))
        {
            return Result<SupportAnalyticsSummary>.Failure(InvalidRangeError, ErrorType.Validation);
        }

        var result = await _reader.GetSummaryAsync(query.FromUtc, query.ToUtc, cancellationToken);
        return Result<SupportAnalyticsSummary>.Success(result);
    }
}

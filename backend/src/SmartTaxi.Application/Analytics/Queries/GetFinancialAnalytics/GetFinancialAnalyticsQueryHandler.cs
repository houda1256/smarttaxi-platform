using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Queries.GetFinancialAnalytics;

public sealed class GetFinancialAnalyticsQueryHandler : IQueryHandler<GetFinancialAnalyticsQuery, Result<FinancialAnalyticsSummary>>
{
    private const string InvalidRangeError = "La période demandée est invalide (FromUtc doit précéder ToUtc, sur au plus 366 jours).";

    private readonly IFinancialAnalyticsReader _reader;

    public GetFinancialAnalyticsQueryHandler(IFinancialAnalyticsReader reader)
    {
        _reader = reader;
    }

    public async Task<Result<FinancialAnalyticsSummary>> Handle(GetFinancialAnalyticsQuery query, CancellationToken cancellationToken)
    {
        if (!AnalyticsDateRange.IsValid(query.FromUtc, query.ToUtc))
        {
            return Result<FinancialAnalyticsSummary>.Failure(InvalidRangeError, ErrorType.Validation);
        }

        var result = await _reader.GetSummaryAsync(query.FromUtc, query.ToUtc, cancellationToken);
        return Result<FinancialAnalyticsSummary>.Success(result);
    }
}

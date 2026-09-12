using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Abstractions;

namespace SmartTaxi.Application.Payments.Queries.GetRevenueSummary;

/// <summary>
/// Read-model aggregation over confirmed Payments — no general ledger exists
/// (out of scope per instructions), so this is computed on demand rather
/// than maintained incrementally. Grouped by ConfirmedAt (UTC), the moment
/// revenue was actually recognized, not CreatedAt.
/// </summary>
public sealed class GetRevenueSummaryQueryHandler : IQueryHandler<GetRevenueSummaryQuery, IReadOnlyCollection<RevenuePeriodSummary>>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetRevenueSummaryQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<IReadOnlyCollection<RevenuePeriodSummary>> Handle(GetRevenueSummaryQuery query, CancellationToken cancellationToken)
    {
        var payments = await _paymentRepository.GetConfirmedBetweenAsync(query.FromDate, query.ToDate, cancellationToken);

        var groups = payments.GroupBy(p => query.Granularity == RevenueReportGranularity.Daily
            ? new DateTime(p.ConfirmedAt!.Value.Year, p.ConfirmedAt.Value.Month, p.ConfirmedAt.Value.Day, 0, 0, 0, DateTimeKind.Utc)
            : new DateTime(p.ConfirmedAt!.Value.Year, p.ConfirmedAt.Value.Month, 1, 0, 0, 0, DateTimeKind.Utc));

        return groups
            .Select(g => new RevenuePeriodSummary(
                g.Key,
                g.Sum(p => p.FinalFareAmount),
                g.Sum(p => p.PlatformCommissionAmount ?? 0m),
                g.Sum(p => p.DriverAmount ?? 0m),
                g.Sum(p => p.OwnerAmount ?? 0m),
                g.Sum(p => p.RefundedAmount),
                g.Count()))
            .OrderBy(r => r.PeriodStart)
            .ToList();
    }
}

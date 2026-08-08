using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Abstractions;

namespace SmartTaxi.Application.Payments.Queries.GetOwnerRevenueReport;

public sealed class GetOwnerRevenueReportQueryHandler : IQueryHandler<GetOwnerRevenueReportQuery, OwnerRevenueReport>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetOwnerRevenueReportQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<OwnerRevenueReport> Handle(GetOwnerRevenueReportQuery query, CancellationToken cancellationToken)
    {
        var payments = await _paymentRepository.GetConfirmedBetweenAsync(query.FromDate, query.ToDate, cancellationToken);
        var forOwner = payments.Where(p => p.OwnerId == query.OwnerId).ToList();

        return new OwnerRevenueReport(
            query.OwnerId, forOwner.Sum(p => p.OwnerAmount ?? 0m), forOwner.Count, query.FromDate, query.ToDate);
    }
}

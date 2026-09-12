using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Domain.Payments.Enums;

namespace SmartTaxi.Application.Payments.Queries.GetPaymentStatistics;

public sealed class GetPaymentStatisticsQueryHandler : IQueryHandler<GetPaymentStatisticsQuery, PaymentStatisticsReport>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentStatisticsQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<PaymentStatisticsReport> Handle(GetPaymentStatisticsQuery query, CancellationToken cancellationToken)
    {
        var payments = await _paymentRepository.GetCreatedBetweenAsync(query.FromDate, query.ToDate, cancellationToken);
        var settled = payments.Where(p => p.Status is PaymentStatus.Paid or PaymentStatus.PartiallyRefunded or PaymentStatus.Refunded).ToList();

        return new PaymentStatisticsReport(
            payments.Count,
            payments.Count(p => p.Status == PaymentStatus.Pending),
            payments.Count(p => p.Status == PaymentStatus.Authorized),
            payments.Count(p => p.Status == PaymentStatus.Paid),
            payments.Count(p => p.Status == PaymentStatus.Failed),
            payments.Count(p => p.Status == PaymentStatus.Cancelled),
            payments.Count(p => p.Status == PaymentStatus.Refunded),
            payments.Count(p => p.Status == PaymentStatus.PartiallyRefunded),
            settled.Sum(p => p.FinalFareAmount),
            settled.Sum(p => p.RefundedAmount),
            settled.Count > 0 ? Math.Round(settled.Average(p => p.FinalFareAmount), 2) : 0m,
            query.FromDate,
            query.ToDate);
    }
}

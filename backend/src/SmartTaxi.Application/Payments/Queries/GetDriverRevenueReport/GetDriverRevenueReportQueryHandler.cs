using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Abstractions;

namespace SmartTaxi.Application.Payments.Queries.GetDriverRevenueReport;

public sealed class GetDriverRevenueReportQueryHandler : IQueryHandler<GetDriverRevenueReportQuery, DriverRevenueReport>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetDriverRevenueReportQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<DriverRevenueReport> Handle(GetDriverRevenueReportQuery query, CancellationToken cancellationToken)
    {
        var payments = await _paymentRepository.GetConfirmedBetweenAsync(query.FromDate, query.ToDate, cancellationToken);
        var forDriver = payments.Where(p => p.DriverId == query.DriverId).ToList();

        return new DriverRevenueReport(
            query.DriverId, forDriver.Sum(p => p.DriverAmount ?? 0m), forDriver.Count, query.FromDate, query.ToDate);
    }
}

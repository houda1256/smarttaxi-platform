using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Abstractions;

namespace SmartTaxi.Application.Payments.Queries.GetAdminPayments;

public sealed class GetAdminPaymentsQueryHandler : IQueryHandler<GetAdminPaymentsQuery, PagedResult<PaymentSummary>>
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly IPaymentRepository _paymentRepository;

    public GetAdminPaymentsQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<PagedResult<PaymentSummary>> Handle(GetAdminPaymentsQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > MaxPageSize ? DefaultPageSize : query.PageSize;

        var filter = new PaymentFilter(query.CustomerId, query.DriverId, query.OwnerId, query.Status, query.FromDate, query.ToDate);
        var result = await _paymentRepository.SearchAsync(filter, pageNumber, pageSize, cancellationToken);

        var summaries = result.Items.Select(PaymentSummary.FromEntity).ToList();
        return new PagedResult<PaymentSummary>(summaries, result.TotalCount, result.PageNumber, result.PageSize);
    }
}

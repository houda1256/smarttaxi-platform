using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Payouts.Abstractions;
using SmartTaxi.Domain.Payments.Payouts.Entities;

namespace SmartTaxi.Application.Payments.Payouts.Queries.GetMyPayouts;

public sealed class GetMyPayoutsQueryHandler : IQueryHandler<GetMyPayoutsQuery, PagedResult<Payout>>
{
    private readonly IPayoutRepository _payoutRepository;

    public GetMyPayoutsQueryHandler(IPayoutRepository payoutRepository)
    {
        _payoutRepository = payoutRepository;
    }

    public Task<PagedResult<Payout>> Handle(GetMyPayoutsQuery query, CancellationToken cancellationToken) =>
        _payoutRepository.GetForBeneficiaryAccountAsync(query.BeneficiaryAccountId, query.Status, query.PageNumber, query.PageSize, cancellationToken);
}

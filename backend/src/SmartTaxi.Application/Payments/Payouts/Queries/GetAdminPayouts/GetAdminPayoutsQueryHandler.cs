using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Payouts.Abstractions;
using SmartTaxi.Domain.Payments.Payouts.Entities;

namespace SmartTaxi.Application.Payments.Payouts.Queries.GetAdminPayouts;

public sealed class GetAdminPayoutsQueryHandler : IQueryHandler<GetAdminPayoutsQuery, PagedResult<Payout>>
{
    private readonly IPayoutRepository _payoutRepository;

    public GetAdminPayoutsQueryHandler(IPayoutRepository payoutRepository)
    {
        _payoutRepository = payoutRepository;
    }

    public Task<PagedResult<Payout>> Handle(GetAdminPayoutsQuery query, CancellationToken cancellationToken) =>
        _payoutRepository.GetForAdminAsync(
            query.BeneficiaryType, query.Status, query.FromUtc, query.ToUtc, query.PageNumber, query.PageSize, cancellationToken);
}

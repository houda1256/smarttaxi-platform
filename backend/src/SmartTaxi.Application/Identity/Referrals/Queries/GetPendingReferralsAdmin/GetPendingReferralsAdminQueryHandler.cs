using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Referrals.Abstractions;

namespace SmartTaxi.Application.Identity.Referrals.Queries.GetPendingReferralsAdmin;

public sealed class GetPendingReferralsAdminQueryHandler
    : IQueryHandler<GetPendingReferralsAdminQuery, IReadOnlyCollection<ReferralSummary>>
{
    private readonly IReferralRepository _repository;

    public GetPendingReferralsAdminQueryHandler(IReferralRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<ReferralSummary>> Handle(
        GetPendingReferralsAdminQuery query, CancellationToken cancellationToken)
    {
        var referrals = await _repository.GetPendingActivationAsync(cancellationToken);

        return referrals.Select(ReferralSummary.FromEntity).ToList();
    }
}

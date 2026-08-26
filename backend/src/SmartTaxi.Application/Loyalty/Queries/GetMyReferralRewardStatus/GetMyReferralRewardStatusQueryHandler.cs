using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Queries.GetMyReferralRewardStatus;

/// <summary>Referral relationships/status are Identity's own (see /api/users/me/referrals); this only surfaces the reward outcome Loyalty granted for referrals where the caller is the referrer.</summary>
public sealed class GetMyReferralRewardStatusQueryHandler : IQueryHandler<GetMyReferralRewardStatusQuery, IReadOnlyCollection<LoyaltyReferralReward>>
{
    private readonly ILoyaltyReferralRewardRepository _referralRewardRepository;

    public GetMyReferralRewardStatusQueryHandler(ILoyaltyReferralRewardRepository referralRewardRepository)
    {
        _referralRewardRepository = referralRewardRepository;
    }

    public Task<IReadOnlyCollection<LoyaltyReferralReward>> Handle(GetMyReferralRewardStatusQuery query, CancellationToken cancellationToken) =>
        _referralRewardRepository.GetForReferrerAsync(query.UserId, cancellationToken);
}

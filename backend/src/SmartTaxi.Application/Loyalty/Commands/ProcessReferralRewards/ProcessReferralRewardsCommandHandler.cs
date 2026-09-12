using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Referrals.Abstractions;

namespace SmartTaxi.Application.Loyalty.Commands.ProcessReferralRewards;

public sealed class ProcessReferralRewardsCommandHandler : ICommandHandler<ProcessReferralRewardsCommand, Result<int>>
{
    private readonly IReferralRepository _referralRepository;
    private readonly LoyaltyReferralRewardGranter _granter;

    public ProcessReferralRewardsCommandHandler(IReferralRepository referralRepository, LoyaltyReferralRewardGranter granter)
    {
        _referralRepository = referralRepository;
        _granter = granter;
    }

    public async Task<Result<int>> Handle(ProcessReferralRewardsCommand command, CancellationToken cancellationToken)
    {
        var rewardEligible = await _referralRepository.GetRewardEligibleAsync(cancellationToken);
        var grantedCount = 0;

        foreach (var referral in rewardEligible)
        {
            if (await _granter.TryGrantIfEligibleAsync(referral, cancellationToken))
            {
                grantedCount++;
            }
        }

        return Result<int>.Success(grantedCount);
    }
}

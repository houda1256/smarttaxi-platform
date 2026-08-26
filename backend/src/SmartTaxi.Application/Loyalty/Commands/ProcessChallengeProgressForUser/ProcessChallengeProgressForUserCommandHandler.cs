using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Application.Loyalty.Contracts;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Loyalty.Commands.ProcessChallengeProgressForUser;

/// <summary>
/// Both criteria are derived from Loyalty's own ledger/reward data (never from
/// Rides/Identity directly): RideCount counts this user's own Earn entries
/// sourced from a Payment; ReferralCount counts LoyaltyReferralReward rows
/// where the user is the referrer. Progress values only ever grow (the
/// underlying counts are monotonic), so a lower recount is simply ignored
/// rather than attempted as a regression.
/// </summary>
public sealed class ProcessChallengeProgressForUserCommandHandler : ICommandHandler<ProcessChallengeProgressForUserCommand, Result<int>>
{
    private const string PaymentSourceType = "Payment";
    private const string ChallengeSourceType = "Challenge";

    private readonly ILoyaltyChallengeRepository _challengeRepository;
    private readonly ILoyaltyChallengeProgressRepository _progressRepository;
    private readonly ILoyaltyPointLedgerRepository _ledgerRepository;
    private readonly ILoyaltyReferralRewardRepository _referralRewardRepository;
    private readonly ILoyaltyAccountRepository _accountRepository;
    private readonly ILoyaltyTierThresholdRepository _tierThresholdRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public ProcessChallengeProgressForUserCommandHandler(
        ILoyaltyChallengeRepository challengeRepository, ILoyaltyChallengeProgressRepository progressRepository,
        ILoyaltyPointLedgerRepository ledgerRepository, ILoyaltyReferralRewardRepository referralRewardRepository,
        ILoyaltyAccountRepository accountRepository, ILoyaltyTierThresholdRepository tierThresholdRepository,
        INotificationDispatcher notificationDispatcher)
    {
        _challengeRepository = challengeRepository;
        _progressRepository = progressRepository;
        _ledgerRepository = ledgerRepository;
        _referralRewardRepository = referralRewardRepository;
        _accountRepository = accountRepository;
        _tierThresholdRepository = tierThresholdRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result<int>> Handle(ProcessChallengeProgressForUserCommand command, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var challenges = await _challengeRepository.GetActiveForRoleAsync(command.Role, utcNow, cancellationToken);
        var rewardedCount = 0;

        foreach (var challenge in challenges)
        {
            var currentValue = challenge.CriteriaType switch
            {
                LoyaltyChallengeCriteriaType.RideCount =>
                    await _ledgerRepository.CountEntriesAsync(command.UserId, PaymentSourceType, LoyaltyLedgerEntryType.Earn, cancellationToken),
                LoyaltyChallengeCriteriaType.ReferralCount =>
                    (await _referralRewardRepository.GetForReferrerAsync(command.UserId, cancellationToken)).Count,
                _ => 0
            };

            var progress = await _progressRepository.GetByUserAndChallengeAsync(command.UserId, challenge.Id, cancellationToken);

            if (progress is null)
            {
                progress = LoyaltyChallengeProgress.Start(challenge.Id, command.UserId, utcNow);

                if (!await _progressRepository.TryAddAsync(progress, cancellationToken))
                {
                    progress = await _progressRepository.GetByUserAndChallengeAsync(command.UserId, challenge.Id, cancellationToken);
                }
            }

            if (progress is null || progress.IsRewarded)
            {
                continue;
            }

            if (currentValue > progress.CurrentValue)
            {
                progress.UpdateProgress(currentValue, challenge.TargetValue, utcNow);
                await _progressRepository.UpdateAsync(progress, cancellationToken);
            }

            if (!progress.IsCompleted || progress.IsRewarded)
            {
                continue;
            }

            var account = await _accountRepository.GetByUserIdAsync(command.UserId, cancellationToken);

            if (account is null)
            {
                continue;
            }

            var thresholds = await _tierThresholdRepository.GetAllAsync(cancellationToken);

            var credited = await _ledgerRepository.TryCreditAsync(
                new LoyaltyLedgerAppendRequest(
                    account.Id, account.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.ChallengeReward, challenge.RewardPoints,
                    ChallengeSourceType, progress.Id, $"Défi complété ({challenge.Code})"),
                thresholds, utcNow, cancellationToken);

            if (credited is null)
            {
                continue;
            }

            progress.MarkRewarded(utcNow);
            await _progressRepository.UpdateAsync(progress, cancellationToken);
            rewardedCount++;

            await _notificationDispatcher.DispatchAsync(
                new NotificationRequest(
                    account.UserId, NotificationCategory.Loyalty, "loyalty.challenge-completed",
                    new Dictionary<string, string> { ["ChallengeCode"] = challenge.Code, ["Points"] = challenge.RewardPoints.ToString() },
                    IsMandatory: false, SourceType: ChallengeSourceType, SourceId: progress.Id),
                cancellationToken);
        }

        return Result<int>.Success(rewardedCount);
    }
}

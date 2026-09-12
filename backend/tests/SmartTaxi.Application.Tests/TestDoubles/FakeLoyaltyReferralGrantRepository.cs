using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Application.Loyalty.Contracts;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>
/// In-memory approximation of the real all-or-nothing grant transaction.
/// Shares its "reward already granted" state with the SAME
/// FakeLoyaltyReferralRewardRepository instance the granter uses for its own
/// pre-check — exactly mirroring how the real repositories both read/write
/// the one LoyaltyReferralRewards table — so a failed or skipped grant is
/// correctly visible as still-ungranted on the next attempt.
/// </summary>
public sealed class FakeLoyaltyReferralGrantRepository : ILoyaltyReferralGrantRepository
{
    private readonly FakeLoyaltyAccountRepository _accountRepository;
    private readonly FakeLoyaltyReferralRewardRepository _referralRewardRepository;

    /// <summary>Test hook: when set, TryGrantAsync fails (as if a genuine DB fault occurred) without mutating anything — used to prove the caller can safely retry afterward.</summary>
    public bool FailNextAttempt { get; set; }

    public FakeLoyaltyReferralGrantRepository(FakeLoyaltyAccountRepository accountRepository, FakeLoyaltyReferralRewardRepository referralRewardRepository)
    {
        _accountRepository = accountRepository;
        _referralRewardRepository = referralRewardRepository;
    }

    public async Task<bool> TryGrantAsync(
        LoyaltyReferralReward reward, LoyaltyLedgerAppendRequest? referrerCreditRequest, LoyaltyLedgerAppendRequest? refereeCreditRequest,
        DateTime utcNow, CancellationToken cancellationToken)
    {
        if (FailNextAttempt)
        {
            FailNextAttempt = false;
            return false;
        }

        if (!await _referralRewardRepository.TryAddAsync(reward, cancellationToken))
        {
            return false;
        }

        if (referrerCreditRequest is not null)
        {
            var account = await _accountRepository.GetByIdAsync(referrerCreditRequest.AccountId, cancellationToken);
            account?.ApplyRewardPointsDelta(referrerCreditRequest.Points, utcNow);
        }

        if (refereeCreditRequest is not null)
        {
            var account = await _accountRepository.GetByIdAsync(refereeCreditRequest.AccountId, cancellationToken);
            account?.ApplyRewardPointsDelta(refereeCreditRequest.Points, utcNow);
        }

        return true;
    }
}

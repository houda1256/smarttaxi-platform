using SmartTaxi.Application.Identity.Referrals.Abstractions;
using SmartTaxi.Domain.Identity.Referrals.Entities;
using SmartTaxi.Domain.Identity.Referrals.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeReferralRepository : IReferralRepository
{
    private readonly Dictionary<Guid, Referral> _referralsById = new();

    public Task<bool> AddAsync(Referral referral, CancellationToken cancellationToken)
    {
        if (_referralsById.Values.Any(r => r.RefereeUserId == referral.RefereeUserId))
        {
            return Task.FromResult(false);
        }

        _referralsById[referral.Id] = referral;
        return Task.FromResult(true);
    }

    public Task<Referral?> GetByIdAsync(Guid referralId, CancellationToken cancellationToken) =>
        Task.FromResult(_referralsById.GetValueOrDefault(referralId));

    public Task<Referral?> GetByRefereeUserIdAsync(Guid refereeUserId, CancellationToken cancellationToken) =>
        Task.FromResult(_referralsById.Values.FirstOrDefault(r => r.RefereeUserId == refereeUserId));

    public Task<IReadOnlyCollection<Referral>> GetForReferrerAsync(Guid referrerUserId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Referral> referrals = _referralsById.Values.Where(r => r.ReferrerUserId == referrerUserId).ToList();
        return Task.FromResult(referrals);
    }

    public Task<IReadOnlyCollection<Referral>> GetPendingActivationAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Referral> referrals =
            _referralsById.Values.Where(r => r.Status == ReferralStatus.PendingActivation).ToList();
        return Task.FromResult(referrals);
    }

    public Task<IReadOnlyCollection<Referral>> GetRewardEligibleAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Referral> referrals =
            _referralsById.Values.Where(r => r.Status == ReferralStatus.RewardEligible).ToList();
        return Task.FromResult(referrals);
    }

    public Task<bool> TryActivateAsync(Guid referralId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_referralsById.TryGetValue(referralId, out var referral) || referral.Status != ReferralStatus.PendingActivation)
        {
            return Task.FromResult(false);
        }

        typeof(Referral).GetProperty(nameof(Referral.Status))!.SetValue(referral, ReferralStatus.RewardEligible);
        typeof(Referral).GetProperty(nameof(Referral.ActivatedAt))!.SetValue(referral, utcNow);
        typeof(Referral).GetProperty(nameof(Referral.UpdatedAt))!.SetValue(referral, utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryInvalidateAsync(Guid referralId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_referralsById.TryGetValue(referralId, out var referral) || referral.Status == ReferralStatus.Invalidated)
        {
            return Task.FromResult(false);
        }

        typeof(Referral).GetProperty(nameof(Referral.Status))!.SetValue(referral, ReferralStatus.Invalidated);
        typeof(Referral).GetProperty(nameof(Referral.UpdatedAt))!.SetValue(referral, utcNow);
        return Task.FromResult(true);
    }

    public int Count => _referralsById.Count;
}

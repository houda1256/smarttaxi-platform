using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Loyalty.Events;

namespace SmartTaxi.Domain.Loyalty.Entities;

/// <summary>One row per (UserId, ChallengeId) — unique-constrained so a user can only ever have one progress row per challenge (see LoyaltyChallengeProgressConfiguration).</summary>
public sealed class LoyaltyChallengeProgress : AggregateRoot
{
    public Guid ChallengeId { get; private set; }
    public Guid UserId { get; private set; }
    public int CurrentValue { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? RewardedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public bool IsCompleted => CompletedAtUtc is not null;

    public bool IsRewarded => RewardedAtUtc is not null;

    private LoyaltyChallengeProgress()
    {
    }

    private LoyaltyChallengeProgress(Guid challengeId, Guid userId, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        ChallengeId = challengeId;
        UserId = userId;
        CurrentValue = 0;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public static LoyaltyChallengeProgress Start(Guid challengeId, Guid userId, DateTime utcNow)
    {
        if (challengeId == Guid.Empty || userId == Guid.Empty)
        {
            throw new ArgumentException("Le défi et l'utilisateur sont requis.");
        }

        return new LoyaltyChallengeProgress(challengeId, userId, utcNow);
    }

    /// <summary>Progress only ever moves forward (it is recomputed from a monotonically growing count — see ProcessChallengeProgressCommandHandler) and completion is recorded exactly once.</summary>
    public void UpdateProgress(int newValue, int targetValue, DateTime utcNow)
    {
        if (newValue < CurrentValue)
        {
            throw new ArgumentException("La progression ne peut pas régresser.");
        }

        CurrentValue = newValue;
        UpdatedAtUtc = utcNow;

        if (CompletedAtUtc is null && CurrentValue >= targetValue)
        {
            CompletedAtUtc = utcNow;
            RaiseDomainEvent(new ChallengeCompleted(Id, ChallengeId, UserId, utcNow));
        }
    }

    public void MarkRewarded(DateTime utcNow)
    {
        if (RewardedAtUtc is not null)
        {
            return;
        }

        RewardedAtUtc = utcNow;
    }
}

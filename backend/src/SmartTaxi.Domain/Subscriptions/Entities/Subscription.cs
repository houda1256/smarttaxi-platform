using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Subscriptions.Enums;
using SmartTaxi.Domain.Subscriptions.Events;

namespace SmartTaxi.Domain.Subscriptions.Entities;

/// <summary>
/// "Subscriber X subscribed to Plan Y for a period." TargetRole is snapshotted
/// from the Plan at creation time (never re-joined) so the DB can enforce "no
/// two concurrent Pending/Active subscriptions for the same role" with a
/// partial unique index on (SubscriberId, TargetRole) — the same
/// snapshot-at-issuance convention Invoice uses for its TaxRule fields.
/// Status transitions (activate/renew/suspend/cancel/expire) are atomic
/// repository-level guards, never domain mutation — same convention as
/// Payment; only creation is a genuine domain method. Rows are never
/// deleted, only transitioned to Cancelled/Expired.
/// </summary>
public sealed class Subscription : AggregateRoot
{
    public Guid SubscriberId { get; private set; }
    public Guid PlanId { get; private set; }
    public UserRole TargetRole { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public DateTime? TrialEndsAt { get; private set; }
    public bool AutoRenew { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Subscription()
    {
    }

    private Subscription(
        Guid subscriberId, Guid planId, UserRole targetRole, DateTime startDate, DateTime endDate, bool autoRenew,
        DateTime? trialEndsAt, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        SubscriberId = subscriberId;
        PlanId = planId;
        TargetRole = targetRole;
        Status = SubscriptionStatus.Pending;
        StartDate = startDate;
        EndDate = endDate;
        AutoRenew = autoRenew;
        TrialEndsAt = trialEndsAt;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;

        RaiseDomainEvent(new SubscriptionCreated(Id, subscriberId, planId, utcNow));
    }

    public static Subscription CreatePending(
        Guid subscriberId, Guid planId, UserRole targetRole, DateTime startDate, DateTime endDate, bool autoRenew,
        DateTime? trialEndsAt, DateTime utcNow)
    {
        if (endDate <= startDate)
        {
            throw new ArgumentException("La date de fin doit être postérieure à la date de début.");
        }

        return new Subscription(subscriberId, planId, targetRole, startDate, endDate, autoRenew, trialEndsAt, utcNow);
    }

    /// <summary>The read-side source of truth for entitlement checks — never trusts a stale "Active" Status past EndDate.</summary>
    public bool IsEffectivelyActive(DateTime utcNow) => Status == SubscriptionStatus.Active && EndDate > utcNow;
}

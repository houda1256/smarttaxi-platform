using SmartTaxi.Domain.Advertising.Enums;
using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Advertising.Entities;

/// <summary>Immutable, append-only audit row — one per lifecycle transition. No update/delete path exists anywhere, by design (same "history row, own table, never updated" convention as PaymentTransactionHistory/NotificationDeliveryAttempt).</summary>
public sealed class AdCampaignReviewHistoryEntry : Entity
{
    public Guid CampaignId { get; private set; }
    public Guid? ReviewerUserId { get; private set; }
    public AdCampaignReviewAction Action { get; private set; }
    public AdCampaignStatus PreviousStatus { get; private set; }
    public AdCampaignStatus NewStatus { get; private set; }
    public string? Reason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private AdCampaignReviewHistoryEntry()
    {
    }

    private AdCampaignReviewHistoryEntry(
        Guid campaignId, Guid? reviewerUserId, AdCampaignReviewAction action, AdCampaignStatus previousStatus,
        AdCampaignStatus newStatus, string? reason, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        CampaignId = campaignId;
        ReviewerUserId = reviewerUserId;
        Action = action;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Reason = reason;
        CreatedAtUtc = utcNow;
    }

    public static AdCampaignReviewHistoryEntry Record(
        Guid campaignId, Guid? reviewerUserId, AdCampaignReviewAction action, AdCampaignStatus previousStatus,
        AdCampaignStatus newStatus, string? reason, DateTime utcNow) =>
        new(campaignId, reviewerUserId, action, previousStatus, newStatus, reason?.Trim(), utcNow);
}

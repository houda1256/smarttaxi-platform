namespace SmartTaxi.Domain.Advertising.Enums;

/// <summary>The action recorded on one CampaignReviewHistory row — distinct from AdCampaignStatus (the resulting state), this is the verb that produced the transition.</summary>
public enum AdCampaignReviewAction
{
    Submitted,
    Approved,
    Rejected,
    ChangesRequested,
    ResubmittedAfterMaterialChange,
    Scheduled,
    Activated,
    Paused,
    Resumed,
    Suspended,
    Reactivated,
    Completed,
    Cancelled
}

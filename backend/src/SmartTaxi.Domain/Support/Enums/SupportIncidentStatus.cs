namespace SmartTaxi.Domain.Support.Enums;

/// <summary>
/// Exact statuses from the business specification — none invented. FalsePositive
/// is deliberately terminal with no reopen path (approved design): a genuinely
/// wrong report should be re-reported, not resurrected. Closed/Reopened admit
/// re-entry into Investigating, mirroring the ticket model's own philosophy.
/// </summary>
public enum SupportIncidentStatus
{
    Reported,
    Acknowledged,
    Investigating,
    Resolved,
    Closed,
    Reopened,
    FalsePositive
}

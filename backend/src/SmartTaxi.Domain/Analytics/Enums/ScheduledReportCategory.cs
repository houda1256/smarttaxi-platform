namespace SmartTaxi.Domain.Analytics.Enums;

/// <summary>
/// Deliberately narrower than the spec's full category list (Financial,
/// Operational, Security, Ride, Partner, Advertising, Support) — Security is
/// excluded here, not just unsupported at runtime, because no security-event
/// data exists anywhere yet (that is Module 13A's audit log). Excluding it
/// from the enum itself means an admin can never even configure a category
/// that would silently generate an empty/fake report.
/// </summary>
public enum ScheduledReportCategory
{
    Financial,
    Operational,
    Ride,
    Partner,
    Advertising,
    Support
}

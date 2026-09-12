using SmartTaxi.Domain.Analytics.Enums;
using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Analytics.Entities;

/// <summary>
/// No real scheduler exists anywhere in this codebase — NextRunAtUtc/
/// LastProcessedAtUtc/ProcessingClaimedAtUtc are advanced only by an explicit,
/// ops-triggered ProcessDueScheduledReportsCommand (mirrors
/// ProcessDueNotificationsCommand/ExpireStaleRoadsideRequestsCommand), never a
/// background job. All three of those fields — and IsActive/Category/
/// Frequency/RecipientUserId updates — are mutated exclusively via atomic
/// repository-level conditional guards (see IScheduledReportRepository), not
/// domain methods, same convention as every other status-bearing aggregate in
/// this codebase (SupportTicket, MaintenanceRequest, etc.) — the guard
/// condition has exactly one source of truth.
///
/// ProcessingClaimedAtUtc is the concurrency-safety anchor: a claim is won by
/// an atomic conditional update BEFORE report generation/delivery is
/// attempted (never after), so two concurrent processor invocations can never
/// both deliver the same due occurrence. A claim that is never finalized
/// (crash between claim and finalize) becomes reclaimable once
/// ProcessingClaimedAtUtc is older than the repository's documented stale-claim
/// threshold — see IScheduledReportRepository's own remarks for the full
/// at-least-once delivery semantics this implies.
/// </summary>
public sealed class ScheduledReportDefinition : AggregateRoot
{
    public ScheduledReportCategory Category { get; private set; }
    public ScheduledReportFrequency Frequency { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public bool IsActive { get; private set; }

    public DateTime NextRunAtUtc { get; private set; }
    public DateTime? LastProcessedAtUtc { get; private set; }

    /// <summary>Null when idle. Set to the claiming processor's utcNow the moment a due occurrence is claimed, cleared back to null on successful finalize.</summary>
    public DateTime? ProcessingClaimedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private ScheduledReportDefinition()
    {
    }

    private ScheduledReportDefinition(
        ScheduledReportCategory category, ScheduledReportFrequency frequency, Guid recipientUserId, DateTime firstRunAtUtc,
        DateTime utcNow)
        : base(Guid.NewGuid())
    {
        Category = category;
        Frequency = frequency;
        RecipientUserId = recipientUserId;
        IsActive = true;
        NextRunAtUtc = firstRunAtUtc;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public static ScheduledReportDefinition Create(
        ScheduledReportCategory category, ScheduledReportFrequency frequency, Guid recipientUserId, DateTime firstRunAtUtc,
        DateTime utcNow)
    {
        if (recipientUserId == Guid.Empty)
        {
            throw new ArgumentException("Le destinataire est requis.");
        }

        return new ScheduledReportDefinition(category, frequency, recipientUserId, firstRunAtUtc, utcNow);
    }

    /// <summary>
    /// Advances strictly from the occurrence's own due timestamp, never from
    /// "now" — so a sweep that runs late catches up one occurrence at a time
    /// instead of silently skipping ahead to the next future slot, and repeated
    /// processing never drifts from the nominal schedule.
    /// </summary>
    public static DateTime ComputeNextRun(ScheduledReportFrequency frequency, DateTime previousRunAtUtc) => frequency switch
    {
        ScheduledReportFrequency.Daily => previousRunAtUtc.AddDays(1),
        ScheduledReportFrequency.Weekly => previousRunAtUtc.AddDays(7),
        ScheduledReportFrequency.Monthly => previousRunAtUtc.AddMonths(1),
        ScheduledReportFrequency.Quarterly => previousRunAtUtc.AddMonths(3),
        _ => throw new ArgumentOutOfRangeException(nameof(frequency))
    };

    /// <summary>The [FromUtc, ToUtc) window a given occurrence's report should cover — the period immediately preceding its own due timestamp.</summary>
    public static (DateTime FromUtc, DateTime ToUtc) ComputeCoveredPeriod(ScheduledReportFrequency frequency, DateTime occurrenceUtc) =>
        frequency switch
        {
            ScheduledReportFrequency.Daily => (occurrenceUtc.AddDays(-1), occurrenceUtc),
            ScheduledReportFrequency.Weekly => (occurrenceUtc.AddDays(-7), occurrenceUtc),
            ScheduledReportFrequency.Monthly => (occurrenceUtc.AddMonths(-1), occurrenceUtc),
            ScheduledReportFrequency.Quarterly => (occurrenceUtc.AddMonths(-3), occurrenceUtc),
            _ => throw new ArgumentOutOfRangeException(nameof(frequency))
        };
}

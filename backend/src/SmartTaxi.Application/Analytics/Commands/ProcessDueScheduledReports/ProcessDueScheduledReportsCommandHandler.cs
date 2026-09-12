using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Analytics.Entities;
using SmartTaxi.Domain.Analytics.Enums;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Analytics.Commands.ProcessDueScheduledReports;

/// <summary>
/// Concurrency/crash semantics (documented, not assumed): the winner of
/// TryClaimAsync is determined BEFORE any report is generated or delivered —
/// two concurrent invocations of this command can never both deliver the same
/// due occurrence, because only one caller's atomic conditional UPDATE can
/// ever affect the row for a given (Id, NextRunAtUtc) pairing. This is an
/// at-least-once guarantee, not exactly-once: if this process crashes (or the
/// generation/dispatch step throws) after a successful DispatchAsync but
/// before TryFinalizeAsync commits, the claim is left in place and becomes
/// reclaimable once IScheduledReportRepository.StaleClaimThreshold has
/// elapsed — a subsequent sweep will then regenerate and redeliver that same
/// occurrence. This trade-off is deliberate: achieving true exactly-once would
/// require either a distributed transaction across the notification and
/// scheduling stores (explicitly out of scope — no distributed transactions
/// anywhere in this codebase) or a persisted per-delivery idempotency ledger,
/// which was not built because nothing in this pass requires stronger-than-
/// at-least-once delivery for an admin-facing report notification.
///
/// A severely overdue definition (missed many occurrences) catches up ONE
/// occurrence per invocation of this command, never all of them in a single
/// call — repeated ops-triggered sweeps are required to fully catch up,
/// deliberately bounding the cost of any single call.
/// </summary>
public sealed class ProcessDueScheduledReportsCommandHandler : ICommandHandler<ProcessDueScheduledReportsCommand, int>
{
    private const string TemplateKey = "analytics.scheduled-report.ready";

    private readonly IScheduledReportRepository _repository;
    private readonly IAdminDashboardReader _dashboardReader;
    private readonly IGrowthAnalyticsReader _growthReader;
    private readonly IRideAnalyticsReader _rideReader;
    private readonly IFleetAnalyticsReader _fleetReader;
    private readonly IAdvertisingAnalyticsReader _advertisingReader;
    private readonly ISupportAnalyticsReader _supportReader;
    private readonly IFinancialAnalyticsReader _financialReader;
    private readonly INotificationDispatcher _notificationDispatcher;

    public ProcessDueScheduledReportsCommandHandler(
        IScheduledReportRepository repository, IAdminDashboardReader dashboardReader, IGrowthAnalyticsReader growthReader,
        IRideAnalyticsReader rideReader, IFleetAnalyticsReader fleetReader, IAdvertisingAnalyticsReader advertisingReader,
        ISupportAnalyticsReader supportReader, IFinancialAnalyticsReader financialReader, INotificationDispatcher notificationDispatcher)
    {
        _repository = repository;
        _dashboardReader = dashboardReader;
        _growthReader = growthReader;
        _rideReader = rideReader;
        _fleetReader = fleetReader;
        _advertisingReader = advertisingReader;
        _supportReader = supportReader;
        _financialReader = financialReader;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<int> Handle(ProcessDueScheduledReportsCommand command, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var dueIds = await _repository.GetDueIdsAsync(utcNow, cancellationToken);
        var processedCount = 0;

        foreach (var id in dueIds)
        {
            var claimedAtUtc = DateTime.UtcNow;
            var claimed = await _repository.TryClaimAsync(id, claimedAtUtc, cancellationToken);

            if (!claimed)
            {
                continue;
            }

            var definition = await _repository.GetByIdAsync(id, cancellationToken);

            if (definition is null)
            {
                continue;
            }

            try
            {
                var occurrenceUtc = definition.NextRunAtUtc;
                var (fromUtc, toUtc) = ScheduledReportDefinition.ComputeCoveredPeriod(definition.Frequency, occurrenceUtc);

                await GenerateAndDeliverAsync(definition, fromUtc, toUtc, cancellationToken);

                var nextRunAtUtc = ScheduledReportDefinition.ComputeNextRun(definition.Frequency, occurrenceUtc);
                var finalizedAtUtc = DateTime.UtcNow;
                var finalized = await _repository.TryFinalizeAsync(id, claimedAtUtc, nextRunAtUtc, finalizedAtUtc, cancellationToken);

                if (finalized)
                {
                    processedCount++;
                }
            }
            catch
            {
                // Deliberately do not finalize: the claim is left in place and
                // becomes reclaimable after StaleClaimThreshold, so a later
                // sweep retries this occurrence instead of silently losing it.
            }
        }

        return processedCount;
    }

    private async Task GenerateAndDeliverAsync(
        ScheduledReportDefinition definition, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        switch (definition.Category)
        {
            case ScheduledReportCategory.Financial:
                await _financialReader.GetSummaryAsync(fromUtc, toUtc, cancellationToken);
                break;
            case ScheduledReportCategory.Operational:
                await _dashboardReader.GetSnapshotAsync(cancellationToken);
                await _growthReader.GetGrowthAsync(fromUtc, toUtc, cancellationToken);
                break;
            case ScheduledReportCategory.Ride:
                await _rideReader.GetSummaryAsync(fromUtc, toUtc, cancellationToken);
                break;
            case ScheduledReportCategory.Partner:
                await _fleetReader.GetSummaryAsync(fromUtc, toUtc, cancellationToken);
                break;
            case ScheduledReportCategory.Advertising:
                await _advertisingReader.GetSummaryAsync(fromUtc, toUtc, cancellationToken);
                break;
            case ScheduledReportCategory.Support:
                await _supportReader.GetSummaryAsync(fromUtc, toUtc, cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(definition), definition.Category, "Catégorie de rapport planifié inconnue.");
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                definition.RecipientUserId, NotificationCategory.Analytics, TemplateKey,
                new Dictionary<string, string>
                {
                    ["Category"] = definition.Category.ToString(),
                    ["FromUtc"] = fromUtc.ToString("O"),
                    ["ToUtc"] = toUtc.ToString("O")
                },
                IsMandatory: false, SourceType: "ScheduledReportDefinition", SourceId: Guid.NewGuid()),
            cancellationToken);
    }
}

using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Analytics.Enums;

namespace SmartTaxi.Application.Analytics.Commands.ExportAnalyticsReport;

/// <summary>
/// Generates the same category-scoped summary ProcessDueScheduledReportsCommand
/// would generate, then hands it to IAnalyticsReportExporter. Financial exports
/// remain exclusively Payments' own (/api/admin/finance/reports/export) — this
/// handler covers only the 5 non-financial categories plus a thin Financial
/// pass-through that reuses IFinancialAnalyticsReader, never a second
/// financial computation.
/// </summary>
public sealed class ExportAnalyticsReportCommandHandler : ICommandHandler<ExportAnalyticsReportCommand, Result<string>>
{
    private const string InvalidRangeError = "La période demandée est invalide (FromUtc doit précéder ToUtc, sur au plus 366 jours).";
    private const string ConfidentialityNotice = "Confidentiel — usage interne SmartTaxi uniquement.";

    private readonly IAdminDashboardReader _dashboardReader;
    private readonly IGrowthAnalyticsReader _growthReader;
    private readonly IRideAnalyticsReader _rideReader;
    private readonly IFleetAnalyticsReader _fleetReader;
    private readonly IAdvertisingAnalyticsReader _advertisingReader;
    private readonly ISupportAnalyticsReader _supportReader;
    private readonly IFinancialAnalyticsReader _financialReader;
    private readonly IAnalyticsReportExporter _exporter;

    public ExportAnalyticsReportCommandHandler(
        IAdminDashboardReader dashboardReader, IGrowthAnalyticsReader growthReader, IRideAnalyticsReader rideReader,
        IFleetAnalyticsReader fleetReader, IAdvertisingAnalyticsReader advertisingReader, ISupportAnalyticsReader supportReader,
        IFinancialAnalyticsReader financialReader, IAnalyticsReportExporter exporter)
    {
        _dashboardReader = dashboardReader;
        _growthReader = growthReader;
        _rideReader = rideReader;
        _fleetReader = fleetReader;
        _advertisingReader = advertisingReader;
        _supportReader = supportReader;
        _financialReader = financialReader;
        _exporter = exporter;
    }

    public async Task<Result<string>> Handle(ExportAnalyticsReportCommand command, CancellationToken cancellationToken)
    {
        if (!AnalyticsDateRange.IsValid(command.FromUtc, command.ToUtc))
        {
            return Result<string>.Failure(InvalidRangeError, ErrorType.Validation);
        }

        switch (command.Category)
        {
            case ScheduledReportCategory.Financial:
                await _financialReader.GetSummaryAsync(command.FromUtc, command.ToUtc, cancellationToken);
                break;
            case ScheduledReportCategory.Operational:
                await _dashboardReader.GetSnapshotAsync(cancellationToken);
                await _growthReader.GetGrowthAsync(command.FromUtc, command.ToUtc, cancellationToken);
                break;
            case ScheduledReportCategory.Ride:
                await _rideReader.GetSummaryAsync(command.FromUtc, command.ToUtc, cancellationToken);
                break;
            case ScheduledReportCategory.Partner:
                await _fleetReader.GetSummaryAsync(command.FromUtc, command.ToUtc, cancellationToken);
                break;
            case ScheduledReportCategory.Advertising:
                await _advertisingReader.GetSummaryAsync(command.FromUtc, command.ToUtc, cancellationToken);
                break;
            case ScheduledReportCategory.Support:
                await _supportReader.GetSummaryAsync(command.FromUtc, command.ToUtc, cancellationToken);
                break;
        }

        var utcNow = DateTime.UtcNow;
        var storageKey = await _exporter.ExportAsync(
            new AnalyticsReportExportRequest(
                command.Category, command.FromUtc, command.ToUtc, command.RequestedByUserId, utcNow, RecordCount: 1,
                ConfidentialityNotice),
            command.Format, cancellationToken);

        return Result<string>.Success(storageKey);
    }
}

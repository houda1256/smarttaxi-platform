using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Domain.Analytics.Entities;
using SmartTaxi.Domain.Analytics.Enums;

namespace SmartTaxi.API.Contracts.Analytics;

public sealed record CreateScheduledReportRequest(
    ScheduledReportCategory Category, ScheduledReportFrequency Frequency, Guid RecipientUserId, DateTime FirstRunAtUtc);

public sealed record UpdateScheduledReportRequest(ScheduledReportCategory Category, ScheduledReportFrequency Frequency, Guid RecipientUserId);

public sealed record ScheduledReportDefinitionResponse(
    Guid Id, string Category, string Frequency, Guid RecipientUserId, bool IsActive, DateTime NextRunAtUtc,
    DateTime? LastProcessedAtUtc, DateTime CreatedAtUtc, DateTime UpdatedAtUtc)
{
    public static ScheduledReportDefinitionResponse FromEntity(ScheduledReportDefinition definition) => new(
        definition.Id, definition.Category.ToString(), definition.Frequency.ToString(), definition.RecipientUserId,
        definition.IsActive, definition.NextRunAtUtc, definition.LastProcessedAtUtc, definition.CreatedAtUtc, definition.UpdatedAtUtc);
}

public sealed record ExportAnalyticsReportRequest(ScheduledReportCategory Category, DateTime FromUtc, DateTime ToUtc, AnalyticsExportFormat Format);

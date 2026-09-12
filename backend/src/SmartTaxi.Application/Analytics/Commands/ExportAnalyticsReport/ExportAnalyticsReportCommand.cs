using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Analytics.Enums;

namespace SmartTaxi.Application.Analytics.Commands.ExportAnalyticsReport;

public sealed record ExportAnalyticsReportCommand(
    ScheduledReportCategory Category, DateTime FromUtc, DateTime ToUtc, Guid RequestedByUserId, AnalyticsExportFormat Format)
    : ICommand<Result<string>>;

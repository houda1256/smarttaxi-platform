using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Analytics.Commands.ProcessDueScheduledReports;

/// <summary>Explicit, ops-triggered sweep — no BackgroundService/Hangfire/Quartz/cron involved. Returns the number of occurrences actually claimed and delivered in this call.</summary>
public sealed record ProcessDueScheduledReportsCommand : ICommand<int>;

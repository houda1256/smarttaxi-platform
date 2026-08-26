using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Notifications.Commands.ProcessDueNotifications;

/// <summary>Explicit, idempotent processing entry point a future scheduler (or an ops endpoint) calls — no Hangfire/Quartz/background worker exists in this codebase.</summary>
public sealed record ProcessDueNotificationsCommand : ICommand<Result<int>>;

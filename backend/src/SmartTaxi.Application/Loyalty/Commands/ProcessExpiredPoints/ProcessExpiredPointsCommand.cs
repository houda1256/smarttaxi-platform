using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Loyalty.Commands.ProcessExpiredPoints;

/// <summary>Explicit, idempotent processing entry point a future scheduler (or an ops endpoint) calls — no external scheduler is introduced, mirroring Notifications' own ProcessDueNotificationsCommand.</summary>
public sealed record ProcessExpiredPointsCommand : ICommand<Result<int>>;

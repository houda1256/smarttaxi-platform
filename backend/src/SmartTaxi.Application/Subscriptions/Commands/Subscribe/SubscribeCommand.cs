using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Subscriptions.Commands.Subscribe;

/// <summary>CallerRoles are the identity roles actually held by the requesting user (from the JWT role claims), used to verify eligibility for the plan's TargetRole.</summary>
public sealed record SubscribeCommand(
    Guid SubscriberId, Guid PlanId, bool AutoRenew, IReadOnlyCollection<UserRole> CallerRoles) : ICommand<Result<Guid>>;

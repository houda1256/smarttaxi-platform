using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Application.Loyalty.Commands.CreateReward;

public sealed record CreateRewardCommand(
    string Code, string Name, string Description, int CostInRewardPoints, LoyaltyRewardType RewardType,
    IReadOnlyList<UserRole> TargetRoles, DateTime? AvailableFrom, DateTime? AvailableTo, int? UsageLimit) : ICommand<Result<Guid>>;

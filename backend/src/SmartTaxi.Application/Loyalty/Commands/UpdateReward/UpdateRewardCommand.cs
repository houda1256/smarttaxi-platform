using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Loyalty.Commands.UpdateReward;

public sealed record UpdateRewardCommand(
    Guid RewardId, string Name, string Description, int CostInRewardPoints, DateTime? AvailableFrom, DateTime? AvailableTo, int? UsageLimit)
    : ICommand<Result>;

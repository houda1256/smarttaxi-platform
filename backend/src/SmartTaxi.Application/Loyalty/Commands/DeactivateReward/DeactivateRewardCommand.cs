using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Loyalty.Commands.DeactivateReward;

public sealed record DeactivateRewardCommand(Guid RewardId) : ICommand<Result>;

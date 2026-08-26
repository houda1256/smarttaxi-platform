using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Loyalty.Commands.ActivateReward;

public sealed record ActivateRewardCommand(Guid RewardId) : ICommand<Result>;

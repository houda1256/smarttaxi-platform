using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Loyalty.Commands.DeactivateChallenge;

public sealed record DeactivateChallengeCommand(Guid ChallengeId) : ICommand<Result>;

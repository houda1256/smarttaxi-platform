using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Loyalty.Commands.ActivateChallenge;

public sealed record ActivateChallengeCommand(Guid ChallengeId) : ICommand<Result>;

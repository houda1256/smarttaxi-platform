using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Commands.TwoFactorChallenge;

public sealed record TwoFactorChallengeCommand(string ChallengeToken, string Code) : ICommand<Result<TwoFactorChallengeResult>>;

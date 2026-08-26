using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Application.Loyalty.Commands.CreateChallenge;

public sealed record CreateChallengeCommand(
    string Code, string Name, LoyaltyChallengeCriteriaType CriteriaType, int TargetValue, int RewardPoints, UserRole? EligibleRole,
    DateTime? ValidFrom, DateTime? ValidTo) : ICommand<Result<Guid>>;

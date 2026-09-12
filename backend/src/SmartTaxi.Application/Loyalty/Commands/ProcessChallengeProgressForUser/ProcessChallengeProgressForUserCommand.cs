using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Loyalty.Commands.ProcessChallengeProgressForUser;

/// <summary>Recomputes progress for every active challenge eligible for this user's role and grants the reward exactly once per challenge — called after a payment award and after a referral reward.</summary>
public sealed record ProcessChallengeProgressForUserCommand(Guid UserId, UserRole Role) : ICommand<Result<int>>;

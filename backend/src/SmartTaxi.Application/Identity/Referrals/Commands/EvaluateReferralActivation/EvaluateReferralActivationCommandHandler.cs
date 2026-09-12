using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Identity.Referrals.Abstractions;
using SmartTaxi.Domain.Identity.Referrals.Enums;

namespace SmartTaxi.Application.Identity.Referrals.Commands.EvaluateReferralActivation;

/// <summary>
/// Checks the configurable activation policy against the referee's current
/// state and, if satisfied, atomically flips the referral to reward-eligible.
/// Returns false (not a failure) whenever nothing changed — not-yet-satisfied
/// is a normal, expected outcome, not an error.
/// </summary>
public sealed class EvaluateReferralActivationCommandHandler
    : ICommandHandler<EvaluateReferralActivationCommand, Result<bool>>
{
    private const string NotFoundError = "Parrainage introuvable.";

    private readonly IReferralRepository _repository;
    private readonly IUserRepository _userRepository;
    private readonly IReferralActivationPolicy _policy;

    public EvaluateReferralActivationCommandHandler(
        IReferralRepository repository, IUserRepository userRepository, IReferralActivationPolicy policy)
    {
        _repository = repository;
        _userRepository = userRepository;
        _policy = policy;
    }

    public async Task<Result<bool>> Handle(EvaluateReferralActivationCommand command, CancellationToken cancellationToken)
    {
        var referral = await _repository.GetByIdAsync(command.ReferralId, cancellationToken);

        if (referral is null)
        {
            return Result<bool>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (referral.Status != ReferralStatus.PendingActivation)
        {
            return Result<bool>.Success(false);
        }

        var referee = await _userRepository.GetByIdAsync(referral.RefereeUserId, cancellationToken);

        if (referee is null)
        {
            return Result<bool>.Success(false);
        }

        var utcNow = DateTime.UtcNow;
        var conditionsSatisfied =
            (!_policy.RequireEmailVerified || referee.EmailVerifiedAt is not null)
            && (!_policy.RequirePhoneVerified || referee.PhoneVerifiedAt is not null)
            && (utcNow - referral.CreatedAt).TotalDays >= _policy.MinAccountAgeDays;

        if (!conditionsSatisfied)
        {
            return Result<bool>.Success(false);
        }

        var activated = await _repository.TryActivateAsync(referral.Id, utcNow, cancellationToken);
        return Result<bool>.Success(activated);
    }
}

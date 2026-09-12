using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Identity.Referrals.Abstractions;
using SmartTaxi.Domain.Identity.Referrals.Entities;

namespace SmartTaxi.Application.Identity.Referrals.Commands.RegisterReferral;

public sealed class RegisterReferralCommandHandler : ICommandHandler<RegisterReferralCommand, Result>
{
    private const string InvalidCodeError = "Code de parrainage invalide.";
    private const string SelfReferralError = "Un utilisateur ne peut pas se parrainer lui-même.";
    private const string AlreadyHasSponsorError = "Cet utilisateur a déjà un parrain.";

    private readonly IUserRepository _userRepository;
    private readonly IReferralRepository _referralRepository;

    public RegisterReferralCommandHandler(IUserRepository userRepository, IReferralRepository referralRepository)
    {
        _userRepository = userRepository;
        _referralRepository = referralRepository;
    }

    public async Task<Result> Handle(RegisterReferralCommand command, CancellationToken cancellationToken)
    {
        var referrer = await _userRepository.GetByReferralCodeAsync(command.ReferralCode, cancellationToken);

        if (referrer is null)
        {
            return Result.Failure(InvalidCodeError, ErrorType.Validation);
        }

        // Redundant with the caller's own identity, but kept as defense in
        // depth against any future reuse of this command from a different flow.
        if (referrer.Id == command.RefereeUserId)
        {
            return Result.Failure(SelfReferralError, ErrorType.Validation);
        }

        var existingForReferee = await _referralRepository.GetByRefereeUserIdAsync(command.RefereeUserId, cancellationToken);

        if (existingForReferee is not null)
        {
            return Result.Failure(AlreadyHasSponsorError, ErrorType.Conflict);
        }

        var referral = new Referral(referrer.Id, command.RefereeUserId, command.ReferralCode, DateTime.UtcNow);
        var added = await _referralRepository.AddAsync(referral, cancellationToken);

        if (!added)
        {
            // Lost a race against another concurrent registration for the same referee.
            return Result.Failure(AlreadyHasSponsorError, ErrorType.Conflict);
        }

        return Result.Success();
    }
}

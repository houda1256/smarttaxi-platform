using System.Security.Cryptography;
using System.Text;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Application.Identity.Commands.ConfirmPhoneVerification;

public sealed class ConfirmPhoneVerificationCommandHandler : ICommandHandler<ConfirmPhoneVerificationCommand, Result>
{
    private const string InvalidOtpError = "Le code est invalide ou a expiré.";

    private readonly IUserRepository _userRepository;
    private readonly IPhoneVerificationOtpRepository _otpRepository;
    private readonly IRefreshTokenHasher _hasher;
    private readonly IPhoneVerificationPolicy _policy;

    public ConfirmPhoneVerificationCommandHandler(
        IUserRepository userRepository,
        IPhoneVerificationOtpRepository otpRepository,
        IRefreshTokenHasher hasher,
        IPhoneVerificationPolicy policy)
    {
        _userRepository = userRepository;
        _otpRepository = otpRepository;
        _hasher = hasher;
        _policy = policy;
    }

    public async Task<Result> Handle(ConfirmPhoneVerificationCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result.Failure(InvalidOtpError, ErrorType.Validation);
        }

        var otp = await _otpRepository.GetLatestForUserAsync(user.Id, cancellationToken);
        var utcNow = DateTime.UtcNow;

        if (otp is null || !otp.IsValid(utcNow))
        {
            return Result.Failure(InvalidOtpError, ErrorType.Validation);
        }

        var submittedHash = _hasher.Hash(command.Otp.Trim());

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(submittedHash), Encoding.UTF8.GetBytes(otp.OtpHash)))
        {
            await _otpRepository.RecordFailedAttemptAsync(
                otp.Id, _policy.MaxAttempts, utcNow, _policy.LockoutDuration, cancellationToken);
            return Result.Failure(InvalidOtpError, ErrorType.Validation);
        }

        var consumed = await _otpRepository.TryConsumeAsync(otp.Id, utcNow, cancellationToken);

        if (!consumed)
        {
            return Result.Failure(InvalidOtpError, ErrorType.Validation);
        }

        user.VerifyPhone(utcNow);
        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result.Success();
    }
}

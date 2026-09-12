using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Identity.Commands.RequestPhoneVerification;

public sealed class RequestPhoneVerificationCommandHandler : ICommandHandler<RequestPhoneVerificationCommand, Result>
{
    private const string ThrottledError = "Veuillez attendre avant de demander un nouveau code.";
    private const string UnauthorizedError = "Utilisateur invalide.";

    private readonly IUserRepository _userRepository;
    private readonly IPhoneVerificationOtpRepository _otpRepository;
    private readonly IOtpGenerator _otpGenerator;
    private readonly IRefreshTokenHasher _hasher;
    private readonly IPhoneVerificationPolicy _policy;
    private readonly ISmsSender _smsSender;

    public RequestPhoneVerificationCommandHandler(
        IUserRepository userRepository,
        IPhoneVerificationOtpRepository otpRepository,
        IOtpGenerator otpGenerator,
        IRefreshTokenHasher hasher,
        IPhoneVerificationPolicy policy,
        ISmsSender smsSender)
    {
        _userRepository = userRepository;
        _otpRepository = otpRepository;
        _otpGenerator = otpGenerator;
        _hasher = hasher;
        _policy = policy;
        _smsSender = smsSender;
    }

    public async Task<Result> Handle(RequestPhoneVerificationCommand command, CancellationToken cancellationToken)
    {
        if (!PhoneNumber.TryCreate(command.PhoneNumber, out var phoneNumber, out var formatError))
        {
            return Result.Failure(formatError, ErrorType.Validation);
        }

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result.Failure(UnauthorizedError, ErrorType.Unauthorized);
        }

        // This flow is authenticated (unlike email verification), so there is
        // no account-enumeration concern — an explicit "please wait" error is
        // better UX than silent throttling here.
        var utcNow = DateTime.UtcNow;
        var lastIssuedAt = await _otpRepository.GetLastIssuedAtAsync(user.Id, cancellationToken);

        if (lastIssuedAt is not null && utcNow - lastIssuedAt.Value < _policy.ResendInterval)
        {
            return Result.Failure(ThrottledError, ErrorType.Validation);
        }

        user.SetPhoneNumber(phoneNumber.Value);
        await _userRepository.UpdateAsync(user, cancellationToken);

        await _otpRepository.InvalidateActiveForUserAsync(user.Id, utcNow, cancellationToken);

        var rawOtp = _otpGenerator.Generate(_policy.OtpDigits);
        var otpHash = _hasher.Hash(rawOtp);
        var otp = new PhoneVerificationOtp(user.Id, otpHash, utcNow.Add(_policy.OtpLifetime), utcNow);
        await _otpRepository.AddAsync(otp, cancellationToken);

        await _smsSender.SendAsync(phoneNumber.Value, $"Votre code de vérification SmartTaxi : {rawOtp}", cancellationToken);

        return Result.Success();
    }
}

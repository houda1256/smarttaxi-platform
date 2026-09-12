using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Application.Identity.Commands.EnrollTwoFactor;

public sealed class EnrollTwoFactorCommandHandler : ICommandHandler<EnrollTwoFactorCommand, Result<EnrollTwoFactorResult>>
{
    private const string UnauthorizedError = "Utilisateur invalide.";

    private readonly IUserRepository _userRepository;
    private readonly ITotpService _totpService;
    private readonly ITwoFactorSecretProtector _protector;
    private readonly ITwoFactorPolicy _policy;

    public EnrollTwoFactorCommandHandler(
        IUserRepository userRepository,
        ITotpService totpService,
        ITwoFactorSecretProtector protector,
        ITwoFactorPolicy policy)
    {
        _userRepository = userRepository;
        _totpService = totpService;
        _protector = protector;
        _policy = policy;
    }

    public async Task<Result<EnrollTwoFactorResult>> Handle(EnrollTwoFactorCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result<EnrollTwoFactorResult>.Failure(UnauthorizedError, ErrorType.Unauthorized);
        }

        var rawSecret = _totpService.GenerateSecret();
        var encryptedSecret = _protector.Protect(rawSecret);

        // Deliberately does not touch TwoFactorEnabled/ActiveSecret — an
        // already-active 2FA setup keeps working with its existing secret
        // until this new enrollment is itself confirmed.
        user.BeginTwoFactorEnrollment(encryptedSecret, DateTime.UtcNow);
        await _userRepository.UpdateAsync(user, cancellationToken);

        var authenticatorUri = _totpService.BuildAuthenticatorUri(rawSecret, user.Email.Value, _policy.Issuer);

        return Result<EnrollTwoFactorResult>.Success(new EnrollTwoFactorResult(rawSecret, authenticatorUri));
    }
}

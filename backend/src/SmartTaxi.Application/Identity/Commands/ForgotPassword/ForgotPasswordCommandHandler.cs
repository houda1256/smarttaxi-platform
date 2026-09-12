using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Identity.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IRefreshTokenGenerator _tokenGenerator;
    private readonly IRefreshTokenHasher _tokenHasher;
    private readonly IPasswordResetPolicy _policy;
    private readonly IEmailSender _emailSender;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository tokenRepository,
        IRefreshTokenGenerator tokenGenerator,
        IRefreshTokenHasher tokenHasher,
        IPasswordResetPolicy policy,
        IEmailSender emailSender)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _tokenGenerator = tokenGenerator;
        _tokenHasher = tokenHasher;
        _policy = policy;
        _emailSender = emailSender;
    }

    public async Task<Result> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        // Same generic outcome regardless of what happens below — never reveal
        // whether this email belongs to an account.
        if (Email.TryCreate(command.Email, out var email, out _))
        {
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
            var utcNow = DateTime.UtcNow;

            if (user is not null && user.IsActive)
            {
                await _tokenRepository.InvalidateActiveForUserAsync(user.Id, utcNow, cancellationToken);

                var rawToken = _tokenGenerator.Generate();
                var tokenHash = _tokenHasher.Hash(rawToken);
                var token = new PasswordResetToken(user.Id, tokenHash, utcNow.Add(_policy.TokenLifetime), utcNow);
                await _tokenRepository.AddAsync(token, cancellationToken);

                await _emailSender.SendAsync(
                    user.Email.Value,
                    "Réinitialisation de votre mot de passe",
                    $"Utilisez ce code pour réinitialiser votre mot de passe : {rawToken}",
                    cancellationToken);
            }
        }

        return Result.Success();
    }
}

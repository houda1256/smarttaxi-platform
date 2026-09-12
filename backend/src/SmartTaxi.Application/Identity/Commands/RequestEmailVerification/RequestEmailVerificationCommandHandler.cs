using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Identity.Commands.RequestEmailVerification;

public sealed class RequestEmailVerificationCommandHandler : ICommandHandler<RequestEmailVerificationCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly IRefreshTokenGenerator _tokenGenerator;
    private readonly IRefreshTokenHasher _tokenHasher;
    private readonly IEmailVerificationPolicy _policy;
    private readonly IEmailSender _emailSender;

    public RequestEmailVerificationCommandHandler(
        IUserRepository userRepository,
        IEmailVerificationTokenRepository tokenRepository,
        IRefreshTokenGenerator tokenGenerator,
        IRefreshTokenHasher tokenHasher,
        IEmailVerificationPolicy policy,
        IEmailSender emailSender)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _tokenGenerator = tokenGenerator;
        _tokenHasher = tokenHasher;
        _policy = policy;
        _emailSender = emailSender;
    }

    public async Task<Result> Handle(RequestEmailVerificationCommand command, CancellationToken cancellationToken)
    {
        // Same generic outcome regardless of what happens below — never reveal
        // whether this email belongs to an account, is already verified, or
        // was rate-limited.
        if (Email.TryCreate(command.Email, out var email, out _))
        {
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
            var utcNow = DateTime.UtcNow;

            if (user is not null && user.IsActive && user.EmailVerifiedAt is null)
            {
                var lastIssuedAt = await _tokenRepository.GetLastIssuedAtAsync(user.Id, cancellationToken);

                if (lastIssuedAt is null || utcNow - lastIssuedAt.Value >= _policy.ResendInterval)
                {
                    await _tokenRepository.InvalidateActiveForUserAsync(user.Id, utcNow, cancellationToken);

                    var rawToken = _tokenGenerator.Generate();
                    var tokenHash = _tokenHasher.Hash(rawToken);
                    var token = new EmailVerificationToken(user.Id, tokenHash, utcNow.Add(_policy.TokenLifetime), utcNow);
                    await _tokenRepository.AddAsync(token, cancellationToken);

                    await _emailSender.SendAsync(
                        user.Email.Value,
                        "Vérifiez votre adresse email",
                        $"Utilisez ce code pour vérifier votre email : {rawToken}",
                        cancellationToken);
                }
            }
        }

        return Result.Success();
    }
}

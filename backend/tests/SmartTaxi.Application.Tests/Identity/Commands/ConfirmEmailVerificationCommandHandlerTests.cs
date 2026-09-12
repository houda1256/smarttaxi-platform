using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Commands.ConfirmEmailVerification;
using SmartTaxi.Application.Identity.Commands.RegisterUser;
using SmartTaxi.Application.Identity.Commands.RequestEmailVerification;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.Identity.Commands;

public class ConfirmEmailVerificationCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeEmailVerificationTokenRepository _tokenRepository = new();
    private readonly FakeRefreshTokenGenerator _tokenGenerator = new();
    private readonly FakeRefreshTokenHasher _tokenHasher = new();
    private readonly FakeEmailSender _emailSender = new();
    private readonly RequestEmailVerificationCommandHandler _requestHandler;
    private readonly ConfirmEmailVerificationCommandHandler _confirmHandler;

    public ConfirmEmailVerificationCommandHandlerTests()
    {
        _requestHandler = new RequestEmailVerificationCommandHandler(
            _userRepository, _tokenRepository, _tokenGenerator, _tokenHasher, new FakeEmailVerificationPolicy(), _emailSender);
        _confirmHandler = new ConfirmEmailVerificationCommandHandler(_tokenRepository, _userRepository, _tokenHasher);
    }

    private async Task<string> RegisterAndRequestVerificationAsync(string email = "user@example.com")
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher, new FakeNotificationDispatcher());
        await registerHandler.Handle(new RegisterUserCommand(email, "correct-password"), CancellationToken.None);
        await _requestHandler.Handle(new RequestEmailVerificationCommand(email), CancellationToken.None);
        return _tokenGenerator.LastGenerated!;
    }

    [Fact]
    public async Task Handle_WithValidToken_VerifiesEmailAndSucceedsOnce()
    {
        var rawToken = await RegisterAndRequestVerificationAsync();

        var result = await _confirmHandler.Handle(new ConfirmEmailVerificationCommand(rawToken), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var user = await _userRepository.GetByEmailAsync(Email.Create("user@example.com"), CancellationToken.None);
        Assert.NotNull(user!.EmailVerifiedAt);
    }

    [Fact]
    public async Task Handle_ReusingAnAlreadyConsumedToken_IsRejected()
    {
        var rawToken = await RegisterAndRequestVerificationAsync();
        await _confirmHandler.Handle(new ConfirmEmailVerificationCommand(rawToken), CancellationToken.None);

        var secondAttempt = await _confirmHandler.Handle(new ConfirmEmailVerificationCommand(rawToken), CancellationToken.None);

        Assert.False(secondAttempt.IsSuccess);
        Assert.Equal(ErrorType.Validation, secondAttempt.ErrorType);
    }

    [Fact]
    public async Task Handle_WithExpiredToken_IsRejected()
    {
        var rawToken = await RegisterAndRequestVerificationAsync();
        var tokenHash = _tokenHasher.Hash(rawToken);
        var token = await _tokenRepository.GetByTokenHashAsync(tokenHash, CancellationToken.None);
        // Simulate expiry via the same repository-level invalidation path used
        // elsewhere (no public entity mutator exists) — expiring "as of now"
        // means it will show as expired for any check made afterwards.
        await _tokenRepository.InvalidateActiveForUserAsync(token!.UserId, DateTime.UtcNow, CancellationToken.None);

        var result = await _confirmHandler.Handle(new ConfirmEmailVerificationCommand(rawToken), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithUnknownToken_IsRejected()
    {
        var result = await _confirmHandler.Handle(new ConfirmEmailVerificationCommand("never-issued"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Handle_ForDeactivatedUser_IsRejected()
    {
        var rawToken = await RegisterAndRequestVerificationAsync();
        var user = await _userRepository.GetByEmailAsync(Email.Create("user@example.com"), CancellationToken.None);
        user!.Deactivate();
        await _userRepository.UpdateAsync(user, CancellationToken.None);

        var result = await _confirmHandler.Handle(new ConfirmEmailVerificationCommand(rawToken), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(user.EmailVerifiedAt);
    }
}

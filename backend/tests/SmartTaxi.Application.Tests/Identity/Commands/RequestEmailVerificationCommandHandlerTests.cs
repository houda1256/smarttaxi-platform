using SmartTaxi.Application.Identity.Commands.RegisterUser;
using SmartTaxi.Application.Identity.Commands.RequestEmailVerification;
using SmartTaxi.Application.Tests.TestDoubles;

namespace SmartTaxi.Application.Tests.Identity.Commands;

public class RequestEmailVerificationCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeEmailVerificationTokenRepository _tokenRepository = new();
    private readonly FakeRefreshTokenGenerator _tokenGenerator = new();
    private readonly FakeRefreshTokenHasher _tokenHasher = new();
    private readonly FakeEmailSender _emailSender = new();
    private readonly FakeEmailVerificationPolicy _policy = new();
    private readonly RequestEmailVerificationCommandHandler _handler;

    public RequestEmailVerificationCommandHandlerTests()
    {
        _handler = new RequestEmailVerificationCommandHandler(
            _userRepository, _tokenRepository, _tokenGenerator, _tokenHasher, _policy, _emailSender);
    }

    private async Task RegisterUserAsync(string email = "user@example.com")
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher);
        await registerHandler.Handle(new RegisterUserCommand(email, "correct-password"), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ForRegisteredUnverifiedUser_IssuesTokenAndSendsEmail()
    {
        await RegisterUserAsync();

        var result = await _handler.Handle(new RequestEmailVerificationCommand("user@example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _tokenRepository.Count);
        Assert.Single(_emailSender.SentMessages);
    }

    [Fact]
    public async Task Handle_ForUnknownEmail_ReturnsSameGenericSuccessAndSendsNothing()
    {
        var result = await _handler.Handle(new RequestEmailVerificationCommand("unknown@example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_emailSender.SentMessages);
    }

    [Fact]
    public async Task Handle_ForDeactivatedUser_ReturnsSameGenericSuccessAndSendsNothing()
    {
        await RegisterUserAsync();
        var user = await _userRepository.GetByEmailAsync(
            SmartTaxi.Domain.Identity.ValueObjects.Email.Create("user@example.com"), CancellationToken.None);
        user!.Deactivate();
        await _userRepository.UpdateAsync(user, CancellationToken.None);

        var result = await _handler.Handle(new RequestEmailVerificationCommand("user@example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_emailSender.SentMessages);
    }

    [Fact]
    public async Task Handle_RequestedTwiceImmediately_SecondRequestIsSilentlyThrottled()
    {
        await RegisterUserAsync();

        await _handler.Handle(new RequestEmailVerificationCommand("user@example.com"), CancellationToken.None);
        var secondResult = await _handler.Handle(new RequestEmailVerificationCommand("user@example.com"), CancellationToken.None);

        Assert.True(secondResult.IsSuccess);
        Assert.Equal(1, _tokenRepository.Count);
        Assert.Single(_emailSender.SentMessages);
    }

    [Fact]
    public async Task Handle_CalledAgain_InvalidatesThePreviousToken()
    {
        await RegisterUserAsync();
        await _handler.Handle(new RequestEmailVerificationCommand("user@example.com"), CancellationToken.None);
        var firstRawToken = _tokenGenerator.LastGenerated!;

        // Force past the resend interval so the second request actually issues a new token.
        var pastPolicy = new FakeEmailVerificationPolicy { ResendInterval = TimeSpan.Zero };
        var handlerWithNoThrottle = new RequestEmailVerificationCommandHandler(
            _userRepository, _tokenRepository, _tokenGenerator, _tokenHasher, pastPolicy, _emailSender);
        await handlerWithNoThrottle.Handle(new RequestEmailVerificationCommand("user@example.com"), CancellationToken.None);

        var firstTokenHash = _tokenHasher.Hash(firstRawToken);
        var firstToken = await _tokenRepository.GetByTokenHashAsync(firstTokenHash, CancellationToken.None);

        Assert.False(firstToken!.IsValid(DateTime.UtcNow));
        Assert.Equal(2, _tokenRepository.Count);
    }
}

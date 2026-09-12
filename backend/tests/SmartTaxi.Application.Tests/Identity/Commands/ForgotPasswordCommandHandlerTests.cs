using SmartTaxi.Application.Identity.Commands.ForgotPassword;
using SmartTaxi.Application.Identity.Commands.RegisterUser;
using SmartTaxi.Application.Tests.TestDoubles;

namespace SmartTaxi.Application.Tests.Identity.Commands;

public class ForgotPasswordCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakePasswordResetTokenRepository _tokenRepository = new();
    private readonly FakeRefreshTokenGenerator _tokenGenerator = new();
    private readonly FakeRefreshTokenHasher _tokenHasher = new();
    private readonly FakeEmailSender _emailSender = new();
    private readonly ForgotPasswordCommandHandler _handler;

    public ForgotPasswordCommandHandlerTests()
    {
        _handler = new ForgotPasswordCommandHandler(
            _userRepository, _tokenRepository, _tokenGenerator, _tokenHasher, new FakePasswordResetPolicy(), _emailSender);
    }

    [Fact]
    public async Task Handle_ForRegisteredUser_IssuesTokenAndReturnsGenericSuccess()
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher, new FakeNotificationDispatcher());
        await registerHandler.Handle(new RegisterUserCommand("user@example.com", "correct-password"), CancellationToken.None);

        var result = await _handler.Handle(new ForgotPasswordCommand("user@example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _tokenRepository.Count);
        Assert.Single(_emailSender.SentMessages);
    }

    [Fact]
    public async Task Handle_ForUnknownEmail_ReturnsSameGenericSuccessAndSendsNothing()
    {
        var knownResult = await RunWithRegisteredUserAsync();
        var unknownResult = await _handler.Handle(new ForgotPasswordCommand("unknown@example.com"), CancellationToken.None);

        Assert.Equal(knownResult.IsSuccess, unknownResult.IsSuccess);
        Assert.Single(_emailSender.SentMessages); // only the known-user request sent anything
    }

    private async Task<SmartTaxi.Application.Common.Result> RunWithRegisteredUserAsync()
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher, new FakeNotificationDispatcher());
        await registerHandler.Handle(new RegisterUserCommand("known@example.com", "correct-password"), CancellationToken.None);
        return await _handler.Handle(new ForgotPasswordCommand("known@example.com"), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ForDeactivatedUser_ReturnsSameGenericSuccessAndSendsNothing()
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher, new FakeNotificationDispatcher());
        var registerResult = await registerHandler.Handle(new RegisterUserCommand("user@example.com", "correct-password"), CancellationToken.None);
        var user = await _userRepository.GetByIdAsync(registerResult.Value!.UserId, CancellationToken.None);
        user!.Deactivate();
        await _userRepository.UpdateAsync(user, CancellationToken.None);

        var result = await _handler.Handle(new ForgotPasswordCommand("user@example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_emailSender.SentMessages);
    }
}

using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Commands.ConfirmTwoFactor;
using SmartTaxi.Application.Identity.Commands.DisableTwoFactor;
using SmartTaxi.Application.Identity.Commands.EnrollTwoFactor;
using SmartTaxi.Application.Identity.Commands.RegisterUser;
using SmartTaxi.Application.Identity.Sessions;
using SmartTaxi.Application.Tests.TestDoubles;

namespace SmartTaxi.Application.Tests.Identity.Commands;

public class DisableTwoFactorCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeTotpService _totpService = new();
    private readonly FakeTwoFactorSecretProtector _protector = new();
    private readonly FakeTwoFactorRecoveryCodeRepository _recoveryCodeRepository = new();
    private readonly FakeTwoFactorPolicy _policy = new();

    private readonly DisableTwoFactorCommandHandler _handler;
    private readonly EnrollTwoFactorCommandHandler _enrollHandler;
    private readonly ConfirmTwoFactorCommandHandler _confirmHandler;

    public DisableTwoFactorCommandHandlerTests()
    {
        var recoveryCodeService = new RecoveryCodeService(
            _recoveryCodeRepository, new FakeRefreshTokenGenerator(), new FakeRefreshTokenHasher(), _policy);

        _handler = new DisableTwoFactorCommandHandler(
            _userRepository, _passwordHasher, _totpService, _protector, _recoveryCodeRepository, recoveryCodeService);
        _enrollHandler = new EnrollTwoFactorCommandHandler(_userRepository, _totpService, _protector, _policy);
        _confirmHandler = new ConfirmTwoFactorCommandHandler(_userRepository, _totpService, _protector, recoveryCodeService);
    }

    private async Task<(Guid UserId, string Secret)> RegisterAndEnableTwoFactorAsync(
        string email = "user@example.com", string password = "correct-password")
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher);
        var registerResult = await registerHandler.Handle(new RegisterUserCommand(email, password), CancellationToken.None);
        var userId = registerResult.Value!.UserId;

        var enrollResult = await _enrollHandler.Handle(new EnrollTwoFactorCommand(userId), CancellationToken.None);
        var code = FakeTotpService.CodeFor(enrollResult.Value!.Secret);
        await _confirmHandler.Handle(new ConfirmTwoFactorCommand(userId, code), CancellationToken.None);

        return (userId, enrollResult.Value.Secret);
    }

    [Fact]
    public async Task Handle_WithCorrectPasswordAndCode_DisablesTwoFactorAndDeletesRecoveryCodes()
    {
        var (userId, secret) = await RegisterAndEnableTwoFactorAsync();

        var result = await _handler.Handle(
            new DisableTwoFactorCommand(userId, "correct-password", FakeTotpService.CodeFor(secret)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var user = await _userRepository.GetByIdAsync(userId, CancellationToken.None);
        Assert.False(user!.TwoFactorEnabled);
        Assert.Empty(await _recoveryCodeRepository.GetActiveForUserAsync(userId, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ReturnsUnauthorizedAndKeepsTwoFactorEnabled()
    {
        var (userId, secret) = await RegisterAndEnableTwoFactorAsync();

        var result = await _handler.Handle(
            new DisableTwoFactorCommand(userId, "wrong-password", FakeTotpService.CodeFor(secret)), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        var user = await _userRepository.GetByIdAsync(userId, CancellationToken.None);
        Assert.True(user!.TwoFactorEnabled);
    }

    [Fact]
    public async Task Handle_WithCorrectPasswordButWrongCode_ReturnsUnauthorizedAndKeepsTwoFactorEnabled()
    {
        var (userId, _) = await RegisterAndEnableTwoFactorAsync();

        var result = await _handler.Handle(
            new DisableTwoFactorCommand(userId, "correct-password", "wrong-code"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        var user = await _userRepository.GetByIdAsync(userId, CancellationToken.None);
        Assert.True(user!.TwoFactorEnabled);
    }
}

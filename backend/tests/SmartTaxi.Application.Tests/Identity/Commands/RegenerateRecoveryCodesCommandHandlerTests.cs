using SmartTaxi.Application.Identity.Commands.ConfirmTwoFactor;
using SmartTaxi.Application.Identity.Commands.EnrollTwoFactor;
using SmartTaxi.Application.Identity.Commands.RegenerateRecoveryCodes;
using SmartTaxi.Application.Identity.Commands.RegisterUser;
using SmartTaxi.Application.Identity.Sessions;
using SmartTaxi.Application.Tests.TestDoubles;

namespace SmartTaxi.Application.Tests.Identity.Commands;

public class RegenerateRecoveryCodesCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeTotpService _totpService = new();
    private readonly FakeTwoFactorSecretProtector _protector = new();
    private readonly FakeTwoFactorRecoveryCodeRepository _recoveryCodeRepository = new();
    private readonly FakeTwoFactorPolicy _policy = new();

    private readonly RegenerateRecoveryCodesCommandHandler _handler;
    private readonly EnrollTwoFactorCommandHandler _enrollHandler;
    private readonly ConfirmTwoFactorCommandHandler _confirmHandler;

    public RegenerateRecoveryCodesCommandHandlerTests()
    {
        var recoveryCodeService = new RecoveryCodeService(
            _recoveryCodeRepository, new FakeRefreshTokenGenerator(), new FakeRefreshTokenHasher(), _policy);

        _handler = new RegenerateRecoveryCodesCommandHandler(_userRepository, recoveryCodeService);
        _enrollHandler = new EnrollTwoFactorCommandHandler(_userRepository, _totpService, _protector, _policy);
        _confirmHandler = new ConfirmTwoFactorCommandHandler(_userRepository, _totpService, _protector, recoveryCodeService);
    }

    private async Task<Guid> RegisterAndEnableTwoFactorAsync(string email = "user@example.com", string password = "correct-password")
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher, new FakeNotificationDispatcher());
        var registerResult = await registerHandler.Handle(new RegisterUserCommand(email, password), CancellationToken.None);
        var userId = registerResult.Value!.UserId;

        var enrollResult = await _enrollHandler.Handle(new EnrollTwoFactorCommand(userId), CancellationToken.None);
        var code = FakeTotpService.CodeFor(enrollResult.Value!.Secret);
        await _confirmHandler.Handle(new ConfirmTwoFactorCommand(userId, code), CancellationToken.None);

        return userId;
    }

    [Fact]
    public async Task Handle_WithTwoFactorEnabled_IssuesFreshBatchAndInvalidatesPrevious()
    {
        var userId = await RegisterAndEnableTwoFactorAsync();
        var previousCodes = await _recoveryCodeRepository.GetActiveForUserAsync(userId, CancellationToken.None);
        var previousRawCode = previousCodes.First();

        var result = await _handler.Handle(new RegenerateRecoveryCodesCommand(userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(_policy.RecoveryCodeCount, result.Value!.RecoveryCodes.Count);

        var activeAfter = await _recoveryCodeRepository.GetActiveForUserAsync(userId, CancellationToken.None);
        Assert.Equal(_policy.RecoveryCodeCount, activeAfter.Count);
        Assert.DoesNotContain(previousRawCode.Id, activeAfter.Select(c => c.Id));
    }

    [Fact]
    public async Task Handle_WithoutTwoFactorEnabled_ReturnsValidationError()
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher, new FakeNotificationDispatcher());
        var registerResult = await registerHandler.Handle(
            new RegisterUserCommand("no2fa@example.com", "correct-password"), CancellationToken.None);

        var result = await _handler.Handle(
            new RegenerateRecoveryCodesCommand(registerResult.Value!.UserId), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}

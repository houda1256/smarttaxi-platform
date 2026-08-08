using SmartTaxi.Application.Identity.Commands.ConfirmTwoFactor;
using SmartTaxi.Application.Identity.Commands.EnrollTwoFactor;
using SmartTaxi.Application.Identity.Commands.RegisterUser;
using SmartTaxi.Application.Identity.Sessions;
using SmartTaxi.Application.Tests.TestDoubles;

namespace SmartTaxi.Application.Tests.Identity.Commands;

public class ConfirmTwoFactorCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeTotpService _totpService = new();
    private readonly FakeTwoFactorSecretProtector _protector = new();
    private readonly FakeTwoFactorRecoveryCodeRepository _recoveryCodeRepository = new();
    private readonly FakeTwoFactorPolicy _policy = new();
    private readonly ConfirmTwoFactorCommandHandler _handler;
    private readonly EnrollTwoFactorCommandHandler _enrollHandler;

    public ConfirmTwoFactorCommandHandlerTests()
    {
        var recoveryCodeService = new RecoveryCodeService(
            _recoveryCodeRepository, new FakeRefreshTokenGenerator(), new FakeRefreshTokenHasher(), _policy);

        _handler = new ConfirmTwoFactorCommandHandler(_userRepository, _totpService, _protector, recoveryCodeService);
        _enrollHandler = new EnrollTwoFactorCommandHandler(_userRepository, _totpService, _protector, _policy);
    }

    private async Task<Guid> RegisterUserAsync(string email = "user@example.com", string password = "correct-password")
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher);
        var result = await registerHandler.Handle(new RegisterUserCommand(email, password), CancellationToken.None);
        return result.Value!.UserId;
    }

    [Fact]
    public async Task Handle_WithCorrectCode_ActivatesTwoFactorAndIssuesRecoveryCodes()
    {
        var userId = await RegisterUserAsync();
        var enrollResult = await _enrollHandler.Handle(new EnrollTwoFactorCommand(userId), CancellationToken.None);
        var code = FakeTotpService.CodeFor(enrollResult.Value!.Secret);

        var result = await _handler.Handle(new ConfirmTwoFactorCommand(userId, code), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(_policy.RecoveryCodeCount, result.Value!.RecoveryCodes.Count);
        Assert.Equal(_policy.RecoveryCodeCount, result.Value.RecoveryCodes.Distinct().Count());

        var user = await _userRepository.GetByIdAsync(userId, CancellationToken.None);
        Assert.True(user!.TwoFactorEnabled);
    }

    [Fact]
    public async Task Handle_WithWrongCode_DoesNotActivateTwoFactor()
    {
        var userId = await RegisterUserAsync();
        await _enrollHandler.Handle(new EnrollTwoFactorCommand(userId), CancellationToken.None);

        var result = await _handler.Handle(new ConfirmTwoFactorCommand(userId, "wrong-code"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        var user = await _userRepository.GetByIdAsync(userId, CancellationToken.None);
        Assert.False(user!.TwoFactorEnabled);
    }

    [Fact]
    public async Task Handle_WithNoPendingEnrollment_ReturnsValidationError()
    {
        var userId = await RegisterUserAsync();

        var result = await _handler.Handle(new ConfirmTwoFactorCommand(userId, "any-code"), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}

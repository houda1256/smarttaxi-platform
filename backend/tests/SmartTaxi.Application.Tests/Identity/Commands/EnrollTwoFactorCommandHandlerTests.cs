using SmartTaxi.Application.Identity.Commands.EnrollTwoFactor;
using SmartTaxi.Application.Identity.Commands.RegisterUser;
using SmartTaxi.Application.Tests.TestDoubles;

namespace SmartTaxi.Application.Tests.Identity.Commands;

public class EnrollTwoFactorCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeTotpService _totpService = new();
    private readonly FakeTwoFactorSecretProtector _protector = new();
    private readonly FakeTwoFactorPolicy _policy = new();
    private readonly EnrollTwoFactorCommandHandler _handler;

    public EnrollTwoFactorCommandHandlerTests()
    {
        _handler = new EnrollTwoFactorCommandHandler(_userRepository, _totpService, _protector, _policy);
    }

    private async Task<Guid> RegisterUserAsync(string email = "user@example.com", string password = "correct-password")
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher);
        var result = await registerHandler.Handle(new RegisterUserCommand(email, password), CancellationToken.None);
        return result.Value!.UserId;
    }

    [Fact]
    public async Task Handle_ForActiveUser_ReturnsSecretAndDoesNotActivateTwoFactor()
    {
        var userId = await RegisterUserAsync();

        var result = await _handler.Handle(new EnrollTwoFactorCommand(userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value!.Secret));
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AuthenticatorUri));

        var user = await _userRepository.GetByIdAsync(userId, CancellationToken.None);
        Assert.False(user!.TwoFactorEnabled);
    }

    [Fact]
    public async Task Handle_WhenAlreadyActive_DoesNotDisruptExistingActiveSecret()
    {
        var userId = await RegisterUserAsync();
        var user = await _userRepository.GetByIdAsync(userId, CancellationToken.None);
        var utcNow = DateTime.UtcNow;
        user!.BeginTwoFactorEnrollment("protected:already-active", utcNow);
        user.ConfirmTwoFactorEnrollment(utcNow);
        await _userRepository.UpdateAsync(user, CancellationToken.None);

        await _handler.Handle(new EnrollTwoFactorCommand(userId), CancellationToken.None);

        var reloaded = await _userRepository.GetByIdAsync(userId, CancellationToken.None);
        Assert.True(reloaded!.TwoFactorEnabled);
        Assert.Equal("protected:already-active", reloaded.TwoFactorActiveSecretEncrypted);
    }

    [Fact]
    public async Task Handle_ForUnknownUser_ReturnsUnauthorized()
    {
        var result = await _handler.Handle(new EnrollTwoFactorCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}

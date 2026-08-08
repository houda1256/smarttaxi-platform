using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Commands.RegisterUser;
using SmartTaxi.Application.Identity.Commands.RequestPhoneVerification;
using SmartTaxi.Application.Tests.TestDoubles;

namespace SmartTaxi.Application.Tests.Identity.Commands;

public class RequestPhoneVerificationCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakePhoneVerificationOtpRepository _otpRepository = new();
    private readonly FakeOtpGenerator _otpGenerator = new();
    private readonly FakeRefreshTokenHasher _hasher = new();
    private readonly FakeSmsSender _smsSender = new();
    private readonly RequestPhoneVerificationCommandHandler _handler;

    public RequestPhoneVerificationCommandHandlerTests()
    {
        _handler = new RequestPhoneVerificationCommandHandler(
            _userRepository, _otpRepository, _otpGenerator, _hasher, new FakePhoneVerificationPolicy(), _smsSender);
    }

    private async Task<Guid> RegisterUserAsync()
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher);
        var result = await registerHandler.Handle(new RegisterUserCommand("user@example.com", "correct-password"), CancellationToken.None);
        return result.Value!.UserId;
    }

    [Fact]
    public async Task Handle_WithValidPhoneNumber_IssuesOtpAndSendsSms()
    {
        var userId = await RegisterUserAsync();

        var result = await _handler.Handle(new RequestPhoneVerificationCommand(userId, "+21612345678"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _otpRepository.Count);
        Assert.Single(_smsSender.SentMessages);
    }

    [Fact]
    public async Task Handle_WithInvalidPhoneFormat_ReturnsValidationFailure()
    {
        var userId = await RegisterUserAsync();

        var result = await _handler.Handle(new RequestPhoneVerificationCommand(userId, "not-a-phone"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Empty(_smsSender.SentMessages);
    }

    [Fact]
    public async Task Handle_RequestedTwiceWithinResendInterval_SecondRequestIsRejected()
    {
        var userId = await RegisterUserAsync();
        await _handler.Handle(new RequestPhoneVerificationCommand(userId, "+21612345678"), CancellationToken.None);

        var secondResult = await _handler.Handle(new RequestPhoneVerificationCommand(userId, "+21612345678"), CancellationToken.None);

        Assert.False(secondResult.IsSuccess);
        Assert.Equal(ErrorType.Validation, secondResult.ErrorType);
        Assert.Single(_smsSender.SentMessages);
    }

    [Fact]
    public async Task Handle_ForDeactivatedUser_ReturnsUnauthorized()
    {
        var userId = await RegisterUserAsync();
        var user = await _userRepository.GetByIdAsync(userId, CancellationToken.None);
        user!.Deactivate();
        await _userRepository.UpdateAsync(user, CancellationToken.None);

        var result = await _handler.Handle(new RequestPhoneVerificationCommand(userId, "+21612345678"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        Assert.Empty(_smsSender.SentMessages);
    }
}

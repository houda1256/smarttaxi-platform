using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Commands.ConfirmPhoneVerification;
using SmartTaxi.Application.Identity.Commands.RegisterUser;
using SmartTaxi.Application.Identity.Commands.RequestPhoneVerification;
using SmartTaxi.Application.Tests.TestDoubles;

namespace SmartTaxi.Application.Tests.Identity.Commands;

public class ConfirmPhoneVerificationCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakePhoneVerificationOtpRepository _otpRepository = new();
    private readonly FakeOtpGenerator _otpGenerator = new();
    private readonly FakeRefreshTokenHasher _hasher = new();
    private readonly FakeSmsSender _smsSender = new();
    private readonly FakePhoneVerificationPolicy _policy = new();
    private readonly RequestPhoneVerificationCommandHandler _requestHandler;
    private readonly ConfirmPhoneVerificationCommandHandler _confirmHandler;

    public ConfirmPhoneVerificationCommandHandlerTests()
    {
        _requestHandler = new RequestPhoneVerificationCommandHandler(
            _userRepository, _otpRepository, _otpGenerator, _hasher, _policy, _smsSender);
        _confirmHandler = new ConfirmPhoneVerificationCommandHandler(_userRepository, _otpRepository, _hasher, _policy);
    }

    private async Task<Guid> RegisterAndRequestOtpAsync()
    {
        var registerHandler = new RegisterUserCommandHandler(_userRepository, _passwordHasher, new FakeNotificationDispatcher());
        var result = await registerHandler.Handle(new RegisterUserCommand("user@example.com", "correct-password"), CancellationToken.None);
        var userId = result.Value!.UserId;
        await _requestHandler.Handle(new RequestPhoneVerificationCommand(userId, "+21612345678"), CancellationToken.None);
        return userId;
    }

    [Fact]
    public async Task Handle_WithCorrectOtp_VerifiesPhoneWithinItsLifetime()
    {
        var userId = await RegisterAndRequestOtpAsync();
        var rawOtp = _otpGenerator.LastGenerated!;

        var result = await _confirmHandler.Handle(new ConfirmPhoneVerificationCommand(userId, rawOtp), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var user = await _userRepository.GetByIdAsync(userId, CancellationToken.None);
        Assert.NotNull(user!.PhoneVerifiedAt);
    }

    [Fact]
    public async Task Handle_WithWrongOtp_IsRejectedAndRecordsAnAttempt()
    {
        var userId = await RegisterAndRequestOtpAsync();

        var result = await _confirmHandler.Handle(new ConfirmPhoneVerificationCommand(userId, "000000"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        var otp = await _otpRepository.GetLatestForUserAsync(userId, CancellationToken.None);
        Assert.Equal(1, otp!.AttemptCount);
    }

    [Fact]
    public async Task Handle_AfterMaxWrongAttempts_LocksTheOtpEvenForTheCorrectCode()
    {
        var userId = await RegisterAndRequestOtpAsync();
        var rawOtp = _otpGenerator.LastGenerated!;

        for (var i = 0; i < _policy.MaxAttempts; i++)
        {
            await _confirmHandler.Handle(new ConfirmPhoneVerificationCommand(userId, "000000"), CancellationToken.None);
        }

        var result = await _confirmHandler.Handle(new ConfirmPhoneVerificationCommand(userId, rawOtp), CancellationToken.None);

        Assert.False(result.IsSuccess);
        var user = await _userRepository.GetByIdAsync(userId, CancellationToken.None);
        Assert.Null(user!.PhoneVerifiedAt);
    }

    [Fact]
    public async Task Handle_ReusingAnAlreadyConsumedOtp_IsRejected()
    {
        var userId = await RegisterAndRequestOtpAsync();
        var rawOtp = _otpGenerator.LastGenerated!;
        await _confirmHandler.Handle(new ConfirmPhoneVerificationCommand(userId, rawOtp), CancellationToken.None);

        var secondAttempt = await _confirmHandler.Handle(new ConfirmPhoneVerificationCommand(userId, rawOtp), CancellationToken.None);

        Assert.False(secondAttempt.IsSuccess);
    }

    [Fact]
    public async Task Handle_WithExpiredOtp_IsRejected()
    {
        var userId = await RegisterAndRequestOtpAsync();
        var rawOtp = _otpGenerator.LastGenerated!;
        await _otpRepository.InvalidateActiveForUserAsync(userId, DateTime.UtcNow, CancellationToken.None);

        var result = await _confirmHandler.Handle(new ConfirmPhoneVerificationCommand(userId, rawOtp), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ForDeactivatedUser_IsRejected()
    {
        var userId = await RegisterAndRequestOtpAsync();
        var rawOtp = _otpGenerator.LastGenerated!;
        var user = await _userRepository.GetByIdAsync(userId, CancellationToken.None);
        user!.Deactivate();
        await _userRepository.UpdateAsync(user, CancellationToken.None);

        var result = await _confirmHandler.Handle(new ConfirmPhoneVerificationCommand(userId, rawOtp), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(user.PhoneVerifiedAt);
    }
}

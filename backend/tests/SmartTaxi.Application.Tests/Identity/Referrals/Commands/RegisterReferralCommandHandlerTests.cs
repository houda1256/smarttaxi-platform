using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Referrals.Commands.RegisterReferral;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.Identity.Referrals.Commands;

public class RegisterReferralCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeReferralRepository _referralRepository = new();
    private readonly RegisterReferralCommandHandler _handler;

    public RegisterReferralCommandHandlerTests()
    {
        _handler = new RegisterReferralCommandHandler(_userRepository, _referralRepository);
    }

    private async Task<User> CreateUserWithReferralCodeAsync(string code)
    {
        var user = User.Create(Email.Create($"{Guid.NewGuid()}@example.com"), HashedPassword.Create("hashed"), UserRole.Customer);
        user.EnsureReferralCode(code);
        await _userRepository.AddAsync(user, CancellationToken.None);
        return user;
    }

    [Fact]
    public async Task Handle_WithValidCodeAndNewReferee_CreatesReferral()
    {
        var referrer = await CreateUserWithReferralCodeAsync("SPONSOR1");
        var refereeId = Guid.NewGuid();

        var result = await _handler.Handle(new RegisterReferralCommand("SPONSOR1", refereeId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _referralRepository.Count);
        var referral = await _referralRepository.GetByRefereeUserIdAsync(refereeId, CancellationToken.None);
        Assert.Equal(referrer.Id, referral!.ReferrerUserId);
    }

    [Fact]
    public async Task Handle_WithUnknownCode_ReturnsValidationErrorAndCreatesNoReferral()
    {
        var result = await _handler.Handle(
            new RegisterReferralCommand("UNKNOWN", Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(0, _referralRepository.Count);
    }

    [Fact]
    public async Task Handle_WhenRefereeIsTheReferrerThemselves_ReturnsValidationError()
    {
        var referrer = await CreateUserWithReferralCodeAsync("SELFCODE");

        var result = await _handler.Handle(new RegisterReferralCommand("SELFCODE", referrer.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(0, _referralRepository.Count);
    }

    [Fact]
    public async Task Handle_WhenRefereeAlreadyHasASponsor_ReturnsConflict()
    {
        var firstSponsor = await CreateUserWithReferralCodeAsync("FIRST01");
        var secondSponsor = await CreateUserWithReferralCodeAsync("SECOND02");
        var refereeId = Guid.NewGuid();
        await _handler.Handle(new RegisterReferralCommand("FIRST01", refereeId), CancellationToken.None);

        var result = await _handler.Handle(new RegisterReferralCommand("SECOND02", refereeId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(1, _referralRepository.Count);
    }
}

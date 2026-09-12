using SmartTaxi.Application.Identity.Referrals.Commands.EvaluateReferralActivation;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.Referrals.Entities;
using SmartTaxi.Domain.Identity.Referrals.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.Identity.Referrals.Commands;

public class EvaluateReferralActivationCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeReferralRepository _referralRepository = new();

    private async Task<User> CreateUserAsync()
    {
        var user = User.Create(Email.Create($"{Guid.NewGuid()}@example.com"), HashedPassword.Create("hashed"), UserRole.Customer, DateTime.UtcNow);
        await _userRepository.AddAsync(user, CancellationToken.None);
        return user;
    }

    [Fact]
    public async Task Handle_WhenRefereeEmailIsVerified_ActivatesTheReferral()
    {
        var handler = new EvaluateReferralActivationCommandHandler(
            _referralRepository, _userRepository, new FakeReferralActivationPolicy { RequireEmailVerified = true });
        var referee = await CreateUserAsync();
        referee.VerifyEmail(DateTime.UtcNow);
        await _userRepository.UpdateAsync(referee, CancellationToken.None);

        var referral = new Referral(Guid.NewGuid(), referee.Id, "CODE", DateTime.UtcNow);
        await _referralRepository.AddAsync(referral, CancellationToken.None);

        var result = await handler.Handle(new EvaluateReferralActivationCommand(referral.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        var reloaded = await _referralRepository.GetByIdAsync(referral.Id, CancellationToken.None);
        Assert.Equal(ReferralStatus.RewardEligible, reloaded!.Status);
    }

    [Fact]
    public async Task Handle_WhenRefereeEmailNotYetVerified_DoesNotActivateAndIsNotAnError()
    {
        var handler = new EvaluateReferralActivationCommandHandler(
            _referralRepository, _userRepository, new FakeReferralActivationPolicy { RequireEmailVerified = true });
        var referee = await CreateUserAsync();

        var referral = new Referral(Guid.NewGuid(), referee.Id, "CODE", DateTime.UtcNow);
        await _referralRepository.AddAsync(referral, CancellationToken.None);

        var result = await handler.Handle(new EvaluateReferralActivationCommand(referral.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
        var reloaded = await _referralRepository.GetByIdAsync(referral.Id, CancellationToken.None);
        Assert.Equal(ReferralStatus.PendingActivation, reloaded!.Status);
    }

    [Fact]
    public async Task Handle_WhenMinAccountAgeNotYetReached_DoesNotActivate()
    {
        var handler = new EvaluateReferralActivationCommandHandler(
            _referralRepository, _userRepository,
            new FakeReferralActivationPolicy { RequireEmailVerified = false, MinAccountAgeDays = 7 });
        var referee = await CreateUserAsync();

        var referral = new Referral(Guid.NewGuid(), referee.Id, "CODE", DateTime.UtcNow);
        await _referralRepository.AddAsync(referral, CancellationToken.None);

        var result = await handler.Handle(new EvaluateReferralActivationCommand(referral.Id), CancellationToken.None);

        Assert.False(result.Value);
    }
}

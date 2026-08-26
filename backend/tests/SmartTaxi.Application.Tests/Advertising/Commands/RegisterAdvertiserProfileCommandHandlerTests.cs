using SmartTaxi.Application.Advertising.Commands.RegisterAdvertiserProfile;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.Advertising.Commands;

public class RegisterAdvertiserProfileCommandHandlerTests
{
    private readonly FakeAdvertiserProfileRepository _profileRepository = new();
    private readonly FakeUserRepository _userRepository = new();
    private readonly RegisterAdvertiserProfileCommandHandler _handler;

    public RegisterAdvertiserProfileCommandHandlerTests()
    {
        _handler = new RegisterAdvertiserProfileCommandHandler(_profileRepository, _userRepository);
    }

    private async Task<User> CreateUserAsync(UserRole role)
    {
        var user = User.Create(Email.Create($"user-{Guid.NewGuid():N}@example.com"), HashedPassword.Create("hash"), role);
        await _userRepository.AddAsync(user, CancellationToken.None);
        return user;
    }

    [Fact]
    public async Task Handle_AdvertiserRole_Succeeds()
    {
        var user = await CreateUserAsync(UserRole.Advertiser);

        var result = await _handler.Handle(
            new RegisterAdvertiserProfileCommand(user.Id, "Acme Ads", "Acme SARL", "TAX1", "Tunis", "addr", "a@acme.tn", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_NonAdvertiserRole_ReturnsForbidden()
    {
        var user = await CreateUserAsync(UserRole.Customer);

        var result = await _handler.Handle(
            new RegisterAdvertiserProfileCommand(user.Id, "Acme Ads", "Acme SARL", "TAX1", "Tunis", "addr", "a@acme.tn", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Handle_AlreadyRegistered_ReturnsConflict()
    {
        var user = await CreateUserAsync(UserRole.Advertiser);
        await _handler.Handle(new RegisterAdvertiserProfileCommand(user.Id, "Acme Ads", "Acme SARL", "TAX1", "Tunis", "addr", "a@acme.tn", null), CancellationToken.None);

        var result = await _handler.Handle(
            new RegisterAdvertiserProfileCommand(user.Id, "Acme Ads 2", "Acme SARL", "TAX1", "Tunis", "addr", "a@acme.tn", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}

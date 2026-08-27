using SmartTaxi.Application.Common;
using SmartTaxi.Application.Maintenance.Commands.RegisterGarageProfile;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.Maintenance.Commands;

public class RegisterGarageProfileCommandHandlerTests
{
    private readonly FakeGarageProfileRepository _profileRepository = new();
    private readonly FakeUserRepository _userRepository = new();
    private readonly RegisterGarageProfileCommandHandler _handler;

    public RegisterGarageProfileCommandHandlerTests()
    {
        _handler = new RegisterGarageProfileCommandHandler(_profileRepository, _userRepository);
    }

    private async Task<User> CreateUserAsync(UserRole role)
    {
        var user = User.Create(Email.Create($"user-{Guid.NewGuid():N}@example.com"), HashedPassword.Create("hash"), role, DateTime.UtcNow);
        await _userRepository.AddAsync(user, CancellationToken.None);
        return user;
    }

    [Fact]
    public async Task Handle_GaragePartnerRole_Succeeds()
    {
        var user = await CreateUserAsync(UserRole.GaragePartner);

        var result = await _handler.Handle(
            new RegisterGarageProfileCommand(user.Id, "Garage Central", "Garage Central SARL", "12 rue X", "Tunis", "Standard", "Vidange"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_NonGaragePartnerRole_ReturnsForbidden()
    {
        var user = await CreateUserAsync(UserRole.Customer);

        var result = await _handler.Handle(
            new RegisterGarageProfileCommand(user.Id, "Garage Central", null, "12 rue X", "Tunis", null, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Handle_AlreadyRegistered_ReturnsConflict()
    {
        var user = await CreateUserAsync(UserRole.GaragePartner);
        await _handler.Handle(new RegisterGarageProfileCommand(user.Id, "Garage Central", null, "12 rue X", "Tunis", null, null), CancellationToken.None);

        var result = await _handler.Handle(
            new RegisterGarageProfileCommand(user.Id, "Garage Central 2", null, "12 rue X", "Tunis", null, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}

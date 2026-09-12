using SmartTaxi.Application.Common;
using SmartTaxi.Application.RoadsideAssistance.Commands.RegisterRoadsidePartnerProfile;
using SmartTaxi.Application.RoadsideAssistance.Commands.UpdateRoadsidePartnerProfile;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.RoadsideAssistance.Commands;

public class RegisterAndUpdateRoadsidePartnerProfileCommandHandlerTests
{
    private readonly FakeRoadsidePartnerProfileRepository _profileRepository = new();
    private readonly FakeUserRepository _userRepository = new();
    private readonly RegisterRoadsidePartnerProfileCommandHandler _registerHandler;
    private readonly UpdateRoadsidePartnerProfileCommandHandler _updateHandler;

    public RegisterAndUpdateRoadsidePartnerProfileCommandHandlerTests()
    {
        _registerHandler = new RegisterRoadsidePartnerProfileCommandHandler(_profileRepository, _userRepository);
        _updateHandler = new UpdateRoadsidePartnerProfileCommandHandler(_profileRepository);
    }

    private async Task<User> CreateUserAsync(UserRole role)
    {
        var user = User.Create(Email.Create($"user-{Guid.NewGuid():N}@example.com"), HashedPassword.Create("hash"), role, DateTime.UtcNow);
        await _userRepository.AddAsync(user, CancellationToken.None);
        return user;
    }

    [Fact]
    public async Task Register_RoadsideAssistancePartnerRole_Succeeds()
    {
        var user = await CreateUserAsync(UserRole.RoadsideAssistancePartner);

        var result = await _registerHandler.Handle(
            new RegisterRoadsidePartnerProfileCommand(user.Id, "Assistance Rapide", null, "12 rue X", "Tunis", "Towing", "Standard", 36.8, 10.18),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Register_NonRoadsideAssistancePartnerRole_ReturnsForbidden()
    {
        var user = await CreateUserAsync(UserRole.Customer);

        var result = await _registerHandler.Handle(
            new RegisterRoadsidePartnerProfileCommand(user.Id, "Assistance Rapide", null, "12 rue X", "Tunis", null, null, null, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Register_AlreadyRegistered_ReturnsConflict()
    {
        var user = await CreateUserAsync(UserRole.RoadsideAssistancePartner);
        await _registerHandler.Handle(
            new RegisterRoadsidePartnerProfileCommand(user.Id, "Assistance Rapide", null, "12 rue X", "Tunis", null, null, null, null),
            CancellationToken.None);

        var result = await _registerHandler.Handle(
            new RegisterRoadsidePartnerProfileCommand(user.Id, "Autre Nom", null, "12 rue X", "Tunis", null, null, null, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Update_ExistingProfile_UpdatesFields()
    {
        var user = await CreateUserAsync(UserRole.RoadsideAssistancePartner);
        await _registerHandler.Handle(
            new RegisterRoadsidePartnerProfileCommand(user.Id, "Assistance Rapide", null, "12 rue X", "Tunis", null, null, null, null),
            CancellationToken.None);

        var result = await _updateHandler.Handle(
            new UpdateRoadsidePartnerProfileCommand(user.Id, "New Name", null, "New Addr", "Sousse", "Towing", "Van", 35.8, 10.6),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _profileRepository.GetByUserIdAsync(user.Id, CancellationToken.None);
        Assert.Equal("New Name", reloaded!.BusinessName);
        Assert.Equal("SOUSSE", reloaded.City);
    }

    [Fact]
    public async Task Update_UnknownProfile_ReturnsNotFound()
    {
        var result = await _updateHandler.Handle(
            new UpdateRoadsidePartnerProfileCommand(Guid.NewGuid(), "New Name", null, "New Addr", "Sousse", null, null, null, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}

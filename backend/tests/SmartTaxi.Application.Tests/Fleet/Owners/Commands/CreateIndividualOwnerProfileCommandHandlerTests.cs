using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Owners.Commands.CreateIndividualOwnerProfile;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.Fleet.Owners.Commands;

public class CreateIndividualOwnerProfileCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeTaxiOwnerProfileRepository _repository = new();
    private readonly CreateIndividualOwnerProfileCommandHandler _handler;

    public CreateIndividualOwnerProfileCommandHandlerTests()
    {
        _handler = new CreateIndividualOwnerProfileCommandHandler(_userRepository, _repository);
    }

    private static CreateIndividualOwnerProfileCommand MakeCommand(Guid userId) => new(
        userId, "Ali", "Ben", "NID1", "Street", "City", "20000", "Maroc",
        "Bank", "Ali Ben", "AC123", null);

    private async Task<Guid> CreateUserWithRoleAsync(UserRole role)
    {
        var user = User.Create(Email.Create($"{Guid.NewGuid()}@example.com"), HashedPassword.Create("hashed"), role);
        await _userRepository.AddAsync(user, CancellationToken.None);
        return user.Id;
    }

    [Fact]
    public async Task Handle_ForUserWithTaxiOwnerRole_CreatesProfile()
    {
        var userId = await CreateUserWithRoleAsync(UserRole.TaxiOwner);

        var result = await _handler.Handle(MakeCommand(userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _repository.Count);
    }

    [Fact]
    public async Task Handle_ForUserWithoutTaxiOwnerRole_ReturnsForbidden()
    {
        var userId = await CreateUserWithRoleAsync(UserRole.Customer);

        var result = await _handler.Handle(MakeCommand(userId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenProfileAlreadyExists_ReturnsConflict()
    {
        var userId = await CreateUserWithRoleAsync(UserRole.TaxiOwner);
        await _handler.Handle(MakeCommand(userId), CancellationToken.None);

        var result = await _handler.Handle(MakeCommand(userId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}

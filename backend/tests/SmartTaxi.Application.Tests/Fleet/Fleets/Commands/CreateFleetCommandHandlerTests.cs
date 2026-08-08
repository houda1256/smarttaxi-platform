using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Fleets.Commands.CreateFleet;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Tests.Fleet.Fleets.Commands;

public class CreateFleetCommandHandlerTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeFleetRepository _repository = new();
    private readonly CreateFleetCommandHandler _handler;

    public CreateFleetCommandHandlerTests()
    {
        _handler = new CreateFleetCommandHandler(_userRepository, _repository);
    }

    [Fact]
    public async Task Handle_ForTaxiOwner_CreatesFleet()
    {
        var user = User.Create(Email.Create($"{Guid.NewGuid()}@example.com"), HashedPassword.Create("h"), UserRole.TaxiOwner);
        await _userRepository.AddAsync(user, CancellationToken.None);

        var result = await _handler.Handle(new CreateFleetCommand(user.Id, "My Fleet", "desc", Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var fleets = await _repository.GetForOwnerAsync(user.Id, CancellationToken.None);
        Assert.Single(fleets);
    }

    [Fact]
    public async Task Handle_ForNonTaxiOwner_ReturnsForbidden()
    {
        var user = User.Create(Email.Create($"{Guid.NewGuid()}@example.com"), HashedPassword.Create("h"), UserRole.Customer);
        await _userRepository.AddAsync(user, CancellationToken.None);

        var result = await _handler.Handle(new CreateFleetCommand(user.Id, "My Fleet", null, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }
}

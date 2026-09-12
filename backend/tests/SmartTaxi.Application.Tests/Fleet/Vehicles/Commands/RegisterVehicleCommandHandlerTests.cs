using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Vehicles.Commands.RegisterVehicle;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.Application.Tests.Fleet.Vehicles.Commands;

public class RegisterVehicleCommandHandlerTests
{
    private readonly FakeVehicleRepository _repository = new();
    private readonly FakeFleetRepository _fleetRepository = new();
    private readonly RegisterVehicleCommandHandler _handler;

    public RegisterVehicleCommandHandlerTests()
    {
        _handler = new RegisterVehicleCommandHandler(_repository, _fleetRepository);
    }

    private static RegisterVehicleCommand MakeCommand(Guid ownerId, string plate, string? vin) => new(
        ownerId, null, "Toyota", "Corolla", 2022, "White", plate, vin, 0,
        FuelType.Petrol, TransmissionType.Automatic, 5, true, false, VehicleCategory.Standard);

    [Fact]
    public async Task Handle_WithUniquePlateAndVin_RegistersVehicle()
    {
        var result = await _handler.Handle(MakeCommand(Guid.NewGuid(), "AA-123-BB", "VIN1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WithDuplicatePlate_ReturnsConflict()
    {
        await _handler.Handle(MakeCommand(Guid.NewGuid(), "AA-123-BB", "VIN1"), CancellationToken.None);

        var result = await _handler.Handle(MakeCommand(Guid.NewGuid(), "AA-123-BB", "VIN2"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithDuplicateVin_ReturnsConflict()
    {
        await _handler.Handle(MakeCommand(Guid.NewGuid(), "AA-123-BB", "VIN1"), CancellationToken.None);

        var result = await _handler.Handle(MakeCommand(Guid.NewGuid(), "CC-456-DD", "VIN1"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithoutVin_AllowsMultipleVehiclesWithNoVin()
    {
        var first = await _handler.Handle(MakeCommand(Guid.NewGuid(), "AA-111-AA", null), CancellationToken.None);
        var second = await _handler.Handle(MakeCommand(Guid.NewGuid(), "BB-222-BB", null), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
    }
}

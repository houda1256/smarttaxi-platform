using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Vehicles.Commands.ApproveVehicle;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.Application.Tests.Fleet.Vehicles.Commands;

public class ApproveVehicleCommandHandlerTests
{
    private readonly FakeVehicleRepository _repository = new();
    private readonly ApproveVehicleCommandHandler _handler;

    public ApproveVehicleCommandHandlerTests()
    {
        _handler = new ApproveVehicleCommandHandler(_repository);
    }

    private static Vehicle RegisterVehicle(Guid ownerId) => Vehicle.Register(
        ownerId, null, "Toyota", "Corolla", 2022, "White", "AA-123-BB", null, 0,
        FuelType.Petrol, TransmissionType.Automatic, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);

    [Fact]
    public async Task Handle_ByReviewer_ApprovesVehicleAndMakesItActive()
    {
        var vehicle = RegisterVehicle(Guid.NewGuid());
        await _repository.AddAsync(vehicle, CancellationToken.None);

        var result = await _handler.Handle(new ApproveVehicleCommand(Guid.NewGuid(), vehicle.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _repository.GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(VehicleVerificationStatus.Approved, reloaded!.VerificationStatus);
        Assert.Equal(VehicleOperationalStatus.Active, reloaded.OperationalStatus);
        Assert.True(reloaded.IsCurrentlyEligibleForRides());
    }

    [Fact]
    public async Task Handle_ByOwner_ReturnsForbiddenAndDoesNotApprove()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = RegisterVehicle(ownerId);
        await _repository.AddAsync(vehicle, CancellationToken.None);

        var result = await _handler.Handle(new ApproveVehicleCommand(ownerId, vehicle.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        var reloaded = await _repository.GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(VehicleVerificationStatus.Pending, reloaded!.VerificationStatus);
    }

    [Fact]
    public async Task Handle_ForAlreadyApprovedVehicle_ReturnsConflict()
    {
        var vehicle = RegisterVehicle(Guid.NewGuid());
        await _repository.AddAsync(vehicle, CancellationToken.None);
        await _handler.Handle(new ApproveVehicleCommand(Guid.NewGuid(), vehicle.Id), CancellationToken.None);

        var result = await _handler.Handle(new ApproveVehicleCommand(Guid.NewGuid(), vehicle.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}

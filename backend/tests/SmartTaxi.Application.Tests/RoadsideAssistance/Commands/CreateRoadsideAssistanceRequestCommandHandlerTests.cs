using SmartTaxi.Application.Common;
using SmartTaxi.Application.RoadsideAssistance.Commands.CreateRoadsideAssistanceRequest;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Assignments.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Domain.Rides.ValueObjects;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.Tests.RoadsideAssistance.Commands;

public class CreateRoadsideAssistanceRequestCommandHandlerTests
{
    private readonly FakeRoadsideAssistanceRequestRepository _requestRepository = new();
    private readonly FakeVehicleRepository _vehicleRepository = new();
    private readonly FakeDriverVehicleAssignmentRepository _assignmentRepository = new();
    private readonly FakeRideRepository _rideRepository = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly CreateRoadsideAssistanceRequestCommandHandler _handler;

    public CreateRoadsideAssistanceRequestCommandHandlerTests()
    {
        _handler = new CreateRoadsideAssistanceRequestCommandHandler(
            _requestRepository, _vehicleRepository, _assignmentRepository, _rideRepository, _notificationDispatcher);
    }

    private async Task<Vehicle> CreateVehicleAsync(Guid ownerId)
    {
        var vehicle = Vehicle.Register(
            ownerId, null, "Toyota", "Corolla", 2022, "White", $"PLATE-{Guid.NewGuid():N}"[..12], null, 10000, FuelType.Petrol,
            TransmissionType.Manual, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);
        return vehicle;
    }

    private CreateRoadsideAssistanceRequestCommand BuildCommand(Guid requesterUserId, RoadsideRequesterRole role, Guid vehicleId, Guid? rideId = null) =>
        new(requesterUserId, role, vehicleId, rideId, RoadsideServiceType.Towing, RoadsideUrgency.High, "Panne moteur", 36.8, 10.18, "A1", "Tunis");

    [Fact]
    public async Task Handle_TaxiOwnerRequestingForOwnVehicle_Succeeds()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);

        var result = await _handler.Handle(BuildCommand(ownerId, RoadsideRequesterRole.TaxiOwner, vehicle.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_notificationDispatcher.DispatchedRequests);
    }

    [Fact]
    public async Task Handle_TaxiOwnerRequestingForAnotherOwnersVehicle_ReturnsForbidden()
    {
        var actualOwnerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(actualOwnerId);
        var impersonatorId = Guid.NewGuid();

        var result = await _handler.Handle(BuildCommand(impersonatorId, RoadsideRequesterRole.TaxiOwner, vehicle.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Handle_DriverWithActiveAssignment_Succeeds()
    {
        var ownerId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);

        var assignment = DriverVehicleAssignment.CreateDraft(
            driverId, vehicle.Id, ownerId, DateOnly.FromDateTime(DateTime.UtcNow), null, null, null, Domain.Fleet.Assignments.Enums.DaysOfWeek.All,
            ownerId, DateTime.UtcNow);
        await _assignmentRepository.AddAsync(assignment, CancellationToken.None);
        await _assignmentRepository.TryApproveAsync(assignment.Id, DateTime.UtcNow, CancellationToken.None);
        await _assignmentRepository.TryActivateAsync(assignment.Id, DateTime.UtcNow, CancellationToken.None);

        var result = await _handler.Handle(BuildCommand(driverId, RoadsideRequesterRole.Driver, vehicle.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_DriverWithoutActiveAssignment_ReturnsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);

        var result = await _handler.Handle(BuildCommand(driverId, RoadsideRequesterRole.Driver, vehicle.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Handle_UnknownVehicle_ReturnsNotFound()
    {
        var result = await _handler.Handle(
            BuildCommand(Guid.NewGuid(), RoadsideRequesterRole.TaxiOwner, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_VehicleAlreadyHasActiveRequest_ReturnsConflict()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var first = await _handler.Handle(BuildCommand(ownerId, RoadsideRequesterRole.TaxiOwner, vehicle.Id), CancellationToken.None);
        Assert.True(first.IsSuccess);

        var second = await _handler.Handle(BuildCommand(ownerId, RoadsideRequesterRole.TaxiOwner, vehicle.Id), CancellationToken.None);

        Assert.False(second.IsSuccess);
        Assert.Equal(ErrorType.Conflict, second.ErrorType);
    }

    [Fact]
    public async Task Handle_WithRideIdNotAssociatedWithRequester_ReturnsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var ride = Ride.Create(
            Guid.NewGuid(), RideType.Immediate, "Pickup", GeoCoordinate.Create(36.8, 10.18), "Dest", GeoCoordinate.Create(36.9, 10.2), null, 1, 0,
            false, false, false, false, null, RidePaymentMethod.Cash, null, "TND", DateTime.UtcNow);
        await _rideRepository.AddAsync(ride, CancellationToken.None);

        var result = await _handler.Handle(BuildCommand(ownerId, RoadsideRequesterRole.TaxiOwner, vehicle.Id, ride.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WithRideIdAssociatedAsCustomer_Succeeds()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var ride = Ride.Create(
            ownerId, RideType.Immediate, "Pickup", GeoCoordinate.Create(36.8, 10.18), "Dest", GeoCoordinate.Create(36.9, 10.2), null, 1, 0, false,
            false, false, false, null, RidePaymentMethod.Cash, null, "TND", DateTime.UtcNow);
        await _rideRepository.AddAsync(ride, CancellationToken.None);

        var result = await _handler.Handle(BuildCommand(ownerId, RoadsideRequesterRole.TaxiOwner, vehicle.Id, ride.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WithUnknownRideId_ReturnsNotFound()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);

        var result = await _handler.Handle(BuildCommand(ownerId, RoadsideRequesterRole.TaxiOwner, vehicle.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}

using SmartTaxi.Application.Common;
using SmartTaxi.Application.RoadsideAssistance.Commands.EscalateRoadsideRequestToMaintenance;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.Tests.RoadsideAssistance.Commands;

public class EscalateRoadsideRequestToMaintenanceCommandHandlerTests
{
    private readonly FakeRoadsideAssistanceRequestRepository _requestRepository = new();
    private readonly FakeMaintenanceRequestRepository _maintenanceRequestRepository = new();
    private readonly FakeRoadsideEscalationRepository _escalationRepository;
    private readonly FakeVehicleRepository _vehicleRepository = new();
    private readonly EscalateRoadsideRequestToMaintenanceCommandHandler _handler;

    public EscalateRoadsideRequestToMaintenanceCommandHandlerTests()
    {
        _escalationRepository = new FakeRoadsideEscalationRepository(_requestRepository, _maintenanceRequestRepository);
        _handler = new EscalateRoadsideRequestToMaintenanceCommandHandler(_requestRepository, _escalationRepository, _vehicleRepository);
    }

    private async Task<Vehicle> CreateVehicleAsync(Guid ownerId)
    {
        var vehicle = Vehicle.Register(
            ownerId, null, "Toyota", "Corolla", 2022, "White", $"PLATE-{Guid.NewGuid():N}"[..12], null, 10000, FuelType.Petrol,
            TransmissionType.Manual, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);
        return vehicle;
    }

    private async Task<RoadsideAssistanceRequest> CreateCompletedRequestAsync(Guid requesterUserId, Guid vehicleId)
    {
        var request = RoadsideAssistanceRequest.Create(
            requesterUserId, RoadsideRequesterRole.Driver, vehicleId, null, RoadsideServiceType.Towing, RoadsideUrgency.High, "Panne", 36.8, 10.18,
            null, null, DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [RoadsideRequestStatus.PartnersAvailable], RoadsideRequestStatus.Completed, null, null, 150m, null, null, false,
            DateTime.UtcNow, CancellationToken.None);
        return request;
    }

    [Fact]
    public async Task Handle_ByActualVehicleOwner_CreatesMaintenanceRequestAndSetsReference()
    {
        var ownerId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var request = await CreateCompletedRequestAsync(driverId, vehicle.Id);

        var result = await _handler.Handle(
            new EscalateRoadsideRequestToMaintenanceCommand(request.Id, ownerId, Guid.NewGuid(), null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloadedRequest = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(result.Value, reloadedRequest!.EscalatedMaintenanceRequestId);
        var maintenanceRequest = await _maintenanceRequestRepository.GetByIdAsync(result.Value, CancellationToken.None);
        Assert.NotNull(maintenanceRequest);
        Assert.Equal(ownerId, maintenanceRequest!.OwnerUserId);
    }

    [Fact]
    public async Task Handle_ByOriginalDriverRequesterNotOwner_ReturnsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var request = await CreateCompletedRequestAsync(driverId, vehicle.Id);

        var result = await _handler.Handle(
            new EscalateRoadsideRequestToMaintenanceCommand(request.Id, driverId, Guid.NewGuid(), null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Handle_RetryAfterSuccess_IsIdempotentAndCreatesOnlyOneMaintenanceRequest()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var request = await CreateCompletedRequestAsync(ownerId, vehicle.Id);

        var first = await _handler.Handle(
            new EscalateRoadsideRequestToMaintenanceCommand(request.Id, ownerId, Guid.NewGuid(), null), CancellationToken.None);
        var second = await _handler.Handle(
            new EscalateRoadsideRequestToMaintenanceCommand(request.Id, ownerId, Guid.NewGuid(), null), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value, second.Value);
    }

    [Fact]
    public async Task Handle_VehicleAlreadyHasActiveMaintenanceRequest_RollsBackAndReturnsConflict()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var existingMaintenanceRequest = MaintenanceRequest.Create(vehicle.Id, ownerId, Guid.NewGuid(), "Entretien prévu", DateTime.UtcNow);
        await _maintenanceRequestRepository.TryAddAsync(existingMaintenanceRequest, CancellationToken.None);
        var request = await CreateCompletedRequestAsync(ownerId, vehicle.Id);

        var result = await _handler.Handle(
            new EscalateRoadsideRequestToMaintenanceCommand(request.Id, ownerId, Guid.NewGuid(), null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        var reloadedRequest = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Null(reloadedRequest!.EscalatedMaintenanceRequestId);
    }

    [Fact]
    public async Task Handle_RequestNotCompleted_ReturnsConflict()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var request = RoadsideAssistanceRequest.Create(
            ownerId, RoadsideRequesterRole.TaxiOwner, vehicle.Id, null, RoadsideServiceType.Towing, RoadsideUrgency.High, "Panne", 36.8, 10.18,
            null, null, DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);

        var result = await _handler.Handle(
            new EscalateRoadsideRequestToMaintenanceCommand(request.Id, ownerId, Guid.NewGuid(), null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}

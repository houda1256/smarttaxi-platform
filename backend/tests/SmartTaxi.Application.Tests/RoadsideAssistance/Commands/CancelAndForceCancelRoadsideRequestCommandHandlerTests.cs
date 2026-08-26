using SmartTaxi.Application.Common;
using SmartTaxi.Application.RoadsideAssistance.Commands.CancelRoadsideAssistanceRequest;
using SmartTaxi.Application.RoadsideAssistance.Commands.ForceCancelRoadsideRequest;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.Tests.RoadsideAssistance.Commands;

public class CancelAndForceCancelRoadsideRequestCommandHandlerTests
{
    private readonly FakeRoadsideAssistanceRequestRepository _requestRepository = new();
    private readonly FakeVehicleRepository _vehicleRepository = new();
    private readonly FakeRoadsideWorkStartRepository _workStartRepository;
    private readonly FakeRoadsideForceCancelRepository _forceCancelRepository;
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly CancelRoadsideAssistanceRequestCommandHandler _cancelHandler;
    private readonly ForceCancelRoadsideRequestCommandHandler _forceCancelHandler;

    public CancelAndForceCancelRoadsideRequestCommandHandlerTests()
    {
        _workStartRepository = new FakeRoadsideWorkStartRepository(_requestRepository, _vehicleRepository);
        _forceCancelRepository = new FakeRoadsideForceCancelRepository(_requestRepository, _vehicleRepository);
        _cancelHandler = new CancelRoadsideAssistanceRequestCommandHandler(_requestRepository, _notificationDispatcher);
        _forceCancelHandler = new ForceCancelRoadsideRequestCommandHandler(_requestRepository, _forceCancelRepository, _notificationDispatcher);
    }

    private async Task<Vehicle> CreateActiveVehicleAsync(Guid ownerId)
    {
        var vehicle = Vehicle.Register(
            ownerId, null, "Toyota", "Corolla", 2022, "White", $"PLATE-{Guid.NewGuid():N}"[..12], null, 10000, FuelType.Petrol,
            TransmissionType.Manual, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);
        await _vehicleRepository.TryApproveAsync(vehicle.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
        return vehicle;
    }

    private async Task<RoadsideAssistanceRequest> CreateRequestAsync(Guid requesterUserId, Guid vehicleId) =>
        await CreateRequestInternalAsync(requesterUserId, vehicleId, RoadsideServiceType.Towing);

    private async Task<RoadsideAssistanceRequest> CreateRequestInternalAsync(Guid requesterUserId, Guid vehicleId, RoadsideServiceType serviceType)
    {
        var request = RoadsideAssistanceRequest.Create(
            requesterUserId, RoadsideRequesterRole.TaxiOwner, vehicleId, null, serviceType, RoadsideUrgency.High, "Panne", 36.8, 10.18, null, null,
            DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        return request;
    }

    [Fact]
    public async Task Cancel_ByRequesterFromPartnersAvailable_Succeeds()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        var request = await CreateRequestAsync(ownerId, vehicle.Id);

        var result = await _cancelHandler.Handle(new CancelRoadsideAssistanceRequestCommand(request.Id, ownerId, "Résolu autrement"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(RoadsideRequestStatus.Cancelled, reloaded!.Status);
    }

    [Fact]
    public async Task Cancel_ByAnotherUser_ReturnsConflict()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        var request = await CreateRequestAsync(ownerId, vehicle.Id);

        var result = await _cancelHandler.Handle(new CancelRoadsideAssistanceRequestCommand(request.Id, Guid.NewGuid(), null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Cancel_AfterInProgress_ReturnsConflict()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        var partnerUserId = Guid.NewGuid();
        var request = await CreateRequestAsync(ownerId, vehicle.Id);
        await _requestRepository.TrySelectPartnerAsync(request.Id, ownerId, partnerUserId, DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryRespondAsync(
            request.Id, partnerUserId, RoadsidePartnerResponse.Accepted, null, null, DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [RoadsideRequestStatus.Accepted], RoadsideRequestStatus.PartnerOnTheWay, null, partnerUserId, null, null, null, false,
            DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [RoadsideRequestStatus.PartnerOnTheWay], RoadsideRequestStatus.PartnerArrived, null, partnerUserId, null, null, null, false,
            DateTime.UtcNow, CancellationToken.None);
        await _workStartRepository.TryStartAsync(request.Id, partnerUserId, vehicle.Id, DateTime.UtcNow, CancellationToken.None);

        var result = await _cancelHandler.Handle(new CancelRoadsideAssistanceRequestCommand(request.Id, ownerId, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task ForceCancel_FromInProgressImmobilizing_ReleasesVehicle()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        var partnerUserId = Guid.NewGuid();
        var request = await CreateRequestAsync(ownerId, vehicle.Id);
        await _requestRepository.TrySelectPartnerAsync(request.Id, ownerId, partnerUserId, DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryRespondAsync(
            request.Id, partnerUserId, RoadsidePartnerResponse.Accepted, null, null, DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [RoadsideRequestStatus.Accepted], RoadsideRequestStatus.PartnerOnTheWay, null, partnerUserId, null, null, null, false,
            DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [RoadsideRequestStatus.PartnerOnTheWay], RoadsideRequestStatus.PartnerArrived, null, partnerUserId, null, null, null, false,
            DateTime.UtcNow, CancellationToken.None);
        await _workStartRepository.TryStartAsync(request.Id, partnerUserId, vehicle.Id, DateTime.UtcNow, CancellationToken.None);

        var result = await _forceCancelHandler.Handle(new ForceCancelRoadsideRequestCommand(request.Id, Guid.NewGuid(), "Litige"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloadedRequest = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        var reloadedVehicle = await _vehicleRepository.GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(RoadsideRequestStatus.Cancelled, reloadedRequest!.Status);
        Assert.Equal(VehicleOperationalStatus.Active, reloadedVehicle!.OperationalStatus);
    }

    [Fact]
    public async Task ForceCancel_WithoutReason_ReturnsValidationError()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        var request = await CreateRequestAsync(ownerId, vehicle.Id);

        var result = await _forceCancelHandler.Handle(new ForceCancelRoadsideRequestCommand(request.Id, Guid.NewGuid(), ""), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task ForceCancel_AlreadyTerminal_ReturnsConflict()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        var request = await CreateRequestAsync(ownerId, vehicle.Id);
        await _cancelHandler.Handle(new CancelRoadsideAssistanceRequestCommand(request.Id, ownerId, null), CancellationToken.None);

        var result = await _forceCancelHandler.Handle(new ForceCancelRoadsideRequestCommand(request.Id, Guid.NewGuid(), "Litige"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}

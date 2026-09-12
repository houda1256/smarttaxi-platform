using SmartTaxi.Application.Common;
using SmartTaxi.Application.RoadsideAssistance.Commands.CompleteRoadsideIntervention;
using SmartTaxi.Application.RoadsideAssistance.Commands.StartRoadsideIntervention;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.Tests.RoadsideAssistance.Commands;

/// <summary>Application-layer proof of the mandatory Fleet atomicity design using the fakes' composed in-memory transaction simulation — the real cross-table guarantee is additionally proven against Postgres in Infrastructure.IntegrationTests.</summary>
public class StartAndCompleteRoadsideInterventionCommandHandlerTests
{
    private readonly FakeRoadsideAssistanceRequestRepository _requestRepository = new();
    private readonly FakeVehicleRepository _vehicleRepository = new();
    private readonly FakeRoadsideWorkStartRepository _workStartRepository;
    private readonly FakeRoadsideCompletionRepository _completionRepository;
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly StartRoadsideInterventionCommandHandler _startHandler;
    private readonly CompleteRoadsideInterventionCommandHandler _completeHandler;

    public StartAndCompleteRoadsideInterventionCommandHandlerTests()
    {
        _workStartRepository = new FakeRoadsideWorkStartRepository(_requestRepository, _vehicleRepository);
        _completionRepository = new FakeRoadsideCompletionRepository(_requestRepository, _vehicleRepository);
        _startHandler = new StartRoadsideInterventionCommandHandler(_requestRepository, _workStartRepository, _notificationDispatcher);
        _completeHandler = new CompleteRoadsideInterventionCommandHandler(_requestRepository, _completionRepository, _notificationDispatcher);
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

    private async Task<(RoadsideAssistanceRequest Request, Guid PartnerUserId)> CreateRequestAtPartnerArrivedAsync(
        Guid requesterUserId, Guid vehicleId, RoadsideServiceType serviceType)
    {
        var partnerUserId = Guid.NewGuid();
        var request = RoadsideAssistanceRequest.Create(
            requesterUserId, RoadsideRequesterRole.TaxiOwner, vehicleId, null, serviceType, RoadsideUrgency.High, "Panne", 36.8, 10.18, null, null,
            DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        await _requestRepository.TrySelectPartnerAsync(request.Id, requesterUserId, partnerUserId, DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryRespondAsync(
            request.Id, partnerUserId, RoadsidePartnerResponse.Accepted, null, null, DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [RoadsideRequestStatus.Accepted], RoadsideRequestStatus.PartnerOnTheWay, null, partnerUserId, null, null, null, false,
            DateTime.UtcNow, CancellationToken.None);
        await _requestRepository.TryTransitionAsync(
            request.Id, [RoadsideRequestStatus.PartnerOnTheWay], RoadsideRequestStatus.PartnerArrived, null, partnerUserId, null, null, null, false,
            DateTime.UtcNow, CancellationToken.None);
        return (request, partnerUserId);
    }

    [Fact]
    public async Task Start_ImmobilizingServiceType_TransitionsRequestAndVehicleTogether()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        var (request, partnerUserId) = await CreateRequestAtPartnerArrivedAsync(ownerId, vehicle.Id, RoadsideServiceType.Towing);

        var result = await _startHandler.Handle(new StartRoadsideInterventionCommand(request.Id, partnerUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloadedRequest = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        var reloadedVehicle = await _vehicleRepository.GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(RoadsideRequestStatus.InProgress, reloadedRequest!.Status);
        Assert.Equal(VehicleOperationalStatus.UnderRoadsideAssistance, reloadedVehicle!.OperationalStatus);
    }

    [Fact]
    public async Task Start_ImmobilizingServiceTypeButVehicleAlreadySuspended_RollsBackWholeTransaction()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        var (request, partnerUserId) = await CreateRequestAtPartnerArrivedAsync(ownerId, vehicle.Id, RoadsideServiceType.Towing);
        await _vehicleRepository.TrySuspendAsync(vehicle.Id, DateTime.UtcNow, CancellationToken.None);

        var result = await _startHandler.Handle(new StartRoadsideInterventionCommand(request.Id, partnerUserId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        var reloadedRequest = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        var reloadedVehicle = await _vehicleRepository.GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(RoadsideRequestStatus.PartnerArrived, reloadedRequest!.Status);
        Assert.Equal(VehicleOperationalStatus.Suspended, reloadedVehicle!.OperationalStatus);
    }

    [Fact]
    public async Task Start_NonImmobilizingServiceType_NeverTouchesFleet()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        var (request, partnerUserId) = await CreateRequestAtPartnerArrivedAsync(ownerId, vehicle.Id, RoadsideServiceType.BatteryJumpStart);

        var result = await _startHandler.Handle(new StartRoadsideInterventionCommand(request.Id, partnerUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloadedRequest = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        var reloadedVehicle = await _vehicleRepository.GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(RoadsideRequestStatus.InProgress, reloadedRequest!.Status);
        Assert.Equal(VehicleOperationalStatus.Active, reloadedVehicle!.OperationalStatus);
    }

    [Fact]
    public async Task Complete_ImmobilizingServiceType_ReleasesVehicleBackToActive()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        var (request, partnerUserId) = await CreateRequestAtPartnerArrivedAsync(ownerId, vehicle.Id, RoadsideServiceType.Towing);
        await _startHandler.Handle(new StartRoadsideInterventionCommand(request.Id, partnerUserId), CancellationToken.None);

        var result = await _completeHandler.Handle(new CompleteRoadsideInterventionCommand(request.Id, partnerUserId, 80m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloadedRequest = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        var reloadedVehicle = await _vehicleRepository.GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(RoadsideRequestStatus.Completed, reloadedRequest!.Status);
        Assert.Equal(80m, reloadedRequest.FinalCost);
        Assert.Equal(VehicleOperationalStatus.Active, reloadedVehicle!.OperationalStatus);
    }

    [Fact]
    public async Task Complete_VehicleIndependentlySuspended_StillCompletesWithoutReactivating()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        var (request, partnerUserId) = await CreateRequestAtPartnerArrivedAsync(ownerId, vehicle.Id, RoadsideServiceType.Towing);
        await _startHandler.Handle(new StartRoadsideInterventionCommand(request.Id, partnerUserId), CancellationToken.None);

        // Independently changed mid-intervention (e.g. by an admin) — completion must not force it back to Active.
        await _vehicleRepository.TryReleaseFromRoadsideAssistanceAsync(vehicle.Id, DateTime.UtcNow, CancellationToken.None);
        await _vehicleRepository.TrySuspendAsync(vehicle.Id, DateTime.UtcNow, CancellationToken.None);

        var result = await _completeHandler.Handle(new CompleteRoadsideInterventionCommand(request.Id, partnerUserId, 80m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloadedRequest = await _requestRepository.GetByIdAsync(request.Id, CancellationToken.None);
        var reloadedVehicle = await _vehicleRepository.GetByIdAsync(vehicle.Id, CancellationToken.None);
        Assert.Equal(RoadsideRequestStatus.Completed, reloadedRequest!.Status);
        Assert.Equal(VehicleOperationalStatus.Suspended, reloadedVehicle!.OperationalStatus);
    }

    [Fact]
    public async Task Complete_InvalidFinalCost_ReturnsValidationError()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateActiveVehicleAsync(ownerId);
        var (request, partnerUserId) = await CreateRequestAtPartnerArrivedAsync(ownerId, vehicle.Id, RoadsideServiceType.Towing);
        await _startHandler.Handle(new StartRoadsideInterventionCommand(request.Id, partnerUserId), CancellationToken.None);

        var result = await _completeHandler.Handle(new CompleteRoadsideInterventionCommand(request.Id, partnerUserId, 0m), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }
}

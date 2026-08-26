using SmartTaxi.Application.Common;
using SmartTaxi.Application.RoadsideAssistance.Queries.GetRecommendedRoadsidePartners;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.Tests.RoadsideAssistance.Queries;

public class GetRecommendedRoadsidePartnersQueryHandlerTests
{
    private readonly FakeRoadsideAssistanceRequestRepository _requestRepository = new();
    private readonly FakeRoadsidePartnerProfileRepository _partnerProfileRepository = new();
    private readonly FakeVehicleRepository _vehicleRepository = new();
    private readonly FakeDistanceCalculator _distanceCalculator = new();
    private readonly GetRecommendedRoadsidePartnersQueryHandler _handler;

    public GetRecommendedRoadsidePartnersQueryHandlerTests()
    {
        _handler = new GetRecommendedRoadsidePartnersQueryHandler(_requestRepository, _partnerProfileRepository, _vehicleRepository, _distanceCalculator);
    }

    private async Task<Vehicle> CreateVehicleAsync(Guid ownerId)
    {
        var vehicle = Vehicle.Register(
            ownerId, null, "Toyota", "Corolla", 2022, "White", $"PLATE-{Guid.NewGuid():N}"[..12], null, 10000, FuelType.Petrol,
            TransmissionType.Manual, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);
        return vehicle;
    }

    private async Task<RoadsideAssistanceRequest> CreateRequestAsync(Guid requesterUserId, Guid vehicleId)
    {
        var request = RoadsideAssistanceRequest.Create(
            requesterUserId, RoadsideRequesterRole.TaxiOwner, vehicleId, null, RoadsideServiceType.Towing, RoadsideUrgency.High, "Panne", 36.8,
            10.18, null, "Tunis", DateTime.UtcNow);
        await _requestRepository.TryAddAsync(request, CancellationToken.None);
        return request;
    }

    [Fact]
    public async Task Handle_CandidateWithCoordinates_ReturnsComputedDistance()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var request = await CreateRequestAsync(ownerId, vehicle.Id);
        var partner = RoadsidePartnerProfile.Register(
            Guid.NewGuid(), "Assistance Rapide", null, "12 rue X", "Tunis", "Towing", "Standard", 36.81, 10.19, DateTime.UtcNow);
        await _partnerProfileRepository.TryAddAsync(partner, CancellationToken.None);

        var result = await _handler.Handle(new GetRecommendedRoadsidePartnersQuery(request.Id, ownerId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var recommendation = Assert.Single(result.Value!);
        Assert.NotNull(recommendation.DistanceKm);
    }

    [Fact]
    public async Task Handle_CandidateWithoutCoordinates_ReturnsNullDistanceNeverDefaulted()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var request = await CreateRequestAsync(ownerId, vehicle.Id);
        var partner = RoadsidePartnerProfile.Register(
            Guid.NewGuid(), "Assistance Rapide", null, "12 rue X", "Tunis", "Towing", "Standard", null, null, DateTime.UtcNow);
        await _partnerProfileRepository.TryAddAsync(partner, CancellationToken.None);

        var result = await _handler.Handle(new GetRecommendedRoadsidePartnersQuery(request.Id, ownerId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var recommendation = Assert.Single(result.Value!);
        Assert.Null(recommendation.DistanceKm);
    }

    [Fact]
    public async Task Handle_IncompatibleServiceType_ExcludesCandidate()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var request = await CreateRequestAsync(ownerId, vehicle.Id);
        var partner = RoadsidePartnerProfile.Register(
            Guid.NewGuid(), "Assistance Rapide", null, "12 rue X", "Tunis", "BatteryJumpStart", "Standard", null, null, DateTime.UtcNow);
        await _partnerProfileRepository.TryAddAsync(partner, CancellationToken.None);

        var result = await _handler.Handle(new GetRecommendedRoadsidePartnersQuery(request.Id, ownerId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task Handle_ByUnrelatedUser_ReturnsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = await CreateVehicleAsync(ownerId);
        var request = await CreateRequestAsync(ownerId, vehicle.Id);

        var result = await _handler.Handle(new GetRecommendedRoadsidePartnersQuery(request.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }
}

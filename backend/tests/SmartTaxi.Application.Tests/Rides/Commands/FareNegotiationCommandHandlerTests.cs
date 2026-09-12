using SmartTaxi.Application.Common;
using SmartTaxi.Application.Rides.Commands.AcceptFare;
using SmartTaxi.Application.Rides.Commands.CounterProposeFare;
using SmartTaxi.Application.Rides.Commands.CreateRide;
using SmartTaxi.Application.Rides.Commands.DriverAcceptRide;
using SmartTaxi.Application.Rides.Commands.ProposeFare;
using SmartTaxi.Application.Rides.Commands.RejectFare;
using SmartTaxi.Application.Rides.Commands.SelectDriver;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.Rides.Commands;

public class FareNegotiationCommandHandlerTests
{
    private readonly FakeRideRepository _rideRepository = new();
    private readonly FakeRideDriverRecommendationRepository _recommendationRepository = new();
    private readonly FakeDriverReservationHoldRepository _holdRepository = new();
    private readonly FakeDriverProfileRepository _driverRepository = new();
    private readonly FakeRideFareProposalRepository _proposalRepository = new();
    private readonly FakeDriverSearchPolicy _searchPolicy = new();
    private readonly FakeNegotiationPolicy _negotiationPolicy = new();

    private readonly CreateRideCommandHandler _createHandler;
    private readonly SelectDriverCommandHandler _selectHandler;
    private readonly DriverAcceptRideCommandHandler _acceptRideHandler;
    private readonly ProposeFareCommandHandler _proposeHandler;
    private readonly CounterProposeFareCommandHandler _counterProposeHandler;
    private readonly AcceptFareCommandHandler _acceptFareHandler;
    private readonly RejectFareCommandHandler _rejectFareHandler;

    public FareNegotiationCommandHandlerTests()
    {
        _createHandler = new CreateRideCommandHandler(_rideRepository);
        _selectHandler = new SelectDriverCommandHandler(_rideRepository, _recommendationRepository, _holdRepository, _searchPolicy);
        _acceptRideHandler = new DriverAcceptRideCommandHandler(_rideRepository, _holdRepository, _driverRepository);
        _proposeHandler = new ProposeFareCommandHandler(_rideRepository, _proposalRepository, _negotiationPolicy);
        _counterProposeHandler = new CounterProposeFareCommandHandler(_rideRepository, _proposalRepository, _driverRepository, _negotiationPolicy);
        _acceptFareHandler = new AcceptFareCommandHandler(_rideRepository, _proposalRepository, _driverRepository);
        _rejectFareHandler = new RejectFareCommandHandler(_rideRepository, _proposalRepository, _driverRepository);
    }

    private async Task<(Guid CustomerId, Guid RideId, Guid DriverUserId)> CreateNegotiatedRideWithAcceptedDriverAsync()
    {
        var customerId = Guid.NewGuid();
        var createResult = await _createHandler.Handle(
            new CreateRideCommand(
                customerId, RideType.Negotiated, "A", 36.8, 10.1, "B", 36.9, 10.2, null, 1, 0, false, false, false,
                false, null, RidePaymentMethod.Cash, null),
            CancellationToken.None);
        var rideId = createResult.Value;

        await _rideRepository.TryTransitionAsync(rideId, RideStatus.Draft, RideStatus.Searching, null, null, DateTime.UtcNow, CancellationToken.None);
        await _rideRepository.TryTransitionAsync(rideId, RideStatus.Searching, RideStatus.DriversAvailable, null, null, DateTime.UtcNow, CancellationToken.None);

        var driverUserId = Guid.NewGuid();
        var driver = DriverProfile.Create(driverUserId, "LIC1", DateTime.UtcNow.AddYears(1), null, true, DateTime.UtcNow);
        await _driverRepository.AddAsync(driver, CancellationToken.None);
        await _driverRepository.TrySubmitForReviewAsync(driver.Id, DateTime.UtcNow, CancellationToken.None);
        await _driverRepository.TryApproveAsync(driver.Id, DateTime.UtcNow, CancellationToken.None);

        var vehicle = Vehicle.Register(
            Guid.NewGuid(), null, "Toyota", "Corolla", 2022, "White", "AA-123-BB", null, 0, FuelType.Petrol,
            TransmissionType.Automatic, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);

        await _recommendationRepository.ReplaceForRideAsync(
            rideId,
            [new RideDriverRecommendation(rideId, driver.Id, vehicle.Id, 2.0m, 5, 40m, "Proche", 1, DateTime.UtcNow)],
            CancellationToken.None);

        await _selectHandler.Handle(new SelectDriverCommand(customerId, rideId, driver.Id, vehicle.Id), CancellationToken.None);
        await _acceptRideHandler.Handle(new DriverAcceptRideCommand(driverUserId, rideId), CancellationToken.None);

        return (customerId, rideId, driverUserId);
    }

    [Fact]
    public async Task Propose_ByCustomer_OpensRoundOne()
    {
        var (customerId, rideId, _) = await CreateNegotiatedRideWithAcceptedDriverAsync();

        var result = await _proposeHandler.Handle(new ProposeFareCommand(customerId, rideId, 25m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var latest = await _proposalRepository.GetLatestForRideAsync(rideId, CancellationToken.None);
        Assert.Equal(1, latest!.RoundNumber);
        Assert.Equal(FareProposalStatus.Proposed, latest.Status);
    }

    [Fact]
    public async Task Propose_OnNonNegotiatedRide_ReturnsValidationError()
    {
        var customerId = Guid.NewGuid();
        var createResult = await _createHandler.Handle(
            new CreateRideCommand(
                customerId, RideType.Immediate, "A", 36.8, 10.1, "B", 36.9, 10.2, null, 1, 0, false, false, false,
                false, null, RidePaymentMethod.Cash, null),
            CancellationToken.None);

        var result = await _proposeHandler.Handle(new ProposeFareCommand(customerId, createResult.Value, 25m), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task CounterPropose_ByDriver_CreatesRoundTwo()
    {
        var (customerId, rideId, driverUserId) = await CreateNegotiatedRideWithAcceptedDriverAsync();
        await _proposeHandler.Handle(new ProposeFareCommand(customerId, rideId, 25m), CancellationToken.None);

        var result = await _counterProposeHandler.Handle(new CounterProposeFareCommand(driverUserId, rideId, 30m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var latest = await _proposalRepository.GetLatestForRideAsync(rideId, CancellationToken.None);
        Assert.Equal(2, latest!.RoundNumber);
        Assert.Equal(FareProposalStatus.CounterProposed, latest.Status);
    }

    [Fact]
    public async Task CounterPropose_BySameProposer_ReturnsValidationError()
    {
        var (customerId, rideId, _) = await CreateNegotiatedRideWithAcceptedDriverAsync();
        await _proposeHandler.Handle(new ProposeFareCommand(customerId, rideId, 25m), CancellationToken.None);

        var result = await _counterProposeHandler.Handle(new CounterProposeFareCommand(customerId, rideId, 30m), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task CounterPropose_BeyondMaxRounds_ReturnsConflict()
    {
        var (customerId, rideId, driverUserId) = await CreateNegotiatedRideWithAcceptedDriverAsync();
        await _proposeHandler.Handle(new ProposeFareCommand(customerId, rideId, 25m), CancellationToken.None);

        var currentProposer = driverUserId;
        var otherProposer = customerId;

        for (var round = 2; round <= _negotiationPolicy.MaxNegotiationRounds; round++)
        {
            await _counterProposeHandler.Handle(new CounterProposeFareCommand(currentProposer, rideId, 25m + round), CancellationToken.None);
            (currentProposer, otherProposer) = (otherProposer, currentProposer);
        }

        var result = await _counterProposeHandler.Handle(new CounterProposeFareCommand(currentProposer, rideId, 99m), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task AcceptFare_ByDriver_SetsImmutableNegotiatedFare()
    {
        var (customerId, rideId, driverUserId) = await CreateNegotiatedRideWithAcceptedDriverAsync();
        await _proposeHandler.Handle(new ProposeFareCommand(customerId, rideId, 25m), CancellationToken.None);

        var result = await _acceptFareHandler.Handle(new AcceptFareCommand(driverUserId, rideId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(25m, result.Value);
        var ride = await _rideRepository.GetByIdAsync(rideId, CancellationToken.None);
        Assert.Equal(25m, ride!.NegotiatedFinalFare);
    }

    [Fact]
    public async Task AcceptFare_OwnProposal_ReturnsValidationError()
    {
        var (customerId, rideId, _) = await CreateNegotiatedRideWithAcceptedDriverAsync();
        await _proposeHandler.Handle(new ProposeFareCommand(customerId, rideId, 25m), CancellationToken.None);

        var result = await _acceptFareHandler.Handle(new AcceptFareCommand(customerId, rideId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task RejectFare_ByDriver_MarksProposalRejected()
    {
        var (customerId, rideId, driverUserId) = await CreateNegotiatedRideWithAcceptedDriverAsync();
        await _proposeHandler.Handle(new ProposeFareCommand(customerId, rideId, 25m), CancellationToken.None);

        var result = await _rejectFareHandler.Handle(new RejectFareCommand(driverUserId, rideId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var latest = await _proposalRepository.GetLatestForRideAsync(rideId, CancellationToken.None);
        Assert.Equal(FareProposalStatus.Rejected, latest!.Status);
    }

    [Fact]
    public async Task CounterPropose_AfterProposalAlreadyAccepted_FailsAtomically()
    {
        var (customerId, rideId, driverUserId) = await CreateNegotiatedRideWithAcceptedDriverAsync();
        var proposeResult = await _proposeHandler.Handle(new ProposeFareCommand(customerId, rideId, 25m), CancellationToken.None);
        await _acceptFareHandler.Handle(new AcceptFareCommand(driverUserId, rideId), CancellationToken.None);

        // Simulate a counter-offer that read the proposal before it was accepted.
        var staleCounter = RideFareProposal.CreateCounterProposal(rideId, customerId, 20m, "TND", 2, DateTime.UtcNow, TimeSpan.FromMinutes(2));
        var added = await _proposalRepository.TryAddCounterProposalAsync(staleCounter, proposeResult.Value, CancellationToken.None);

        Assert.False(added);
    }
}

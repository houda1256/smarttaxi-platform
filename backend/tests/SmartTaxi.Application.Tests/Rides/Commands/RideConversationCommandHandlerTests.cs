using SmartTaxi.Application.Common;
using SmartTaxi.Application.Rides.Commands.CompleteRide;
using SmartTaxi.Application.Rides.Commands.CreateRide;
using SmartTaxi.Application.Rides.Commands.DriverAcceptRide;
using SmartTaxi.Application.Rides.Commands.DriverArrived;
using SmartTaxi.Application.Rides.Commands.DriverEnRoute;
using SmartTaxi.Application.Rides.Commands.PassengerOnBoard;
using SmartTaxi.Application.Rides.Commands.ReportRideMessage;
using SmartTaxi.Application.Rides.Commands.SelectDriver;
using SmartTaxi.Application.Rides.Commands.SendRideMessage;
using SmartTaxi.Application.Rides.Commands.StartRide;
using SmartTaxi.Application.Rides.Queries.GetRideMessages;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.Rides.Commands;

public class RideConversationCommandHandlerTests
{
    private readonly FakeRideRepository _rideRepository = new();
    private readonly FakeRideDriverRecommendationRepository _recommendationRepository = new();
    private readonly FakeDriverReservationHoldRepository _holdRepository = new();
    private readonly FakeDriverProfileRepository _driverRepository = new();
    private readonly FakeVehicleRepository _vehicleRepository = new();
    private readonly FakeDriverSearchPolicy _searchPolicy = new();
    private readonly FakeFareCalculator _fareCalculator = new();
    private readonly FakeDynamicPricingProvider _dynamicPricingProvider = new();
    private readonly FakeRideConversationRepository _conversationRepository = new();
    private readonly FakeRideMessageRepository _messageRepository = new();
    private readonly FakeRideServicePolicy _servicePolicy = new();

    private readonly CreateRideCommandHandler _createHandler;
    private readonly SelectDriverCommandHandler _selectHandler;
    private readonly DriverAcceptRideCommandHandler _acceptHandler;
    private readonly DriverEnRouteCommandHandler _enRouteHandler;
    private readonly DriverArrivedCommandHandler _arrivedHandler;
    private readonly PassengerOnBoardCommandHandler _passengerOnBoardHandler;
    private readonly StartRideCommandHandler _startHandler;
    private readonly CompleteRideCommandHandler _completeHandler;
    private readonly SendRideMessageCommandHandler _sendMessageHandler;
    private readonly GetRideMessagesQueryHandler _getMessagesHandler;
    private readonly ReportRideMessageCommandHandler _reportMessageHandler;

    public RideConversationCommandHandlerTests()
    {
        _createHandler = new CreateRideCommandHandler(_rideRepository);
        _selectHandler = new SelectDriverCommandHandler(_rideRepository, _recommendationRepository, _holdRepository, _searchPolicy);
        _acceptHandler = new DriverAcceptRideCommandHandler(_rideRepository, _holdRepository, _driverRepository);
        _enRouteHandler = new DriverEnRouteCommandHandler(_rideRepository, _driverRepository);
        _arrivedHandler = new DriverArrivedCommandHandler(_rideRepository, _driverRepository);
        _passengerOnBoardHandler = new PassengerOnBoardCommandHandler(_rideRepository, _driverRepository);
        _startHandler = new StartRideCommandHandler(_rideRepository, _driverRepository);
        _completeHandler = new CompleteRideCommandHandler(_rideRepository, _driverRepository, _vehicleRepository, _fareCalculator, _dynamicPricingProvider, new FakeNotificationDispatcher());
        _sendMessageHandler = new SendRideMessageCommandHandler(_rideRepository, _driverRepository, _conversationRepository, _messageRepository, _servicePolicy);
        _getMessagesHandler = new GetRideMessagesQueryHandler(_rideRepository, _driverRepository, _conversationRepository, _messageRepository);
        _reportMessageHandler = new ReportRideMessageCommandHandler(_rideRepository, _driverRepository, _messageRepository);
    }

    private async Task<(Guid CustomerId, Guid RideId, Guid DriverUserId)> CreateAcceptedRideAsync()
    {
        var customerId = Guid.NewGuid();
        var createResult = await _createHandler.Handle(
            new CreateRideCommand(
                customerId, RideType.Immediate, "A", 36.8, 10.1, "B", 36.9, 10.2, null, 1, 0, false, false, false,
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
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);

        await _recommendationRepository.ReplaceForRideAsync(
            rideId,
            [new RideDriverRecommendation(rideId, driver.Id, vehicle.Id, 2.0m, 5, 40m, "Proche", 1, DateTime.UtcNow)],
            CancellationToken.None);

        await _selectHandler.Handle(new SelectDriverCommand(customerId, rideId, driver.Id, vehicle.Id), CancellationToken.None);
        await _acceptHandler.Handle(new DriverAcceptRideCommand(driverUserId, rideId), CancellationToken.None);

        return (customerId, rideId, driverUserId);
    }

    [Fact]
    public async Task SendMessage_ByParticipant_CreatesConversationAndMessage()
    {
        var (customerId, rideId, _) = await CreateAcceptedRideAsync();

        var result = await _sendMessageHandler.Handle(
            new SendRideMessageCommand(customerId, rideId, RideMessageType.Text, "J'arrive dans 2 minutes"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var messages = await _getMessagesHandler.Handle(new GetRideMessagesQuery(customerId, rideId), CancellationToken.None);
        Assert.Single(messages.Value);
    }

    [Fact]
    public async Task SendMessage_ByNonParticipant_ReturnsForbidden()
    {
        var (_, rideId, _) = await CreateAcceptedRideAsync();

        var result = await _sendMessageHandler.Handle(
            new SendRideMessageCommand(Guid.NewGuid(), rideId, RideMessageType.Text, "Test"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task ReportMessage_MarksMessageAsReported()
    {
        var (customerId, rideId, driverUserId) = await CreateAcceptedRideAsync();
        var sendResult = await _sendMessageHandler.Handle(
            new SendRideMessageCommand(customerId, rideId, RideMessageType.Text, "Bonjour"), CancellationToken.None);

        var result = await _reportMessageHandler.Handle(new ReportRideMessageCommand(driverUserId, rideId, sendResult.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var messages = await _getMessagesHandler.Handle(new GetRideMessagesQuery(customerId, rideId), CancellationToken.None);
        Assert.True(messages.Value.Single().IsReported);
    }

    [Fact]
    public async Task SendMessage_AfterReadOnlyWindowElapsedPostCompletion_ReturnsConflict()
    {
        var customerId = Guid.NewGuid();
        var createResult = await _createHandler.Handle(
            new CreateRideCommand(
                customerId, RideType.Immediate, "A", 36.8, 10.1, "B", 36.9, 10.2, null, 1, 0, false, false, false,
                false, null, RidePaymentMethod.Cash, null),
            CancellationToken.None);
        var rideId = createResult.Value;

        await _rideRepository.TryTransitionAsync(rideId, RideStatus.Draft, RideStatus.Searching, null, null, DateTime.UtcNow, CancellationToken.None);
        var cancelledAt = DateTime.UtcNow.AddMinutes(-120);
        await _rideRepository.TryTransitionAsync(rideId, RideStatus.Searching, RideStatus.CancelledByCustomer, customerId, "Changement d'avis", cancelledAt, CancellationToken.None);

        var result = await _sendMessageHandler.Handle(
            new SendRideMessageCommand(customerId, rideId, RideMessageType.Text, "Trop tard"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}

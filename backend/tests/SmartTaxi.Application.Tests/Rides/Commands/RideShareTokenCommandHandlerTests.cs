using SmartTaxi.Application.Common;
using SmartTaxi.Application.Rides.Commands.CreateRide;
using SmartTaxi.Application.Rides.Commands.CreateRideShareToken;
using SmartTaxi.Application.Rides.Commands.RevokeRideShareToken;
using SmartTaxi.Application.Rides.Queries.GetPublicRideShareView;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.Rides.Commands;

public class RideShareTokenCommandHandlerTests
{
    private readonly FakeRideRepository _rideRepository = new();
    private readonly FakeRideShareTokenRepository _tokenRepository = new();
    private readonly FakeRideShareTokenGenerator _tokenGenerator = new();
    private readonly FakeRideShareTokenHasher _tokenHasher = new();
    private readonly FakeRideServicePolicy _servicePolicy = new();
    private readonly FakeDriverProfileRepository _driverRepository = new();
    private readonly FakeVehicleRepository _vehicleRepository = new();
    private readonly FakeUserPreferencesRepository _preferencesRepository = new();
    private readonly FakeRouteEstimationService _routeEstimationService = new();

    private readonly CreateRideCommandHandler _createHandler;
    private readonly CreateRideShareTokenCommandHandler _createTokenHandler;
    private readonly RevokeRideShareTokenCommandHandler _revokeTokenHandler;
    private readonly GetPublicRideShareViewQueryHandler _publicViewHandler;

    public RideShareTokenCommandHandlerTests()
    {
        _createHandler = new CreateRideCommandHandler(_rideRepository);
        _createTokenHandler = new CreateRideShareTokenCommandHandler(_rideRepository, _tokenRepository, _tokenGenerator, _tokenHasher, _servicePolicy);
        _revokeTokenHandler = new RevokeRideShareTokenCommandHandler(_rideRepository, _tokenRepository);
        _publicViewHandler = new GetPublicRideShareViewQueryHandler(
            _tokenRepository, _tokenHasher, _rideRepository, _driverRepository, _vehicleRepository, _preferencesRepository, _routeEstimationService);
    }

    private async Task<(Guid CustomerId, Guid RideId)> CreateRideAsync()
    {
        var customerId = Guid.NewGuid();
        var createResult = await _createHandler.Handle(
            new CreateRideCommand(
                customerId, RideType.Immediate, "A", 36.8, 10.1, "B", 36.9, 10.2, null, 1, 0, false, false, false,
                false, null, RidePaymentMethod.Cash, null),
            CancellationToken.None);

        return (customerId, createResult.Value);
    }

    [Fact]
    public async Task CreateToken_ByOwningCustomer_ReturnsRawTokenAndPersistsOnlyHash()
    {
        var (customerId, rideId) = await CreateRideAsync();

        var result = await _createTokenHandler.Handle(new CreateRideShareTokenCommand(customerId, rideId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await _tokenRepository.GetActiveForRideAsync(rideId, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.NotEqual(result.Value, stored!.TokenHash);
        Assert.Equal(_tokenHasher.Hash(result.Value), stored.TokenHash);
    }

    [Fact]
    public async Task CreateToken_ByNonOwner_ReturnsNotFound()
    {
        var (_, rideId) = await CreateRideAsync();

        var result = await _createTokenHandler.Handle(new CreateRideShareTokenCommand(Guid.NewGuid(), rideId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task CreateToken_WhenActiveTokenAlreadyExists_RevokesPreviousOne()
    {
        var (customerId, rideId) = await CreateRideAsync();
        var firstResult = await _createTokenHandler.Handle(new CreateRideShareTokenCommand(customerId, rideId), CancellationToken.None);

        var secondResult = await _createTokenHandler.Handle(new CreateRideShareTokenCommand(customerId, rideId), CancellationToken.None);

        Assert.True(secondResult.IsSuccess);
        var firstView = await _publicViewHandler.Handle(new GetPublicRideShareViewQuery(firstResult.Value), CancellationToken.None);
        Assert.False(firstView.IsSuccess);
    }

    [Fact]
    public async Task RevokeToken_Succeeds_AndTokenNoLongerValid()
    {
        var (customerId, rideId) = await CreateRideAsync();
        var createResult = await _createTokenHandler.Handle(new CreateRideShareTokenCommand(customerId, rideId), CancellationToken.None);

        var revokeResult = await _revokeTokenHandler.Handle(new RevokeRideShareTokenCommand(customerId, rideId), CancellationToken.None);

        Assert.True(revokeResult.IsSuccess);
        var view = await _publicViewHandler.Handle(new GetPublicRideShareViewQuery(createResult.Value), CancellationToken.None);
        Assert.False(view.IsSuccess);
    }

    [Fact]
    public async Task GetPublicView_WithValidToken_ReturnsNarrowView()
    {
        var (customerId, rideId) = await CreateRideAsync();
        var createResult = await _createTokenHandler.Handle(new CreateRideShareTokenCommand(customerId, rideId), CancellationToken.None);

        var view = await _publicViewHandler.Handle(new GetPublicRideShareViewQuery(createResult.Value), CancellationToken.None);

        Assert.True(view.IsSuccess);
        Assert.Equal(RideStatus.Draft.ToString(), view.Value.Status);
    }

    [Fact]
    public async Task GetPublicView_WithUnknownToken_ReturnsNotFound()
    {
        var view = await _publicViewHandler.Handle(new GetPublicRideShareViewQuery("does-not-exist"), CancellationToken.None);

        Assert.False(view.IsSuccess);
        Assert.Equal(ErrorType.NotFound, view.ErrorType);
    }
}

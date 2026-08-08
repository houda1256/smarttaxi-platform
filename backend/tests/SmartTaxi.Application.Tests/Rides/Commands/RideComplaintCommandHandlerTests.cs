using SmartTaxi.Application.Common;
using SmartTaxi.Application.Rides.Commands.CreateRide;
using SmartTaxi.Application.Rides.Commands.DismissRideComplaint;
using SmartTaxi.Application.Rides.Commands.DriverAcceptRide;
using SmartTaxi.Application.Rides.Commands.ResolveRideComplaint;
using SmartTaxi.Application.Rides.Commands.SelectDriver;
using SmartTaxi.Application.Rides.Commands.SubmitRideComplaint;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.Rides.Commands;

public class RideComplaintCommandHandlerTests
{
    private readonly FakeRideRepository _rideRepository = new();
    private readonly FakeRideDriverRecommendationRepository _recommendationRepository = new();
    private readonly FakeDriverReservationHoldRepository _holdRepository = new();
    private readonly FakeDriverProfileRepository _driverRepository = new();
    private readonly FakeDriverSearchPolicy _searchPolicy = new();
    private readonly FakeRideComplaintRepository _complaintRepository = new();

    private readonly CreateRideCommandHandler _createHandler;
    private readonly SelectDriverCommandHandler _selectHandler;
    private readonly DriverAcceptRideCommandHandler _acceptHandler;
    private readonly SubmitRideComplaintCommandHandler _submitHandler;
    private readonly ResolveRideComplaintCommandHandler _resolveHandler;
    private readonly DismissRideComplaintCommandHandler _dismissHandler;

    public RideComplaintCommandHandlerTests()
    {
        _createHandler = new CreateRideCommandHandler(_rideRepository);
        _selectHandler = new SelectDriverCommandHandler(_rideRepository, _recommendationRepository, _holdRepository, _searchPolicy);
        _acceptHandler = new DriverAcceptRideCommandHandler(_rideRepository, _holdRepository, _driverRepository);
        _submitHandler = new SubmitRideComplaintCommandHandler(_rideRepository, _driverRepository, _complaintRepository);
        _resolveHandler = new ResolveRideComplaintCommandHandler(_complaintRepository);
        _dismissHandler = new DismissRideComplaintCommandHandler(_complaintRepository);
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

        await _recommendationRepository.ReplaceForRideAsync(
            rideId,
            [new RideDriverRecommendation(rideId, driver.Id, vehicle.Id, 2.0m, 5, 40m, "Proche", 1, DateTime.UtcNow)],
            CancellationToken.None);

        await _selectHandler.Handle(new SelectDriverCommand(customerId, rideId, driver.Id, vehicle.Id), CancellationToken.None);
        await _acceptHandler.Handle(new DriverAcceptRideCommand(driverUserId, rideId), CancellationToken.None);

        return (customerId, rideId, driverUserId);
    }

    [Fact]
    public async Task SubmitComplaint_ByCustomerAgainstDriver_Succeeds()
    {
        var (customerId, rideId, _) = await CreateAcceptedRideAsync();

        var result = await _submitHandler.Handle(
            new SubmitRideComplaintCommand(customerId, rideId, RideComplaintCategory.DriverBehavior, "Conduite dangereuse"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SubmitComplaint_ByNonParticipant_ReturnsForbidden()
    {
        var (_, rideId, _) = await CreateAcceptedRideAsync();

        var result = await _submitHandler.Handle(
            new SubmitRideComplaintCommand(Guid.NewGuid(), rideId, RideComplaintCategory.Other, "Test"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task ResolveComplaint_Succeeds()
    {
        var (customerId, rideId, _) = await CreateAcceptedRideAsync();
        var submitResult = await _submitHandler.Handle(
            new SubmitRideComplaintCommand(customerId, rideId, RideComplaintCategory.FareIssue, "Tarif incorrect"), CancellationToken.None);

        var result = await _resolveHandler.Handle(new ResolveRideComplaintCommand(submitResult.Value, "Remboursement effectué"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var complaint = await _complaintRepository.GetByIdAsync(submitResult.Value, CancellationToken.None);
        Assert.Equal(RideComplaintStatus.Resolved, complaint!.Status);
    }

    [Fact]
    public async Task DismissComplaint_Succeeds()
    {
        var (customerId, rideId, _) = await CreateAcceptedRideAsync();
        var submitResult = await _submitHandler.Handle(
            new SubmitRideComplaintCommand(customerId, rideId, RideComplaintCategory.Other, "Test"), CancellationToken.None);

        var result = await _dismissHandler.Handle(new DismissRideComplaintCommand(submitResult.Value, "Non fondé"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var complaint = await _complaintRepository.GetByIdAsync(submitResult.Value, CancellationToken.None);
        Assert.Equal(RideComplaintStatus.Dismissed, complaint!.Status);
    }

    [Fact]
    public async Task ResolveComplaint_AlreadyResolved_ReturnsConflict()
    {
        var (customerId, rideId, _) = await CreateAcceptedRideAsync();
        var submitResult = await _submitHandler.Handle(
            new SubmitRideComplaintCommand(customerId, rideId, RideComplaintCategory.Other, "Test"), CancellationToken.None);
        await _resolveHandler.Handle(new ResolveRideComplaintCommand(submitResult.Value, "Traité"), CancellationToken.None);

        var result = await _resolveHandler.Handle(new ResolveRideComplaintCommand(submitResult.Value, "Traité"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}

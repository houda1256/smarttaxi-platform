using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Application.Rides.Commands.ActivateSos;

/// <summary>
/// Creates the Ride module's own safety record and raises RideSOSActivated —
/// there is no Incident/emergency-service module yet to escalate into (see
/// RideSafetyEvent's doc comment), so this is the complete integration point
/// a future Administration/Support module can subscribe to.
/// </summary>
public sealed class ActivateSosCommandHandler : ICommandHandler<ActivateSosCommand, Result<Guid>>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NotParticipantError = "Seuls les participants à la course peuvent déclencher une alerte SOS.";

    private readonly IRideRepository _rideRepository;
    private readonly IDriverProfileRepository _driverRepository;
    private readonly IRideSafetyEventRepository _safetyEventRepository;

    public ActivateSosCommandHandler(
        IRideRepository rideRepository, IDriverProfileRepository driverRepository, IRideSafetyEventRepository safetyEventRepository)
    {
        _rideRepository = rideRepository;
        _driverRepository = driverRepository;
        _safetyEventRepository = safetyEventRepository;
    }

    public async Task<Result<Guid>> Handle(ActivateSosCommand command, CancellationToken cancellationToken)
    {
        if (!GeoCoordinate.TryCreate(command.Latitude, command.Longitude, out var location, out var locationError))
        {
            return Result<Guid>.Failure(locationError, ErrorType.Validation);
        }

        var ride = await _rideRepository.GetByIdAsync(command.RideId, cancellationToken);

        if (ride is null)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var isCustomer = command.RequestingUserId == ride.CustomerId;
        var isDriver = false;

        if (!isCustomer && ride.SelectedDriverId is not null)
        {
            var driver = await _driverRepository.GetByIdAsync(ride.SelectedDriverId.Value, cancellationToken);
            isDriver = driver is not null && driver.UserId == command.RequestingUserId;
        }

        if (!isCustomer && !isDriver)
        {
            return Result<Guid>.Failure(NotParticipantError, ErrorType.Forbidden);
        }

        RideSafetyEvent safetyEvent;

        try
        {
            safetyEvent = RideSafetyEvent.Trigger(ride.Id, command.RequestingUserId, location, command.Reason, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _safetyEventRepository.AddAsync(safetyEvent, cancellationToken);

        return Result<Guid>.Success(safetyEvent.Id);
    }
}

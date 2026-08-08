using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Application.Rides.Commands.UpdateDriverAvailabilityLocation;

public sealed class UpdateDriverAvailabilityLocationCommandHandler
    : ICommandHandler<UpdateDriverAvailabilityLocationCommand, Result>
{
    private const string NotFoundError = "Profil chauffeur introuvable.";

    private readonly IDriverProfileRepository _driverRepository;

    public UpdateDriverAvailabilityLocationCommandHandler(IDriverProfileRepository driverRepository)
    {
        _driverRepository = driverRepository;
    }

    public async Task<Result> Handle(UpdateDriverAvailabilityLocationCommand command, CancellationToken cancellationToken)
    {
        if (!GeoCoordinate.TryCreate(command.Latitude, command.Longitude, out _, out var error))
        {
            return Result.Failure(error, ErrorType.Validation);
        }

        var driver = await _driverRepository.GetByUserIdAsync(command.RequestingUserId, cancellationToken);

        if (driver is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        driver.UpdateLastKnownLocation(command.Latitude, command.Longitude, DateTime.UtcNow);
        await _driverRepository.UpdateAsync(driver, cancellationToken);

        return Result.Success();
    }
}

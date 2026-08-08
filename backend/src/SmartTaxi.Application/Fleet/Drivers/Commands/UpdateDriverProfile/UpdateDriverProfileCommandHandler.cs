using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;

namespace SmartTaxi.Application.Fleet.Drivers.Commands.UpdateDriverProfile;

public sealed class UpdateDriverProfileCommandHandler : ICommandHandler<UpdateDriverProfileCommand, Result>
{
    private const string NotFoundError = "Profil chauffeur introuvable.";

    private readonly IDriverProfileRepository _repository;

    public UpdateDriverProfileCommandHandler(IDriverProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(UpdateDriverProfileCommand command, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(command.DriverProfileId, cancellationToken);

        if (profile is null || profile.UserId != command.RequestingUserId)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        profile.UpdateProfile(command.DriverLicenseNumber, command.DriverLicenseExpiration, command.TaxiLicenseNumber, DateTime.UtcNow);
        await _repository.UpdateAsync(profile, cancellationToken);

        return Result.Success();
    }
}

using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Maintenance.Abstractions;

namespace SmartTaxi.Application.Maintenance.Commands.UpdateGarageProfile;

public sealed class UpdateGarageProfileCommandHandler : ICommandHandler<UpdateGarageProfileCommand, Result>
{
    private const string NotFoundError = "Profil garage introuvable.";

    private readonly IGarageProfileRepository _profileRepository;

    public UpdateGarageProfileCommandHandler(IGarageProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<Result> Handle(UpdateGarageProfileCommand command, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(command.UserId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        try
        {
            profile.UpdateProfile(
                command.BusinessName, command.LegalName, command.Address, command.City, command.SupportedVehicleCategories,
                command.AvailableServices, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message, ErrorType.Validation);
        }

        await _profileRepository.UpdateAsync(profile, cancellationToken);
        return Result.Success();
    }
}

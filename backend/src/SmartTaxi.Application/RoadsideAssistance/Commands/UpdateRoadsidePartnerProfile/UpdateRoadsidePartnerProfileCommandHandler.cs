using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.UpdateRoadsidePartnerProfile;

public sealed class UpdateRoadsidePartnerProfileCommandHandler : ICommandHandler<UpdateRoadsidePartnerProfileCommand, Result>
{
    private const string NotFoundError = "Profil d'assistance routière introuvable.";

    private readonly IRoadsidePartnerProfileRepository _profileRepository;

    public UpdateRoadsidePartnerProfileCommandHandler(IRoadsidePartnerProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<Result> Handle(UpdateRoadsidePartnerProfileCommand command, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(command.UserId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        try
        {
            profile.UpdateProfile(
                command.BusinessName, command.LegalName, command.Address, command.City, command.SupportedServiceTypes,
                command.SupportedVehicleCategories, command.Latitude, command.Longitude, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message, ErrorType.Validation);
        }

        await _profileRepository.UpdateAsync(profile, cancellationToken);
        return Result.Success();
    }
}

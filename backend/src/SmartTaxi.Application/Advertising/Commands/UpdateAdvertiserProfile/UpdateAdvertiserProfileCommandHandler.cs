using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.UpdateAdvertiserProfile;

public sealed class UpdateAdvertiserProfileCommandHandler : ICommandHandler<UpdateAdvertiserProfileCommand, Result>
{
    private const string NotFoundError = "Profil annonceur introuvable.";

    private readonly IAdvertiserProfileRepository _profileRepository;

    public UpdateAdvertiserProfileCommandHandler(IAdvertiserProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<Result> Handle(UpdateAdvertiserProfileCommand command, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(command.UserId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        try
        {
            profile.UpdateProfile(
                command.BusinessName, command.LegalName, command.TaxIdentifier, command.City, command.Address, command.ContactEmail,
                command.ContactPhone, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message, ErrorType.Validation);
        }

        await _profileRepository.UpdateAsync(profile, cancellationToken);
        return Result.Success();
    }
}

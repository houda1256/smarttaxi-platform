using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Application.Maintenance.Commands.RegisterGarageProfile;

/// <summary>
/// Does not duplicate Identity's own professional-account approval state —
/// this only creates the Maintenance-owned business metadata for a user
/// Identity already recognizes as holding the GaragePartner role. One
/// profile per UserId, enforced by the repository's unique-index-backed
/// TryAddAsync. Mirrors RegisterAdvertiserProfileCommandHandler exactly.
/// </summary>
public sealed class RegisterGarageProfileCommandHandler : ICommandHandler<RegisterGarageProfileCommand, Result<Guid>>
{
    private const string NotGaragePartnerRoleError = "Seul un utilisateur avec le rôle GaragePartner peut créer un profil garage.";
    private const string AlreadyRegisteredError = "Un profil garage existe déjà pour cet utilisateur.";

    private readonly IGarageProfileRepository _profileRepository;
    private readonly IUserRepository _userRepository;

    public RegisterGarageProfileCommandHandler(IGarageProfileRepository profileRepository, IUserRepository userRepository)
    {
        _profileRepository = profileRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<Guid>> Handle(RegisterGarageProfileCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null || !user.HasRole(UserRole.GaragePartner))
        {
            return Result<Guid>.Failure(NotGaragePartnerRoleError, ErrorType.Forbidden);
        }

        if (await _profileRepository.GetByUserIdAsync(command.UserId, cancellationToken) is not null)
        {
            return Result<Guid>.Failure(AlreadyRegisteredError, ErrorType.Conflict);
        }

        GarageProfile profile;

        try
        {
            profile = GarageProfile.Register(
                command.UserId, command.BusinessName, command.LegalName, command.Address, command.City,
                command.SupportedVehicleCategories, command.AvailableServices, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        if (!await _profileRepository.TryAddAsync(profile, cancellationToken))
        {
            return Result<Guid>.Failure(AlreadyRegisteredError, ErrorType.Conflict);
        }

        return Result<Guid>.Success(profile.Id);
    }
}

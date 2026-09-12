using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Entities;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.RegisterRoadsidePartnerProfile;

/// <summary>
/// Does not duplicate Identity's own professional-account approval state —
/// this only creates the Roadside-owned business metadata for a user Identity
/// already recognizes as holding the RoadsideAssistancePartner role. One
/// profile per UserId, enforced by the repository's unique-index-backed
/// TryAddAsync. Mirrors RegisterGarageProfileCommandHandler exactly.
/// </summary>
public sealed class RegisterRoadsidePartnerProfileCommandHandler : ICommandHandler<RegisterRoadsidePartnerProfileCommand, Result<Guid>>
{
    private const string NotPartnerRoleError = "Seul un utilisateur avec le rôle RoadsideAssistancePartner peut créer un profil d'assistance routière.";
    private const string AlreadyRegisteredError = "Un profil d'assistance routière existe déjà pour cet utilisateur.";

    private readonly IRoadsidePartnerProfileRepository _profileRepository;
    private readonly IUserRepository _userRepository;

    public RegisterRoadsidePartnerProfileCommandHandler(IRoadsidePartnerProfileRepository profileRepository, IUserRepository userRepository)
    {
        _profileRepository = profileRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<Guid>> Handle(RegisterRoadsidePartnerProfileCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null || !user.HasRole(UserRole.RoadsideAssistancePartner))
        {
            return Result<Guid>.Failure(NotPartnerRoleError, ErrorType.Forbidden);
        }

        if (await _profileRepository.GetByUserIdAsync(command.UserId, cancellationToken) is not null)
        {
            return Result<Guid>.Failure(AlreadyRegisteredError, ErrorType.Conflict);
        }

        RoadsidePartnerProfile profile;

        try
        {
            profile = RoadsidePartnerProfile.Register(
                command.UserId, command.BusinessName, command.LegalName, command.Address, command.City,
                command.SupportedServiceTypes, command.SupportedVehicleCategories, command.Latitude, command.Longitude, DateTime.UtcNow);
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

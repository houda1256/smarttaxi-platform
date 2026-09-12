using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Advertising.Commands.RegisterAdvertiserProfile;

/// <summary>
/// Does not duplicate Identity's own professional-account approval state —
/// this only creates the Advertising-owned business/billing metadata for a
/// user Identity already recognizes as holding (or being eligible to hold)
/// the Advertiser role. One profile per UserId, enforced by the repository's
/// unique-index-backed TryAddAsync.
/// </summary>
public sealed class RegisterAdvertiserProfileCommandHandler : ICommandHandler<RegisterAdvertiserProfileCommand, Result<Guid>>
{
    private const string NotAdvertiserRoleError = "Seul un utilisateur avec le rôle Advertiser peut créer un profil annonceur.";
    private const string AlreadyRegisteredError = "Un profil annonceur existe déjà pour cet utilisateur.";

    private readonly IAdvertiserProfileRepository _profileRepository;
    private readonly IUserRepository _userRepository;

    public RegisterAdvertiserProfileCommandHandler(IAdvertiserProfileRepository profileRepository, IUserRepository userRepository)
    {
        _profileRepository = profileRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<Guid>> Handle(RegisterAdvertiserProfileCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null || !user.HasRole(UserRole.Advertiser))
        {
            return Result<Guid>.Failure(NotAdvertiserRoleError, ErrorType.Forbidden);
        }

        if (await _profileRepository.GetByUserIdAsync(command.UserId, cancellationToken) is not null)
        {
            return Result<Guid>.Failure(AlreadyRegisteredError, ErrorType.Conflict);
        }

        AdvertiserProfile profile;

        try
        {
            profile = AdvertiserProfile.Register(
                command.UserId, command.BusinessName, command.LegalName, command.TaxIdentifier, command.City, command.Address,
                command.ContactEmail, command.ContactPhone, DateTime.UtcNow);
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

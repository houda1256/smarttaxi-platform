using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Fleet.Drivers.Commands.CreateDriverProfile;

public sealed class CreateDriverProfileCommandHandler : ICommandHandler<CreateDriverProfileCommand, Result<Guid>>
{
    private const string UserNotFoundError = "Utilisateur introuvable.";
    private const string MissingRoleError = "L'utilisateur doit avoir le rôle Driver.";
    private const string AlreadyExistsError = "Un profil chauffeur existe déjà pour cet utilisateur.";

    private readonly IUserRepository _userRepository;
    private readonly IDriverProfileRepository _repository;

    public CreateDriverProfileCommandHandler(IUserRepository userRepository, IDriverProfileRepository repository)
    {
        _userRepository = userRepository;
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreateDriverProfileCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return Result<Guid>.Failure(UserNotFoundError, ErrorType.NotFound);
        }

        if (!user.HasRole(UserRole.Driver))
        {
            return Result<Guid>.Failure(MissingRoleError, ErrorType.Forbidden);
        }

        var existing = await _repository.GetByUserIdAsync(command.UserId, cancellationToken);

        if (existing is not null)
        {
            return Result<Guid>.Failure(AlreadyExistsError, ErrorType.Conflict);
        }

        var profile = DriverProfile.Create(
            command.UserId, command.DriverLicenseNumber, command.DriverLicenseExpiration, command.TaxiLicenseNumber,
            command.IndependentDriver, DateTime.UtcNow);

        await _repository.AddAsync(profile, cancellationToken);

        return Result<Guid>.Success(profile.Id);
    }
}

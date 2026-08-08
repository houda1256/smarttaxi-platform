using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Owners.Abstractions;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Fleet.Owners.Entities;
using SmartTaxi.Domain.Fleet.Owners.ValueObjects;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Fleet.Owners.Commands.CreateCompanyOwnerProfile;

public sealed class CreateCompanyOwnerProfileCommandHandler
    : ICommandHandler<CreateCompanyOwnerProfileCommand, Result<Guid>>
{
    private const string UserNotFoundError = "Utilisateur introuvable.";
    private const string MissingRoleError = "L'utilisateur doit avoir le rôle TaxiOwner.";
    private const string AlreadyExistsError = "Un profil propriétaire existe déjà pour cet utilisateur.";

    private readonly IUserRepository _userRepository;
    private readonly ITaxiOwnerProfileRepository _repository;

    public CreateCompanyOwnerProfileCommandHandler(IUserRepository userRepository, ITaxiOwnerProfileRepository repository)
    {
        _userRepository = userRepository;
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreateCompanyOwnerProfileCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return Result<Guid>.Failure(UserNotFoundError, ErrorType.NotFound);
        }

        if (!user.HasRole(UserRole.TaxiOwner))
        {
            return Result<Guid>.Failure(MissingRoleError, ErrorType.Forbidden);
        }

        var existing = await _repository.GetByUserIdAsync(command.UserId, cancellationToken);

        if (existing is not null)
        {
            return Result<Guid>.Failure(AlreadyExistsError, ErrorType.Conflict);
        }

        if (!Address.TryCreate(command.Street, command.City, command.PostalCode, command.Country, out var address, out var addressError))
        {
            return Result<Guid>.Failure(addressError, ErrorType.Validation);
        }

        if (!BankInformation.TryCreate(
                command.BankName, command.AccountHolderName, command.AccountNumber, command.SwiftOrBic,
                out var bankInfo, out var bankError))
        {
            return Result<Guid>.Failure(bankError, ErrorType.Validation);
        }

        var profile = TaxiOwnerProfile.CreateCompany(
            command.UserId, command.LegalName, command.TradeName, command.TaxIdentifier, command.RegistrationNumber,
            address, bankInfo, DateTime.UtcNow);

        await _repository.AddAsync(profile, cancellationToken);

        return Result<Guid>.Success(profile.Id);
    }
}

using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Owners.Abstractions;
using SmartTaxi.Domain.Fleet.Owners.Enums;
using SmartTaxi.Domain.Fleet.Owners.ValueObjects;

namespace SmartTaxi.Application.Fleet.Owners.Commands.UpdateOwnerProfile;

public sealed class UpdateOwnerProfileCommandHandler : ICommandHandler<UpdateOwnerProfileCommand, Result>
{
    private const string NotFoundError = "Profil propriétaire introuvable.";
    private const string ForbiddenError = "Vous ne pouvez modifier que votre propre profil.";

    private readonly ITaxiOwnerProfileRepository _repository;

    public UpdateOwnerProfileCommandHandler(ITaxiOwnerProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(UpdateOwnerProfileCommand command, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(command.OwnerId, cancellationToken);

        if (profile is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (profile.UserId != command.RequestingUserId)
        {
            return Result.Failure(ForbiddenError, ErrorType.Forbidden);
        }

        if (!Address.TryCreate(command.Street, command.City, command.PostalCode, command.Country, out var address, out var addressError))
        {
            return Result.Failure(addressError, ErrorType.Validation);
        }

        if (!BankInformation.TryCreate(
                command.BankName, command.AccountHolderName, command.AccountNumber, command.SwiftOrBic,
                out var bankInfo, out var bankError))
        {
            return Result.Failure(bankError, ErrorType.Validation);
        }

        var utcNow = DateTime.UtcNow;

        if (profile.OwnerType == OwnerType.Individual)
        {
            if (command.FirstName is null || command.LastName is null || command.NationalId is null)
            {
                return Result.Failure("Prénom, nom et numéro d'identité nationale requis.", ErrorType.Validation);
            }

            profile.UpdateIndividualDetails(command.FirstName, command.LastName, command.NationalId, utcNow);
        }
        else
        {
            if (command.LegalName is null || command.TradeName is null || command.TaxIdentifier is null
                || command.RegistrationNumber is null)
            {
                return Result.Failure("Raison sociale, nom commercial, identifiant fiscal et numéro d'enregistrement requis.", ErrorType.Validation);
            }

            profile.UpdateCompanyDetails(command.LegalName, command.TradeName, command.TaxIdentifier, command.RegistrationNumber, utcNow);
        }

        profile.UpdateContactInformation(address, bankInfo, utcNow);
        await _repository.UpdateAsync(profile, cancellationToken);

        return Result.Success();
    }
}

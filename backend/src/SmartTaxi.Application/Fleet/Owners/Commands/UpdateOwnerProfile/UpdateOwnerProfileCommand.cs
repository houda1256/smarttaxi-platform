using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Owners.Commands.UpdateOwnerProfile;

public sealed record UpdateOwnerProfileCommand(
    Guid RequestingUserId,
    Guid OwnerId,
    string? FirstName,
    string? LastName,
    string? NationalId,
    string? LegalName,
    string? TradeName,
    string? TaxIdentifier,
    string? RegistrationNumber,
    string Street,
    string City,
    string? PostalCode,
    string Country,
    string BankName,
    string AccountHolderName,
    string AccountNumber,
    string? SwiftOrBic) : ICommand<Result>;

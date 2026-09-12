using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Owners.Commands.CreateCompanyOwnerProfile;

public sealed record CreateCompanyOwnerProfileCommand(
    Guid UserId,
    string LegalName,
    string TradeName,
    string TaxIdentifier,
    string RegistrationNumber,
    string Street,
    string City,
    string? PostalCode,
    string Country,
    string BankName,
    string AccountHolderName,
    string AccountNumber,
    string? SwiftOrBic) : ICommand<Result<Guid>>;

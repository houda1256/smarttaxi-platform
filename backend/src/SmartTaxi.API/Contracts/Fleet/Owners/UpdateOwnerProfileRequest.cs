namespace SmartTaxi.API.Contracts.Fleet.Owners;

public sealed record UpdateOwnerProfileRequest(
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
    string? SwiftOrBic);

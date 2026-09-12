namespace SmartTaxi.API.Contracts.Fleet.Owners;

public sealed record CreateIndividualOwnerProfileRequest(
    string FirstName,
    string LastName,
    string NationalId,
    string Street,
    string City,
    string? PostalCode,
    string Country,
    string BankName,
    string AccountHolderName,
    string AccountNumber,
    string? SwiftOrBic);

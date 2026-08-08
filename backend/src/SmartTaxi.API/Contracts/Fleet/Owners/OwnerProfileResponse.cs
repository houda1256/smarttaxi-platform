using SmartTaxi.Application.Fleet.Owners;

namespace SmartTaxi.API.Contracts.Fleet.Owners;

public sealed record OwnerProfileResponse(
    Guid Id,
    Guid UserId,
    string OwnerType,
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
    string? SwiftOrBic,
    string VerificationStatus,
    string AccountStatus,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static OwnerProfileResponse FromSummary(OwnerProfileSummary summary) => new(
        summary.Id,
        summary.UserId,
        summary.OwnerType.ToString(),
        summary.FirstName,
        summary.LastName,
        summary.NationalId,
        summary.LegalName,
        summary.TradeName,
        summary.TaxIdentifier,
        summary.RegistrationNumber,
        summary.Street,
        summary.City,
        summary.PostalCode,
        summary.Country,
        summary.BankName,
        summary.AccountHolderName,
        summary.AccountNumber,
        summary.SwiftOrBic,
        summary.VerificationStatus.ToString(),
        summary.AccountStatus.ToString(),
        summary.CreatedAt,
        summary.UpdatedAt);
}

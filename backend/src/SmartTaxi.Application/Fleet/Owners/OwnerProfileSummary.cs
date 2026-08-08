using SmartTaxi.Domain.Fleet.Owners.Entities;
using SmartTaxi.Domain.Fleet.Owners.Enums;

namespace SmartTaxi.Application.Fleet.Owners;

public sealed record OwnerProfileSummary(
    Guid Id,
    Guid UserId,
    OwnerType OwnerType,
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
    OwnerVerificationStatus VerificationStatus,
    OwnerAccountStatus AccountStatus,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static OwnerProfileSummary FromEntity(TaxiOwnerProfile profile) => new(
        profile.Id,
        profile.UserId,
        profile.OwnerType,
        profile.FirstName,
        profile.LastName,
        profile.NationalId,
        profile.LegalName,
        profile.TradeName,
        profile.TaxIdentifier,
        profile.RegistrationNumber,
        profile.Address.Street,
        profile.Address.City,
        profile.Address.PostalCode,
        profile.Address.Country,
        profile.BankInformation.BankName,
        profile.BankInformation.AccountHolderName,
        profile.BankInformation.AccountNumber,
        profile.BankInformation.SwiftOrBic,
        profile.VerificationStatus,
        profile.AccountStatus,
        profile.CreatedAt,
        profile.UpdatedAt);
}

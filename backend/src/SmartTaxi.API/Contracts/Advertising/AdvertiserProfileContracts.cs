using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.API.Contracts.Advertising;

public sealed record RegisterAdvertiserProfileRequest(
    string BusinessName, string LegalName, string TaxIdentifier, string City, string Address, string ContactEmail, string? ContactPhone);

public sealed record UpdateAdvertiserProfileRequest(
    string BusinessName, string LegalName, string TaxIdentifier, string City, string Address, string ContactEmail, string? ContactPhone);

public sealed record AdvertiserProfileResponse(
    Guid Id, string BusinessName, string LegalName, string TaxIdentifier, string City, string Address, string ContactEmail,
    string? ContactPhone, bool IsActive, DateTime CreatedAtUtc)
{
    public static AdvertiserProfileResponse FromEntity(AdvertiserProfile profile) =>
        new(profile.Id, profile.BusinessName, profile.LegalName, profile.TaxIdentifier, profile.City, profile.Address, profile.ContactEmail,
            profile.ContactPhone, profile.IsActive, profile.CreatedAtUtc);
}

using SmartTaxi.Domain.RoadsideAssistance.Entities;

namespace SmartTaxi.API.Contracts.RoadsideAssistance;

public sealed record RegisterRoadsidePartnerProfileRequest(
    string BusinessName, string? LegalName, string Address, string City, string? SupportedServiceTypes,
    string? SupportedVehicleCategories, double? Latitude, double? Longitude);

public sealed record UpdateRoadsidePartnerProfileRequest(
    string BusinessName, string? LegalName, string Address, string City, string? SupportedServiceTypes,
    string? SupportedVehicleCategories, double? Latitude, double? Longitude);

public sealed record RoadsidePartnerProfileResponse(
    Guid Id, Guid UserId, string BusinessName, string? LegalName, string Address, string City, string? SupportedServiceTypes,
    string? SupportedVehicleCategories, double? Latitude, double? Longitude, bool IsActive, DateTime CreatedAtUtc, DateTime UpdatedAtUtc)
{
    public static RoadsidePartnerProfileResponse FromEntity(RoadsidePartnerProfile profile) => new(
        profile.Id, profile.UserId, profile.BusinessName, profile.LegalName, profile.Address, profile.City, profile.SupportedServiceTypes,
        profile.SupportedVehicleCategories, profile.Latitude, profile.Longitude, profile.IsActive, profile.CreatedAtUtc, profile.UpdatedAtUtc);
}

using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.API.Contracts.Maintenance;

public sealed record RegisterGarageProfileRequest(
    string BusinessName, string? LegalName, string Address, string City, string? SupportedVehicleCategories, string? AvailableServices);

public sealed record UpdateGarageProfileRequest(
    string BusinessName, string? LegalName, string Address, string City, string? SupportedVehicleCategories, string? AvailableServices);

public sealed record GarageProfileResponse(
    Guid Id, Guid UserId, string BusinessName, string? LegalName, string Address, string City, string? SupportedVehicleCategories,
    string? AvailableServices, bool IsActive, DateTime CreatedAtUtc, DateTime UpdatedAtUtc)
{
    public static GarageProfileResponse FromEntity(GarageProfile profile) => new(
        profile.Id, profile.UserId, profile.BusinessName, profile.LegalName, profile.Address, profile.City,
        profile.SupportedVehicleCategories, profile.AvailableServices, profile.IsActive, profile.CreatedAtUtc, profile.UpdatedAtUtc);
}

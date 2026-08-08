using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Domain.Identity.Professional;

public static class ProfessionalRoles
{
    public static readonly IReadOnlyCollection<UserRole> All =
    [
        UserRole.Driver,
        UserRole.TaxiOwner,
        UserRole.GaragePartner,
        UserRole.RoadsideAssistancePartner,
        UserRole.Advertiser,
        UserRole.BusinessCustomer
    ];

    public static bool IsProfessionalRole(UserRole role) => All.Contains(role);
}

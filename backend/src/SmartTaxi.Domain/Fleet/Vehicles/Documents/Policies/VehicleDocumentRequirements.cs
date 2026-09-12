using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;

namespace SmartTaxi.Domain.Fleet.Vehicles.Documents.Policies;

/// <summary>Which document types are critical for a vehicle to remain eligible for rides.</summary>
public static class VehicleDocumentRequirements
{
    public static readonly IReadOnlyCollection<VehicleDocumentType> Critical =
    [
        VehicleDocumentType.RegistrationDocument,
        VehicleDocumentType.Insurance,
        VehicleDocumentType.TechnicalInspection,
        VehicleDocumentType.TaxiLicense
    ];
}

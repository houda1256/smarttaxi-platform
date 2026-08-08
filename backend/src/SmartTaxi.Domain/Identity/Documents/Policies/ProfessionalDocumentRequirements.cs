using SmartTaxi.Domain.Identity.Documents.Enums;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Domain.Identity.Documents.Policies;

/// <summary>
/// Static catalog of the document types that are critical for a role to
/// remain professionally eligible. Deliberately not DB-configurable in this
/// sub-slice — full professional-account onboarding (sub-slice 2e) may revisit
/// this as a per-role, admin-configurable set.
/// </summary>
public static class ProfessionalDocumentRequirements
{
    private static readonly IReadOnlyDictionary<UserRole, IReadOnlyCollection<DocumentType>> RequirementsByRole =
        new Dictionary<UserRole, IReadOnlyCollection<DocumentType>>
        {
            [UserRole.Driver] = [DocumentType.DriverLicense, DocumentType.CriminalRecordCertificate],
            [UserRole.TaxiOwner] =
            [
                DocumentType.BusinessRegistrationCertificate,
                DocumentType.VehicleRegistrationCertificate,
                DocumentType.VehicleInsuranceCertificate
            ],
            [UserRole.GaragePartner] =
                [DocumentType.GarageOperatingLicense, DocumentType.BusinessRegistrationCertificate],
            [UserRole.RoadsideAssistancePartner] =
                [DocumentType.RoadsideAssistanceCertification, DocumentType.BusinessRegistrationCertificate],
            [UserRole.Advertiser] =
                [DocumentType.AdvertiserBusinessLicense, DocumentType.TaxIdentificationCertificate],
            [UserRole.BusinessCustomer] =
                [DocumentType.BusinessRegistrationCertificate, DocumentType.TaxIdentificationCertificate]
        };

    public static IReadOnlyCollection<DocumentType> GetCriticalDocumentTypes(UserRole role) =>
        RequirementsByRole.TryGetValue(role, out var types) ? types : [];
}

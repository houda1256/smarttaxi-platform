using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;

namespace SmartTaxi.Application.Fleet.Vehicles;

public sealed record VehicleEligibilityReport(bool IsEligible, IReadOnlyCollection<VehicleDocumentType> MissingOrInvalidDocumentTypes);

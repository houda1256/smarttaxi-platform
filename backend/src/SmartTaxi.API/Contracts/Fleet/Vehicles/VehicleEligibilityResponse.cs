using SmartTaxi.Application.Fleet.Vehicles;

namespace SmartTaxi.API.Contracts.Fleet.Vehicles;

public sealed record VehicleEligibilityResponse(bool IsEligible, IReadOnlyCollection<string> MissingOrInvalidDocumentTypes)
{
    public static VehicleEligibilityResponse FromReport(VehicleEligibilityReport report) => new(
        report.IsEligible, report.MissingOrInvalidDocumentTypes.Select(t => t.ToString()).ToList());
}

using SmartTaxi.Application.Fleet.Vehicles.Documents.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Policies;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;

namespace SmartTaxi.Application.Fleet.Vehicles;

/// <summary>
/// A vehicle is only fully eligible for rides when it has passed platform
/// verification, its operational status is Active, and every critical
/// document is currently approved and unexpired.
/// </summary>
public sealed class VehicleEligibilityChecker
{
    private readonly IVehicleDocumentRepository _documentRepository;

    public VehicleEligibilityChecker(IVehicleDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public async Task<VehicleEligibilityReport> CheckAsync(Vehicle vehicle, DateTime utcNow, CancellationToken cancellationToken)
    {
        var missing = new List<VehicleDocumentType>();

        foreach (var type in VehicleDocumentRequirements.Critical)
        {
            var latest = await _documentRepository.GetLatestForVehicleAndTypeAsync(vehicle.Id, type, cancellationToken);

            if (latest is null || !latest.IsCurrentlyValid(utcNow))
            {
                missing.Add(type);
            }
        }

        var isEligible = vehicle.IsCurrentlyEligibleForRides() && missing.Count == 0;

        return new VehicleEligibilityReport(isEligible, missing);
    }
}

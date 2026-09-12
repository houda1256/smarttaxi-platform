using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Alerts.Abstractions;
using SmartTaxi.Application.Fleet.Contracts.Abstractions;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles.Documents.Abstractions;
using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Domain.Fleet.Alerts.Entities;
using SmartTaxi.Domain.Fleet.Alerts.Enums;
using SmartTaxi.Domain.Fleet.Drivers.Enums;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Policies;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.Application.Fleet.Alerts.Commands.GenerateFleetAlerts;

/// <summary>
/// A modest, real (not stubbed) alert scan: expiring/expired critical vehicle
/// documents, suspended vehicles, and suspended drivers under an active
/// contract with this owner. No scheduler/background job wires this up yet —
/// it is invoked on demand (e.g. from a dashboard refresh or a future cron
/// endpoint), and skips conditions that already have an open alert.
/// </summary>
public sealed class GenerateFleetAlertsCommandHandler : ICommandHandler<GenerateFleetAlertsCommand, Result<int>>
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IVehicleDocumentRepository _vehicleDocumentRepository;
    private readonly IDriverOwnerContractRepository _contractRepository;
    private readonly IDriverProfileRepository _driverRepository;
    private readonly IFleetAlertRepository _alertRepository;
    private readonly IDocumentExpirationPolicy _expirationPolicy;

    public GenerateFleetAlertsCommandHandler(
        IVehicleRepository vehicleRepository, IVehicleDocumentRepository vehicleDocumentRepository,
        IDriverOwnerContractRepository contractRepository, IDriverProfileRepository driverRepository,
        IFleetAlertRepository alertRepository, IDocumentExpirationPolicy expirationPolicy)
    {
        _vehicleRepository = vehicleRepository;
        _vehicleDocumentRepository = vehicleDocumentRepository;
        _contractRepository = contractRepository;
        _driverRepository = driverRepository;
        _alertRepository = alertRepository;
        _expirationPolicy = expirationPolicy;
    }

    public async Task<Result<int>> Handle(GenerateFleetAlertsCommand command, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var createdCount = 0;

        var vehicles = await _vehicleRepository.GetForOwnerAsync(command.OwnerId, cancellationToken);

        foreach (var vehicle in vehicles)
        {
            if (vehicle.OperationalStatus == VehicleOperationalStatus.Suspended
                && await TryRaiseAsync(command.OwnerId, FleetAlertType.SuspendedVehicle, vehicle.Id,
                    "Le véhicule est suspendu.", cancellationToken))
            {
                createdCount++;
            }

            foreach (var documentType in VehicleDocumentRequirements.Critical)
            {
                var document = await _vehicleDocumentRepository.GetLatestForVehicleAndTypeAsync(
                    vehicle.Id, documentType, cancellationToken);

                if (document is null || document.Status != VehicleDocumentStatus.Approved || document.ExpirationDate is null)
                {
                    continue;
                }

                var isExpiringSoon = document.ExpirationDate <= utcNow.AddDays(_expirationPolicy.ReminderLeadDays);

                if (!isExpiringSoon)
                {
                    continue;
                }

                var alertType = MapToAlertType(documentType);

                if (await TryRaiseAsync(command.OwnerId, alertType, vehicle.Id,
                        $"Le document {documentType} du véhicule arrive à expiration ou a expiré.", cancellationToken))
                {
                    createdCount++;
                }
            }
        }

        var contracts = await _contractRepository.GetForOwnerAsync(command.OwnerId, cancellationToken);
        var driverIds = contracts.Select(c => c.DriverId).Distinct();

        foreach (var driverId in driverIds)
        {
            var driver = await _driverRepository.GetByIdAsync(driverId, cancellationToken);

            if (driver is not null && driver.VerificationStatus == DriverVerificationStatus.Suspended
                && await TryRaiseAsync(command.OwnerId, FleetAlertType.SuspendedDriver, driver.Id,
                    "Le chauffeur est suspendu.", cancellationToken))
            {
                createdCount++;
            }
        }

        return Result<int>.Success(createdCount);
    }

    private async Task<bool> TryRaiseAsync(
        Guid ownerId, FleetAlertType alertType, Guid relatedEntityId, string message, CancellationToken cancellationToken)
    {
        var alreadyOpen = await _alertRepository.ExistsOpenAlertAsync(ownerId, alertType, relatedEntityId, cancellationToken);

        if (alreadyOpen)
        {
            return false;
        }

        var alert = new FleetAlert(ownerId, null, alertType, relatedEntityId, message, DateTime.UtcNow);
        await _alertRepository.AddAsync(alert, cancellationToken);

        return true;
    }

    private static FleetAlertType MapToAlertType(VehicleDocumentType documentType) => documentType switch
    {
        VehicleDocumentType.Insurance => FleetAlertType.ExpiringInsurance,
        VehicleDocumentType.TechnicalInspection => FleetAlertType.ExpiringTechnicalInspection,
        VehicleDocumentType.TaxiLicense => FleetAlertType.ExpiringTaxiLicense,
        _ => FleetAlertType.DocumentExpiration
    };
}

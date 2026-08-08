using SmartTaxi.Application.Fleet.Alerts.Commands.GenerateFleetAlerts;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Alerts.Enums;
using SmartTaxi.Domain.Fleet.Contracts.Entities;
using SmartTaxi.Domain.Fleet.Contracts.Enums;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Drivers.Enums;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Documents.Enums;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.Application.Tests.Fleet.Alerts.Commands;

public class GenerateFleetAlertsCommandHandlerTests
{
    private readonly FakeVehicleRepository _vehicleRepository = new();
    private readonly FakeVehicleDocumentRepository _vehicleDocumentRepository = new();
    private readonly FakeDriverOwnerContractRepository _contractRepository = new();
    private readonly FakeDriverProfileRepository _driverRepository = new();
    private readonly FakeFleetAlertRepository _alertRepository = new();
    private readonly FakeDocumentExpirationPolicy _expirationPolicy = new();
    private readonly GenerateFleetAlertsCommandHandler _handler;

    public GenerateFleetAlertsCommandHandlerTests()
    {
        _handler = new GenerateFleetAlertsCommandHandler(
            _vehicleRepository, _vehicleDocumentRepository, _contractRepository, _driverRepository,
            _alertRepository, _expirationPolicy);
    }

    private static Vehicle RegisterVehicle(Guid ownerId, string plate) => Vehicle.Register(
        ownerId, null, "Toyota", "Corolla", 2022, "White", plate, null, 0,
        FuelType.Petrol, TransmissionType.Automatic, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);

    [Fact]
    public async Task Handle_ForSuspendedVehicle_CreatesOpenAlertOnce()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = RegisterVehicle(ownerId, "AA-111-BB");
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);
        typeof(Vehicle).GetProperty(nameof(Vehicle.OperationalStatus))!.SetValue(vehicle, VehicleOperationalStatus.Suspended);

        var firstRun = await _handler.Handle(new GenerateFleetAlertsCommand(ownerId), CancellationToken.None);
        var secondRun = await _handler.Handle(new GenerateFleetAlertsCommand(ownerId), CancellationToken.None);

        Assert.Equal(1, firstRun.Value);
        Assert.Equal(0, secondRun.Value);
        var alerts = await _alertRepository.GetForOwnerAsync(ownerId, FleetAlertStatus.Open, CancellationToken.None);
        Assert.Contains(alerts, a => a.AlertType == FleetAlertType.SuspendedVehicle && a.RelatedEntityId == vehicle.Id);
    }

    [Fact]
    public async Task Handle_ForVehicleWithExpiringInsurance_CreatesExpiringInsuranceAlert()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = RegisterVehicle(ownerId, "AA-222-CC");
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);

        var document = VehicleDocument.Upload(
            vehicle.Id, VehicleDocumentType.Insurance, "storage-ref", "insurance.pdf", "application/pdf", 1024,
            "sha256hash", DateTime.UtcNow.AddYears(-1), DateTime.UtcNow.AddDays(5), DateTime.UtcNow);
        await _vehicleDocumentRepository.AddAsync(document, CancellationToken.None);
        await _vehicleDocumentRepository.TryApproveAsync(document.Id, Guid.NewGuid(), DateTime.UtcNow, null, CancellationToken.None);

        var result = await _handler.Handle(new GenerateFleetAlertsCommand(ownerId), CancellationToken.None);

        Assert.Equal(1, result.Value);
        var alerts = await _alertRepository.GetForOwnerAsync(ownerId, FleetAlertStatus.Open, CancellationToken.None);
        Assert.Contains(alerts, a => a.AlertType == FleetAlertType.ExpiringInsurance && a.RelatedEntityId == vehicle.Id);
    }

    [Fact]
    public async Task Handle_ForSuspendedDriverUnderContract_CreatesSuspendedDriverAlert()
    {
        var ownerId = Guid.NewGuid();
        var driver = DriverProfile.Create(Guid.NewGuid(), "LIC1", DateTime.UtcNow.AddYears(1), null, true, DateTime.UtcNow);
        await _driverRepository.AddAsync(driver, CancellationToken.None);
        typeof(DriverProfile).GetProperty(nameof(DriverProfile.VerificationStatus))!
            .SetValue(driver, DriverVerificationStatus.Suspended);

        var contract = DriverOwnerContract.CreateDraft(
            ownerId, driver.Id, null, ContractType.FixedSalary, new DateOnly(2026, 1, 1), null,
            1000, null, null, PaymentFrequency.Monthly, null, DateTime.UtcNow);
        await _contractRepository.AddAsync(contract, CancellationToken.None);

        var result = await _handler.Handle(new GenerateFleetAlertsCommand(ownerId), CancellationToken.None);

        Assert.Equal(1, result.Value);
        var alerts = await _alertRepository.GetForOwnerAsync(ownerId, FleetAlertStatus.Open, CancellationToken.None);
        Assert.Contains(alerts, a => a.AlertType == FleetAlertType.SuspendedDriver && a.RelatedEntityId == driver.Id);
    }
}

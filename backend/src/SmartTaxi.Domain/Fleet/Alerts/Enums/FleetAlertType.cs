namespace SmartTaxi.Domain.Fleet.Alerts.Enums;

public enum FleetAlertType
{
    ExpiringInsurance,
    ExpiringTechnicalInspection,
    ExpiringTaxiLicense,
    OverdueMaintenance,
    HighMileage,
    SuspendedVehicle,
    SuspendedDriver,
    InvalidAssignment,
    UnusualExpense,
    DocumentExpiration
}

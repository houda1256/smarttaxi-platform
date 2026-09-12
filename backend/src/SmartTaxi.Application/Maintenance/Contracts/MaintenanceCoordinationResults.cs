namespace SmartTaxi.Application.Maintenance.Contracts;

/// <summary>Outcome of IMaintenanceWorkStartRepository.TryStartAsync's single atomic transaction (Module 9 mandatory atomicity design).</summary>
public enum MaintenanceWorkStartResult
{
    Started,

    /// <summary>MaintenanceRequest wasn't in VehicleReceived, or GarageUserId didn't match the caller — neither table was touched.</summary>
    RequestNotEligible,

    /// <summary>Vehicle.OperationalStatus wasn't Active (e.g. already Suspended) — neither table was touched, the whole transaction rolled back.</summary>
    VehicleNotEligible
}

/// <summary>Outcome of IMaintenanceCompletionRepository.TryCompleteAsync's single atomic transaction.</summary>
public enum MaintenanceCompletionResult
{
    Completed,

    /// <summary>MaintenanceRequest wasn't in InProgress/WaitingForParts, or GarageUserId didn't match the caller — nothing was touched.</summary>
    RequestNotEligible
}

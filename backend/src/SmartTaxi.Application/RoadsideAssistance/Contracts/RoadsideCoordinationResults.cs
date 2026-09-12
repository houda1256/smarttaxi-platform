namespace SmartTaxi.Application.RoadsideAssistance.Contracts;

/// <summary>Outcome of IRoadsideWorkStartRepository.TryStartAsync's single atomic transaction.</summary>
public enum RoadsideWorkStartResult
{
    Started,

    /// <summary>RoadsideAssistanceRequest wasn't in PartnerArrived, or PartnerUserId didn't match the caller — neither table was touched.</summary>
    RequestNotEligible,

    /// <summary>Vehicle.OperationalStatus wasn't Active (e.g. already Suspended/UnderMaintenance) — neither table was touched, the whole transaction rolled back.</summary>
    VehicleNotEligible
}

/// <summary>Outcome of IRoadsideCompletionRepository.TryCompleteAsync's single atomic transaction.</summary>
public enum RoadsideCompletionResult
{
    Completed,

    /// <summary>RoadsideAssistanceRequest wasn't in InProgress, or PartnerUserId didn't match the caller — nothing was touched.</summary>
    RequestNotEligible
}

/// <summary>Outcome of IRoadsideEscalationRepository.TryEscalateAsync's single atomic transaction.</summary>
public enum RoadsideEscalationOutcome
{
    Escalated,

    /// <summary>EscalatedMaintenanceRequestId was already set — idempotent no-op, the existing MaintenanceRequestId is returned.</summary>
    AlreadyEscalated,

    /// <summary>RoadsideAssistanceRequest wasn't Completed — nothing was touched.</summary>
    RequestNotEligible,

    /// <summary>Maintenance's own one-active-request-per-vehicle partial unique index rejected the insert — the whole transaction rolled back, EscalatedMaintenanceRequestId stays null.</summary>
    MaintenanceConflict
}

public sealed record RoadsideEscalationResult(RoadsideEscalationOutcome Outcome, Guid? MaintenanceRequestId);

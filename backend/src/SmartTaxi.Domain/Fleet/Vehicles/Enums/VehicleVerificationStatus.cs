namespace SmartTaxi.Domain.Fleet.Vehicles.Enums;

/// <summary>
/// The one-time platform verification gate — independent from, but kept in
/// lockstep at approve/reject time with, OperationalStatus (see Vehicle
/// entity remarks). Distinct dimension: OperationalStatus additionally
/// tracks day-to-day availability (maintenance, suspension, retirement)
/// that can change after verification without re-triggering this gate.
/// </summary>
public enum VehicleVerificationStatus
{
    Pending,
    Approved,
    Rejected
}

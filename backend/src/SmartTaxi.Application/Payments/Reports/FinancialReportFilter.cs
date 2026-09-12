namespace SmartTaxi.Application.Payments.Reports;

/// <summary>
/// ActorId matches CustomerId/DriverId/OwnerId on Payments, BeneficiaryAccountId's
/// owner on Payouts, and OwnerId on FleetExpenses/CashRegisters — "the actor"
/// generically, per the master prompt's filter list. VehicleId/FleetId narrow
/// FleetExpenses only, since Payments/Payouts/Ledger don't carry a VehicleId.
/// </summary>
public sealed record FinancialReportFilter(
    DateTime FromUtc,
    DateTime ToUtc,
    Guid? ActorId = null,
    Guid? VehicleId = null,
    Guid? FleetId = null,
    string? Service = null,
    string? PaymentMethod = null,
    string? Status = null);

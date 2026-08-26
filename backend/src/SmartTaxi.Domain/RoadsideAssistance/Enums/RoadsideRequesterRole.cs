namespace SmartTaxi.Domain.RoadsideAssistance.Enums;

/// <summary>
/// Deliberately a local, module-owned enum rather than a reference to
/// Identity.UserRole — Roadside Assistance only ever needs to distinguish
/// these two payer-eligible roles (see Finance settlement's account-type
/// resolution), and a plain Guid + this narrow enum avoids coupling to
/// Identity's full role set, matching the codebase-wide "plain Guid
/// references, no cross-module entity coupling" convention.
/// </summary>
public enum RoadsideRequesterRole
{
    TaxiOwner,
    Driver
}

namespace SmartTaxi.Domain.Payments.CashDeclarations.Enums;

/// <summary>The two operating models the master prompt validates — derived from the Driver's active DriverOwnerContract at submission time and stored for audit even if the contract later changes.</summary>
public enum CashDeclarationOperatingModel
{
    /// <summary>Model A: the Driver keeps the cash collected and owes the platform/owner their commission/share.</summary>
    DriverKeepsCashOwesShare,

    /// <summary>Model B: the Driver returns the cash to the TaxiOwner, who later pays the Driver's share separately.</summary>
    DriverRemitsCashToOwner
}

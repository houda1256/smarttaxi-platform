namespace SmartTaxi.Domain.Rides.Enums;

/// <summary>
/// The Customer's stated preference only — no real payment integration exists
/// yet. This is the clean, self-contained field the future Payments module
/// will read, not a payment-processing concept itself.
/// </summary>
public enum RidePaymentMethod
{
    Cash,
    Card,
    Wallet
}

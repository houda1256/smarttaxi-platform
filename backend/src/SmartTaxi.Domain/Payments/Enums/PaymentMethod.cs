namespace SmartTaxi.Domain.Payments.Enums;

/// <summary>
/// The actual settlement method for a specific Payment — distinct from
/// Ride.PreferredPaymentMethod (the Customer's stated preference at request
/// time, which may include Wallet). No online gateway exists yet, so Card
/// here only records that a card was used, without any real processing.
/// </summary>
public enum PaymentMethod
{
    Cash,
    Card,
    CashAtAgency
}

namespace SmartTaxi.Domain.Payments.Enums;

public enum PaymentStatus
{
    Pending,
    Authorized,
    Paid,
    Failed,
    Cancelled,
    Refunded,
    PartiallyRefunded
}

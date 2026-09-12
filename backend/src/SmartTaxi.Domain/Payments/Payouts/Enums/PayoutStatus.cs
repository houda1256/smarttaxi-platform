namespace SmartTaxi.Domain.Payments.Payouts.Enums;

public enum PayoutStatus
{
    Requested,
    PendingApproval,
    Approved,
    Processing,
    Paid,
    Failed,
    Rejected,
    Cancelled
}

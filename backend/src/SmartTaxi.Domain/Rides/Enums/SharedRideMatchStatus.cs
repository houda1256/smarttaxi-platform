namespace SmartTaxi.Domain.Rides.Enums;

public enum SharedRideMatchStatus
{
    Matching,
    WaitingForCustomerApprovals,
    WaitingForDriverApproval,
    Confirmed,
    Rejected,
    Expired,
    Cancelled
}

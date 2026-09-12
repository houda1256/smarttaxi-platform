namespace SmartTaxi.Domain.Rides.Enums;

public enum RideStatus
{
    Draft,
    Searching,
    DriversAvailable,
    DriverSelected,
    PendingDriverResponse,
    DriverAccepted,
    DriverRejected,
    DriverEnRoute,
    DriverArrived,
    PassengerOnBoard,
    InProgress,
    AwaitingPayment,
    Completed,
    CancelledByCustomer,
    CancelledByDriver,
    CancelledByAdmin,
    Expired,
    NoDriverAvailable,
    CustomerNoShow,
    DriverNoShow,
    Disputed
}

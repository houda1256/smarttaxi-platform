using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.API.Contracts.Rides;

/// <summary>
/// Grouped in one file rather than the usual one-file-per-DTO Fleet
/// convention — a documented, deliberate simplification given the Ride
/// module's exceptionally large endpoint surface (see the 4o final report).
/// </summary>
public sealed record CreateRideRequest(
    RideType RideType, string PickupAddress, double PickupLatitude, double PickupLongitude, string DestinationAddress,
    double DestinationLatitude, double DestinationLongitude, DateTime? ScheduledAt, int PassengerCount, int LuggageCount,
    bool NeedsAirConditioning, bool NeedsAccessibleVehicle, bool HasChildSeatRequest, bool HasPet,
    VehicleCategory? PreferredVehicleCategory, RidePaymentMethod PreferredPaymentMethod, string? SpecialInstructions);

public sealed record SelectDriverRequest(Guid DriverId, Guid VehicleId);

public sealed record DriverRejectRideRequest(string? Reason);

public sealed record ProposeFareRequest(decimal Amount);

public sealed record CancelRideByCustomerRequest(CustomerCancellationReason Reason, string? Details);

public sealed record CancelRideByDriverRequest(DriverCancellationReason Reason, string? Details);

public sealed record CancelRideByAdminRequest(string? Reason);

public sealed record UpdateDriverLocationRequest(double Latitude, double Longitude, double? Speed, double? Heading, double? Accuracy);

public sealed record UpdateDriverAvailabilityLocationRequest(double Latitude, double Longitude);

public sealed record ApproveSharedRideByDriverRequest(Guid VehicleId);

public sealed record SubmitRideRatingRequest(int Score, string? Comment, string? Tags);

public sealed record SubmitRideComplaintRequest(RideComplaintCategory Category, string Description);

public sealed record ResolveComplaintRequest(string Resolution);

public sealed record ActivateSosRequest(double Latitude, double Longitude, string Reason);

public sealed record SendRideMessageRequest(RideMessageType MessageType, string Content);

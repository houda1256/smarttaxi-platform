using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides;

public sealed record RideSummary(
    Guid Id,
    string RideNumber,
    Guid CustomerId,
    Guid? SelectedDriverId,
    Guid? VehicleId,
    RideType RideType,
    string PickupAddress,
    double PickupLatitude,
    double PickupLongitude,
    string DestinationAddress,
    double DestinationLatitude,
    double DestinationLongitude,
    DateTime RequestedAt,
    DateTime? ScheduledAt,
    int PassengerCount,
    int LuggageCount,
    bool NeedsAirConditioning,
    bool NeedsAccessibleVehicle,
    bool HasChildSeatRequest,
    bool HasPet,
    VehicleCategory? PreferredVehicleCategory,
    RidePaymentMethod PreferredPaymentMethod,
    string? SpecialInstructions,
    decimal? EstimatedDistanceKm,
    decimal? ActualDistanceKm,
    int? EstimatedDurationMinutes,
    int? ActualDurationMinutes,
    decimal? EstimatedFare,
    decimal? FinalFare,
    decimal? NegotiatedFinalFare,
    string Currency,
    RideStatus Status,
    double? LastKnownLatitude,
    double? LastKnownLongitude,
    DateTime? LastLocationRecordedAt,
    string? CancellationReason,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static RideSummary FromEntity(Ride ride) => new(
        ride.Id, ride.RideNumber, ride.CustomerId, ride.SelectedDriverId, ride.VehicleId, ride.RideType,
        ride.PickupAddress, ride.PickupLocation.Latitude, ride.PickupLocation.Longitude, ride.DestinationAddress,
        ride.DestinationLocation.Latitude, ride.DestinationLocation.Longitude, ride.RequestedAt, ride.ScheduledAt,
        ride.PassengerCount, ride.LuggageCount, ride.NeedsAirConditioning, ride.NeedsAccessibleVehicle,
        ride.HasChildSeatRequest, ride.HasPet, ride.PreferredVehicleCategory, ride.PreferredPaymentMethod,
        ride.SpecialInstructions, ride.EstimatedDistanceKm, ride.ActualDistanceKm, ride.EstimatedDurationMinutes,
        ride.ActualDurationMinutes, ride.EstimatedFare, ride.FinalFare, ride.NegotiatedFinalFare, ride.Currency, ride.Status,
        ride.LastKnownLatitude, ride.LastKnownLongitude, ride.LastLocationRecordedAt, ride.CancellationReason,
        ride.CreatedAt, ride.UpdatedAt);
}

namespace SmartTaxi.Application.Rides.Queries.GetPublicRideShareView;

/// <summary>
/// Deliberately narrow: no CustomerId/DriverId, no phone/email, no fare or
/// financial data — only what a trusted contact needs to follow the trip.
/// </summary>
public sealed record PublicRideShareView(
    string Status, double? Latitude, double? Longitude, string? DriverDisplayName, string? VehicleBrand,
    string? VehicleModel, string? VehicleLicensePlate, int? EstimatedArrivalMinutes);

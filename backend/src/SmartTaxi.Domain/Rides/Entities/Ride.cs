using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Domain.Rides.Events;
using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Domain.Rides.Entities;

/// <summary>
/// CustomerId/SelectedDriverId/VehicleId reference Identity/Fleet aggregates
/// by Guid only, matching the codebase-wide cross-module convention. Status
/// transitions are enforced as atomic repository-level guards (see
/// RideStatusTransitions for the full legal-transition graph), not domain
/// mutation methods — same pattern as every prior Fleet sub-slice, since
/// almost every Ride transition can be raced by two different actors.
/// </summary>
public sealed class Ride : AggregateRoot
{
    public string RideNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Guid? SelectedDriverId { get; private set; }
    public Guid? VehicleId { get; private set; }
    public RideType RideType { get; private set; }

    public string PickupAddress { get; private set; } = string.Empty;
    public GeoCoordinate PickupLocation { get; private set; } = null!;
    public string DestinationAddress { get; private set; } = string.Empty;
    public GeoCoordinate DestinationLocation { get; private set; } = null!;

    public DateTime RequestedAt { get; private set; }
    public DateTime? ScheduledAt { get; private set; }

    public int PassengerCount { get; private set; }
    public int LuggageCount { get; private set; }
    public bool NeedsAirConditioning { get; private set; }
    public bool NeedsAccessibleVehicle { get; private set; }
    public bool HasChildSeatRequest { get; private set; }
    public bool HasPet { get; private set; }
    public VehicleCategory? PreferredVehicleCategory { get; private set; }
    public RidePaymentMethod PreferredPaymentMethod { get; private set; }
    public string? SpecialInstructions { get; private set; }

    public decimal? EstimatedDistanceKm { get; private set; }
    public decimal? ActualDistanceKm { get; private set; }
    public int? EstimatedDurationMinutes { get; private set; }
    public int? ActualDurationMinutes { get; private set; }
    public decimal? EstimatedFare { get; private set; }
    public decimal? FinalFare { get; private set; }

    /// <summary>
    /// Set once a fare negotiation (RideType.Negotiated) is Accepted — from
    /// that point the price is immutable; CompleteRide uses this directly
    /// instead of recomputing via IFareCalculator when it is set.
    /// </summary>
    public decimal? NegotiatedFinalFare { get; private set; }

    public string Currency { get; private set; } = "TND";

    public RideStatus Status { get; private set; }

    public double? LastKnownLatitude { get; private set; }
    public double? LastKnownLongitude { get; private set; }
    public DateTime? LastLocationRecordedAt { get; private set; }

    public DateTime? DriverArrivedAt { get; private set; }
    public string? CancellationReason { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Ride()
    {
    }

    private Ride(
        Guid customerId, RideType rideType, string pickupAddress, GeoCoordinate pickupLocation,
        string destinationAddress, GeoCoordinate destinationLocation, DateTime? scheduledAt, int passengerCount,
        int luggageCount, bool needsAirConditioning, bool needsAccessibleVehicle, bool hasChildSeatRequest,
        bool hasPet, VehicleCategory? preferredVehicleCategory, RidePaymentMethod preferredPaymentMethod,
        string? specialInstructions, string currency, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        CustomerId = customerId;
        RideType = rideType;
        PickupAddress = pickupAddress;
        PickupLocation = pickupLocation;
        DestinationAddress = destinationAddress;
        DestinationLocation = destinationLocation;
        ScheduledAt = scheduledAt;
        PassengerCount = passengerCount;
        LuggageCount = luggageCount;
        NeedsAirConditioning = needsAirConditioning;
        NeedsAccessibleVehicle = needsAccessibleVehicle;
        HasChildSeatRequest = hasChildSeatRequest;
        HasPet = hasPet;
        PreferredVehicleCategory = preferredVehicleCategory;
        PreferredPaymentMethod = preferredPaymentMethod;
        SpecialInstructions = specialInstructions;
        Currency = currency;
        RideNumber = GenerateRideNumber(utcNow);
        Status = RideStatus.Draft;
        RequestedAt = utcNow;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;

        RaiseDomainEvent(new RideRequested(Id, customerId, utcNow));
    }

    public static Ride Create(
        Guid customerId, RideType rideType, string pickupAddress, GeoCoordinate pickupLocation,
        string destinationAddress, GeoCoordinate destinationLocation, DateTime? scheduledAt, int passengerCount,
        int luggageCount, bool needsAirConditioning, bool needsAccessibleVehicle, bool hasChildSeatRequest,
        bool hasPet, VehicleCategory? preferredVehicleCategory, RidePaymentMethod preferredPaymentMethod,
        string? specialInstructions, string currency, DateTime utcNow)
    {
        if (passengerCount <= 0)
        {
            throw new ArgumentException("Le nombre de passagers doit être positif.");
        }

        if (luggageCount < 0)
        {
            throw new ArgumentException("Le nombre de bagages ne peut pas être négatif.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new ArgumentException("La devise doit être un code ISO à 3 lettres.");
        }

        if (rideType == RideType.Scheduled)
        {
            if (scheduledAt is null)
            {
                throw new ArgumentException("Une date planifiée est requise pour une course planifiée.");
            }

            if (scheduledAt <= utcNow)
            {
                throw new ArgumentException("La date planifiée doit être dans le futur.");
            }
        }
        else if (scheduledAt is not null)
        {
            throw new ArgumentException("Seule une course planifiée peut avoir une date planifiée.");
        }

        return new Ride(
            customerId, rideType, pickupAddress, pickupLocation, destinationAddress, destinationLocation,
            scheduledAt, passengerCount, luggageCount, needsAirConditioning, needsAccessibleVehicle,
            hasChildSeatRequest, hasPet, preferredVehicleCategory, preferredPaymentMethod, specialInstructions,
            currency.Trim().ToUpperInvariant(), utcNow);
    }

    private static string GenerateRideNumber(DateTime utcNow) =>
        $"RD-{utcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}

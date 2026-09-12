using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Domain.Tests.Rides.Entities;

public class RideTests
{
    private static readonly GeoCoordinate Pickup = GeoCoordinate.Create(36.8065, 10.1815);
    private static readonly GeoCoordinate Destination = GeoCoordinate.Create(36.85, 10.2);

    [Fact]
    public void Create_Immediate_StartsInDraftWithNoScheduledAt()
    {
        var ride = Ride.Create(
            Guid.NewGuid(), RideType.Immediate, "Pickup St", Pickup, "Dest St", Destination, scheduledAt: null,
            passengerCount: 1, luggageCount: 0, needsAirConditioning: false, needsAccessibleVehicle: false,
            hasChildSeatRequest: false, hasPet: false, preferredVehicleCategory: null,
            preferredPaymentMethod: RidePaymentMethod.Cash, specialInstructions: null, currency: "TND",
            utcNow: DateTime.UtcNow);

        Assert.Equal(RideStatus.Draft, ride.Status);
        Assert.Null(ride.ScheduledAt);
        Assert.StartsWith("RD-", ride.RideNumber);
        Assert.Single(ride.DomainEvents);
    }

    [Fact]
    public void Create_Scheduled_InThePast_Throws()
    {
        Assert.Throws<ArgumentException>(() => Ride.Create(
            Guid.NewGuid(), RideType.Scheduled, "Pickup St", Pickup, "Dest St", Destination,
            scheduledAt: DateTime.UtcNow.AddMinutes(-5), passengerCount: 1, luggageCount: 0,
            needsAirConditioning: false, needsAccessibleVehicle: false, hasChildSeatRequest: false, hasPet: false,
            preferredVehicleCategory: null, preferredPaymentMethod: RidePaymentMethod.Cash,
            specialInstructions: null, currency: "TND", utcNow: DateTime.UtcNow));
    }

    [Fact]
    public void Create_Scheduled_WithoutScheduledAt_Throws()
    {
        Assert.Throws<ArgumentException>(() => Ride.Create(
            Guid.NewGuid(), RideType.Scheduled, "Pickup St", Pickup, "Dest St", Destination, scheduledAt: null,
            passengerCount: 1, luggageCount: 0, needsAirConditioning: false, needsAccessibleVehicle: false,
            hasChildSeatRequest: false, hasPet: false, preferredVehicleCategory: null,
            preferredPaymentMethod: RidePaymentMethod.Cash, specialInstructions: null, currency: "TND",
            utcNow: DateTime.UtcNow));
    }

    [Fact]
    public void Create_Immediate_WithScheduledAtProvided_Throws()
    {
        Assert.Throws<ArgumentException>(() => Ride.Create(
            Guid.NewGuid(), RideType.Immediate, "Pickup St", Pickup, "Dest St", Destination,
            scheduledAt: DateTime.UtcNow.AddHours(1), passengerCount: 1, luggageCount: 0,
            needsAirConditioning: false, needsAccessibleVehicle: false, hasChildSeatRequest: false, hasPet: false,
            preferredVehicleCategory: null, preferredPaymentMethod: RidePaymentMethod.Cash,
            specialInstructions: null, currency: "TND", utcNow: DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithZeroPassengers_Throws()
    {
        Assert.Throws<ArgumentException>(() => Ride.Create(
            Guid.NewGuid(), RideType.Immediate, "Pickup St", Pickup, "Dest St", Destination, scheduledAt: null,
            passengerCount: 0, luggageCount: 0, needsAirConditioning: false, needsAccessibleVehicle: false,
            hasChildSeatRequest: false, hasPet: false, preferredVehicleCategory: null,
            preferredPaymentMethod: RidePaymentMethod.Cash, specialInstructions: null, currency: "TND",
            utcNow: DateTime.UtcNow));
    }

    [Fact]
    public void Create_Scheduled_InTheFuture_Succeeds()
    {
        var ride = Ride.Create(
            Guid.NewGuid(), RideType.Scheduled, "Pickup St", Pickup, "Dest St", Destination,
            scheduledAt: DateTime.UtcNow.AddHours(2), passengerCount: 2, luggageCount: 1, needsAirConditioning: true,
            needsAccessibleVehicle: false, hasChildSeatRequest: false, hasPet: false, preferredVehicleCategory: null,
            preferredPaymentMethod: RidePaymentMethod.Card, specialInstructions: "Ring the bell", currency: "tnd",
            utcNow: DateTime.UtcNow);

        Assert.Equal(RideType.Scheduled, ride.RideType);
        Assert.NotNull(ride.ScheduledAt);
        Assert.Equal("TND", ride.Currency);
    }
}

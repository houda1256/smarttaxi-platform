using SmartTaxi.Application.Support;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Assignments.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Payments.Entities;
using SmartTaxi.Domain.Payments.Enums;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Domain.Rides.ValueObjects;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Tests.Support;

/// <summary>Proves the real composition logic against the existing per-module fakes — one representative case per class of check (ownership field, either-of-two-actors field, and the Vehicle owner-or-driver-assignment case), plus the not-found path.</summary>
public class SupportRelatedEntityValidatorTests
{
    private readonly FakeRideRepository _rideRepository = new();
    private readonly FakePaymentRepository _paymentRepository = new();
    private readonly FakeFinancialAccountRepository _financialAccountRepository = new();
    private readonly FakeFinancialLedgerRepository _financialLedgerRepository;
    private readonly FakePayoutRepository _payoutRepository;
    private readonly FakeFinancialDisputeRepository _financialDisputeRepository;
    private readonly FakeSubscriptionRepository _subscriptionRepository = new();
    private readonly FakeAdCampaignRepository _adCampaignRepository = new();
    private readonly FakeMaintenanceRequestRepository _maintenanceRequestRepository = new();
    private readonly FakeRoadsideAssistanceRequestRepository _roadsideAssistanceRequestRepository = new();
    private readonly FakeProfessionalAccountRequestRepository _professionalAccountRequestRepository = new();
    private readonly FakeVehicleRepository _vehicleRepository = new();
    private readonly FakeDriverVehicleAssignmentRepository _driverVehicleAssignmentRepository = new();
    private readonly SupportRelatedEntityValidator _validator;

    public SupportRelatedEntityValidatorTests()
    {
        _financialLedgerRepository = new FakeFinancialLedgerRepository(_financialAccountRepository);
        _payoutRepository = new FakePayoutRepository(_financialAccountRepository, _financialLedgerRepository);
        _financialDisputeRepository = new FakeFinancialDisputeRepository(_financialAccountRepository, _payoutRepository);
        _validator = new SupportRelatedEntityValidator(
            _rideRepository, _paymentRepository, _financialDisputeRepository, _subscriptionRepository, _adCampaignRepository,
            _maintenanceRequestRepository, _roadsideAssistanceRequestRepository, _professionalAccountRequestRepository, _vehicleRepository,
            _driverVehicleAssignmentRepository);
    }

    [Fact]
    public async Task Ride_OwnedByCustomer_IsValid()
    {
        var customerId = Guid.NewGuid();
        var ride = Ride.Create(
            customerId, RideType.Immediate, "A", GeoCoordinate.Create(36.8, 10.18), "B", GeoCoordinate.Create(36.9, 10.2), null, 1, 0, false,
            false, false, false, null, RidePaymentMethod.Cash, null, "TND", DateTime.UtcNow);
        await _rideRepository.AddAsync(ride, CancellationToken.None);

        var isValid = await _validator.IsValidReferenceAsync(SupportRelatedEntityType.Ride, ride.Id, customerId, CancellationToken.None);

        Assert.True(isValid);
    }

    [Fact]
    public async Task Ride_NotOwnedByCaller_IsInvalid()
    {
        var ride = Ride.Create(
            Guid.NewGuid(), RideType.Immediate, "A", GeoCoordinate.Create(36.8, 10.18), "B", GeoCoordinate.Create(36.9, 10.2), null, 1, 0, false,
            false, false, false, null, RidePaymentMethod.Cash, null, "TND", DateTime.UtcNow);
        await _rideRepository.AddAsync(ride, CancellationToken.None);

        var isValid = await _validator.IsValidReferenceAsync(SupportRelatedEntityType.Ride, ride.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.False(isValid);
    }

    [Fact]
    public async Task Payment_OwnedByCustomer_IsValid()
    {
        var customerId = Guid.NewGuid();
        var payment = Payment.Create(
            Guid.NewGuid(), "RD-20260101-AAAAAAAA", customerId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.Cash, null, 25m,
            "TND", DateTime.UtcNow);
        await _paymentRepository.TryAddAsync(payment, CancellationToken.None);

        var isValid = await _validator.IsValidReferenceAsync(SupportRelatedEntityType.Payment, payment.Id, customerId, CancellationToken.None);

        Assert.True(isValid);
    }

    [Fact]
    public async Task Vehicle_OwnedByCaller_IsValid()
    {
        var ownerId = Guid.NewGuid();
        var vehicle = Vehicle.Register(
            ownerId, null, "Toyota", "Corolla", 2022, "White", $"PLATE-{Guid.NewGuid():N}"[..12], null, 10000, FuelType.Petrol,
            TransmissionType.Manual, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);

        var isValid = await _validator.IsValidReferenceAsync(SupportRelatedEntityType.Vehicle, vehicle.Id, ownerId, CancellationToken.None);

        Assert.True(isValid);
    }

    [Fact]
    public async Task Vehicle_WithActiveDriverAssignment_IsValid()
    {
        var ownerId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var vehicle = Vehicle.Register(
            ownerId, null, "Toyota", "Corolla", 2022, "White", $"PLATE-{Guid.NewGuid():N}"[..12], null, 10000, FuelType.Petrol,
            TransmissionType.Manual, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);

        var assignment = DriverVehicleAssignment.CreateDraft(
            driverId, vehicle.Id, ownerId, DateOnly.FromDateTime(DateTime.UtcNow), null, null, null,
            Domain.Fleet.Assignments.Enums.DaysOfWeek.All, ownerId, DateTime.UtcNow);
        await _driverVehicleAssignmentRepository.AddAsync(assignment, CancellationToken.None);
        await _driverVehicleAssignmentRepository.TryApproveAsync(assignment.Id, DateTime.UtcNow, CancellationToken.None);
        await _driverVehicleAssignmentRepository.TryActivateAsync(assignment.Id, DateTime.UtcNow, CancellationToken.None);

        var isValid = await _validator.IsValidReferenceAsync(SupportRelatedEntityType.Vehicle, vehicle.Id, driverId, CancellationToken.None);

        Assert.True(isValid);
    }

    [Fact]
    public async Task Vehicle_WithoutOwnershipOrAssignment_IsInvalid()
    {
        var vehicle = Vehicle.Register(
            Guid.NewGuid(), null, "Toyota", "Corolla", 2022, "White", $"PLATE-{Guid.NewGuid():N}"[..12], null, 10000, FuelType.Petrol,
            TransmissionType.Manual, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);

        var isValid = await _validator.IsValidReferenceAsync(SupportRelatedEntityType.Vehicle, vehicle.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.False(isValid);
    }

    [Fact]
    public async Task UnknownEntityId_IsInvalid()
    {
        var isValid = await _validator.IsValidReferenceAsync(SupportRelatedEntityType.Ride, Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.False(isValid);
    }
}

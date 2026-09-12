using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Payments.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Payments.Commands.CreateRidePayment;

/// <summary>
/// Reuses Ride/Fleet repositories directly (cross-module reference by Guid,
/// same convention Ride used for Fleet) rather than duplicating Ride/Vehicle
/// data. Duplicate-payment prevention is two-layered: this app-level check,
/// backed by the DB partial unique index on Payment.RideId enforced by
/// IPaymentRepository.TryAddAsync.
/// </summary>
public sealed class CreateRidePaymentCommandHandler : ICommandHandler<CreateRidePaymentCommand, Result<Guid>>
{
    private const string NotFoundError = "Course introuvable.";
    private const string NotAwaitingPaymentError = "Cette course n'est pas en attente de paiement.";
    private const string NotParticipantError = "Seuls les participants à la course peuvent créer le paiement.";
    private const string NoFinalFareError = "La course n'a pas de tarif final déterminé.";
    private const string DuplicatePaymentError = "Un paiement existe déjà pour cette course.";
    private const string DriverNotFoundError = "Chauffeur introuvable pour cette course.";
    private const string VehicleNotFoundError = "Véhicule introuvable pour cette course.";

    private readonly IPaymentRepository _paymentRepository;
    private readonly IRideRepository _rideRepository;
    private readonly IDriverProfileRepository _driverRepository;
    private readonly IVehicleRepository _vehicleRepository;

    public CreateRidePaymentCommandHandler(
        IPaymentRepository paymentRepository, IRideRepository rideRepository, IDriverProfileRepository driverRepository,
        IVehicleRepository vehicleRepository)
    {
        _paymentRepository = paymentRepository;
        _rideRepository = rideRepository;
        _driverRepository = driverRepository;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<Result<Guid>> Handle(CreateRidePaymentCommand command, CancellationToken cancellationToken)
    {
        var ride = await _rideRepository.GetByIdAsync(command.RideId, cancellationToken);

        if (ride is null || ride.SelectedDriverId is null || ride.VehicleId is null)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var driver = await _driverRepository.GetByIdAsync(ride.SelectedDriverId.Value, cancellationToken);

        if (driver is null)
        {
            return Result<Guid>.Failure(DriverNotFoundError, ErrorType.NotFound);
        }

        if (command.RequestingUserId != ride.CustomerId && command.RequestingUserId != driver.UserId)
        {
            return Result<Guid>.Failure(NotParticipantError, ErrorType.Forbidden);
        }

        if (ride.Status != RideStatus.AwaitingPayment)
        {
            return Result<Guid>.Failure(NotAwaitingPaymentError, ErrorType.Conflict);
        }

        var existing = await _paymentRepository.GetActiveByRideIdAsync(ride.Id, cancellationToken);

        if (existing is not null)
        {
            return Result<Guid>.Failure(DuplicatePaymentError, ErrorType.Conflict);
        }

        var finalFareAmount = ride.NegotiatedFinalFare ?? ride.FinalFare;

        if (finalFareAmount is null)
        {
            return Result<Guid>.Failure(NoFinalFareError, ErrorType.Validation);
        }

        var vehicle = await _vehicleRepository.GetByIdAsync(ride.VehicleId.Value, cancellationToken);

        if (vehicle is null)
        {
            return Result<Guid>.Failure(VehicleNotFoundError, ErrorType.NotFound);
        }

        Payment payment;

        try
        {
            payment = Payment.Create(
                ride.Id, ride.RideNumber, ride.CustomerId, driver.Id, vehicle.Id, vehicle.OwnerId, command.PaymentMethod,
                ride.EstimatedFare, finalFareAmount.Value, ride.Currency, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        var added = await _paymentRepository.TryAddAsync(payment, cancellationToken);

        return added ? Result<Guid>.Success(payment.Id) : Result<Guid>.Failure(DuplicatePaymentError, ErrorType.Conflict);
    }
}

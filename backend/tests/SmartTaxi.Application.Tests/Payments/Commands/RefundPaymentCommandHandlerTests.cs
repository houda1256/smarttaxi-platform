using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments;
using SmartTaxi.Application.Payments.Commands.ConfirmPayment;
using SmartTaxi.Application.Payments.Commands.CreateRidePayment;
using SmartTaxi.Application.Payments.Commands.RefundPayment;
using SmartTaxi.Application.Payments.Ledger;
using SmartTaxi.Application.Rides.Commands.CompleteRide;
using SmartTaxi.Application.Rides.Commands.CreateRide;
using SmartTaxi.Application.Rides.Commands.DriverAcceptRide;
using SmartTaxi.Application.Rides.Commands.DriverArrived;
using SmartTaxi.Application.Rides.Commands.DriverEnRoute;
using SmartTaxi.Application.Rides.Commands.PassengerOnBoard;
using SmartTaxi.Application.Rides.Commands.SelectDriver;
using SmartTaxi.Application.Rides.Commands.StartRide;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Payments.Enums;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.Payments.Commands;

public class RefundPaymentCommandHandlerTests
{
    private readonly FakeRideRepository _rideRepository = new();
    private readonly FakeRideDriverRecommendationRepository _recommendationRepository = new();
    private readonly FakeDriverReservationHoldRepository _holdRepository = new();
    private readonly FakeDriverProfileRepository _driverRepository = new();
    private readonly FakeVehicleRepository _vehicleRepository = new();
    private readonly FakeDriverSearchPolicy _searchPolicy = new();
    private readonly FakeFareCalculator _fareCalculator = new();
    private readonly FakeDynamicPricingProvider _dynamicPricingProvider = new();
    private readonly FakePaymentRepository _paymentRepository = new();
    private readonly FakeDriverOwnerContractRepository _contractRepository = new();
    private readonly FakePlatformCommissionPolicy _commissionPolicy = new();
    private readonly FakeRefundRecordRepository _refundRecordRepository = new();
    private readonly FakeInvoiceRepository _invoiceRepository = new();
    private readonly FakeReceiptRepository _receiptRepository = new();
    private readonly FakeInvoicePdfGenerator _invoicePdfGenerator = new();
    private readonly FakeReceiptPdfGenerator _receiptPdfGenerator = new();
    private readonly FakeInvoiceTaxPolicy _taxPolicy = new();
    private readonly FakeFinancialAccountRepository _accountRepository = new();
    private readonly FakeFinancialLedgerRepository _ledgerRepository;

    private readonly CreateRideCommandHandler _createRideHandler;
    private readonly SelectDriverCommandHandler _selectDriverHandler;
    private readonly DriverAcceptRideCommandHandler _acceptHandler;
    private readonly DriverEnRouteCommandHandler _enRouteHandler;
    private readonly DriverArrivedCommandHandler _arrivedHandler;
    private readonly PassengerOnBoardCommandHandler _passengerOnBoardHandler;
    private readonly StartRideCommandHandler _startHandler;
    private readonly CompleteRideCommandHandler _completeRideHandler;
    private readonly CreateRidePaymentCommandHandler _createPaymentHandler;
    private readonly ConfirmPaymentCommandHandler _confirmHandler;
    private readonly RefundPaymentCommandHandler _refundHandler;

    public RefundPaymentCommandHandlerTests()
    {
        _ledgerRepository = new FakeFinancialLedgerRepository(_accountRepository);
        _createRideHandler = new CreateRideCommandHandler(_rideRepository);
        _selectDriverHandler = new SelectDriverCommandHandler(_rideRepository, _recommendationRepository, _holdRepository, _searchPolicy);
        _acceptHandler = new DriverAcceptRideCommandHandler(_rideRepository, _holdRepository, _driverRepository);
        _enRouteHandler = new DriverEnRouteCommandHandler(_rideRepository, _driverRepository);
        _arrivedHandler = new DriverArrivedCommandHandler(_rideRepository, _driverRepository);
        _passengerOnBoardHandler = new PassengerOnBoardCommandHandler(_rideRepository, _driverRepository);
        _startHandler = new StartRideCommandHandler(_rideRepository, _driverRepository);
        _completeRideHandler = new CompleteRideCommandHandler(_rideRepository, _driverRepository, _vehicleRepository, _fareCalculator, _dynamicPricingProvider, new FakeNotificationDispatcher());

        var revenueSharingCalculator = new RevenueSharingCalculator(_contractRepository, _commissionPolicy);
        var ledgerPostingService = new LedgerPostingService(_accountRepository, _ledgerRepository);
        _createPaymentHandler = new CreateRidePaymentCommandHandler(_paymentRepository, _rideRepository, _driverRepository, _vehicleRepository);
        _confirmHandler = new ConfirmPaymentCommandHandler(
            _paymentRepository, _driverRepository, revenueSharingCalculator, _rideRepository, _invoiceRepository,
            _receiptRepository, _invoicePdfGenerator, _receiptPdfGenerator, _taxPolicy, ledgerPostingService, new FakeNotificationDispatcher(), new FakeLoyaltyEarningDispatcher());
        _refundHandler = new RefundPaymentCommandHandler(_paymentRepository, _refundRecordRepository, ledgerPostingService);
    }

    private async Task<Guid> CreateConfirmedPaymentAsync()
    {
        var customerId = Guid.NewGuid();
        var createRideResult = await _createRideHandler.Handle(
            new CreateRideCommand(
                customerId, RideType.Immediate, "A", 36.8, 10.1, "B", 36.9, 10.2, null, 1, 0, false, false, false,
                false, null, RidePaymentMethod.Cash, null),
            CancellationToken.None);
        var rideId = createRideResult.Value;

        await _rideRepository.TryTransitionAsync(rideId, RideStatus.Draft, RideStatus.Searching, null, null, DateTime.UtcNow, CancellationToken.None);
        await _rideRepository.TryTransitionAsync(rideId, RideStatus.Searching, RideStatus.DriversAvailable, null, null, DateTime.UtcNow, CancellationToken.None);

        var driverUserId = Guid.NewGuid();
        var driver = DriverProfile.Create(driverUserId, "LIC1", DateTime.UtcNow.AddYears(1), null, true, DateTime.UtcNow);
        await _driverRepository.AddAsync(driver, CancellationToken.None);
        await _driverRepository.TrySubmitForReviewAsync(driver.Id, DateTime.UtcNow, CancellationToken.None);
        await _driverRepository.TryApproveAsync(driver.Id, DateTime.UtcNow, CancellationToken.None);

        var vehicle = Vehicle.Register(
            Guid.NewGuid(), null, "Toyota", "Corolla", 2022, "White", "AA-123-BB", null, 0, FuelType.Petrol,
            TransmissionType.Automatic, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);

        await _recommendationRepository.ReplaceForRideAsync(
            rideId,
            [new RideDriverRecommendation(rideId, driver.Id, vehicle.Id, 2.0m, 5, 40m, "Proche", 1, DateTime.UtcNow)],
            CancellationToken.None);

        await _selectDriverHandler.Handle(new SelectDriverCommand(customerId, rideId, driver.Id, vehicle.Id), CancellationToken.None);
        await _acceptHandler.Handle(new DriverAcceptRideCommand(driverUserId, rideId), CancellationToken.None);
        await _enRouteHandler.Handle(new DriverEnRouteCommand(driverUserId, rideId), CancellationToken.None);
        await _arrivedHandler.Handle(new DriverArrivedCommand(driverUserId, rideId), CancellationToken.None);
        await _passengerOnBoardHandler.Handle(new PassengerOnBoardCommand(driverUserId, rideId), CancellationToken.None);
        await _startHandler.Handle(new StartRideCommand(driverUserId, rideId), CancellationToken.None);
        await _completeRideHandler.Handle(new CompleteRideCommand(driverUserId, rideId, 8.5m, 15), CancellationToken.None);

        var createPaymentResult = await _createPaymentHandler.Handle(
            new CreateRidePaymentCommand(customerId, rideId, PaymentMethod.Cash), CancellationToken.None);
        await _confirmHandler.Handle(new ConfirmPaymentCommand(customerId, createPaymentResult.Value), CancellationToken.None);

        return createPaymentResult.Value;
    }

    [Fact]
    public async Task Refund_Full_SetsStatusToRefundedAndRecordsAudit()
    {
        var paymentId = await CreateConfirmedPaymentAsync();
        var adminId = Guid.NewGuid();

        var result = await _refundHandler.Handle(
            new RefundPaymentCommand(adminId, paymentId, RefundType.Full, null, "Erreur de facturation"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var payment = await _paymentRepository.GetByIdAsync(paymentId, CancellationToken.None);
        Assert.Equal(PaymentStatus.Refunded, payment!.Status);
        Assert.Equal(payment.FinalFareAmount, payment.RefundedAmount);

        var records = await _refundRecordRepository.GetForPaymentAsync(paymentId, CancellationToken.None);
        Assert.Single(records);
        Assert.Equal(RefundType.Full, records.Single().RefundType);
        Assert.Equal(adminId, records.Single().RequestedBy);
    }

    [Fact]
    public async Task Refund_Partial_SetsStatusToPartiallyRefunded()
    {
        var paymentId = await CreateConfirmedPaymentAsync();
        var payment = await _paymentRepository.GetByIdAsync(paymentId, CancellationToken.None);
        var partialAmount = Math.Round(payment!.FinalFareAmount / 2, 2);

        var result = await _refundHandler.Handle(
            new RefundPaymentCommand(Guid.NewGuid(), paymentId, RefundType.Partial, partialAmount, "Trajet plus court que prévu"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _paymentRepository.GetByIdAsync(paymentId, CancellationToken.None);
        Assert.Equal(PaymentStatus.PartiallyRefunded, reloaded!.Status);
        Assert.Equal(partialAmount, reloaded.RefundedAmount);
    }

    [Fact]
    public async Task Refund_PartialExceedingRemainingAmount_ReturnsValidationError()
    {
        var paymentId = await CreateConfirmedPaymentAsync();
        var payment = await _paymentRepository.GetByIdAsync(paymentId, CancellationToken.None);

        var result = await _refundHandler.Handle(
            new RefundPaymentCommand(Guid.NewGuid(), paymentId, RefundType.Partial, payment!.FinalFareAmount + 100m, "Test"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Refund_TwoPartialsSummingToFull_MovesToRefundedOnSecond()
    {
        var paymentId = await CreateConfirmedPaymentAsync();
        var payment = await _paymentRepository.GetByIdAsync(paymentId, CancellationToken.None);
        var half = Math.Round(payment!.FinalFareAmount / 2, 2);
        var remainder = payment.FinalFareAmount - half;

        await _refundHandler.Handle(new RefundPaymentCommand(Guid.NewGuid(), paymentId, RefundType.Partial, half, "Part 1"), CancellationToken.None);
        var second = await _refundHandler.Handle(
            new RefundPaymentCommand(Guid.NewGuid(), paymentId, RefundType.Partial, remainder, "Part 2"), CancellationToken.None);

        Assert.True(second.IsSuccess);
        var reloaded = await _paymentRepository.GetByIdAsync(paymentId, CancellationToken.None);
        Assert.Equal(PaymentStatus.Refunded, reloaded!.Status);
    }

    [Fact]
    public async Task Refund_OnUnknownPayment_ReturnsNotFound()
    {
        var result = await _refundHandler.Handle(
            new RefundPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), RefundType.Full, null, "Test"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Refund_OnPendingUnconfirmedPayment_ReturnsConflict()
    {
        var customerId = Guid.NewGuid();
        var createRideResult = await _createRideHandler.Handle(
            new CreateRideCommand(
                customerId, RideType.Immediate, "A", 36.8, 10.1, "B", 36.9, 10.2, null, 1, 0, false, false, false,
                false, null, RidePaymentMethod.Cash, null),
            CancellationToken.None);
        var rideId = createRideResult.Value;

        await _rideRepository.TryTransitionAsync(rideId, RideStatus.Draft, RideStatus.Searching, null, null, DateTime.UtcNow, CancellationToken.None);
        await _rideRepository.TryTransitionAsync(rideId, RideStatus.Searching, RideStatus.DriversAvailable, null, null, DateTime.UtcNow, CancellationToken.None);

        var driverUserId = Guid.NewGuid();
        var driver = DriverProfile.Create(driverUserId, "LIC1", DateTime.UtcNow.AddYears(1), null, true, DateTime.UtcNow);
        await _driverRepository.AddAsync(driver, CancellationToken.None);
        await _driverRepository.TrySubmitForReviewAsync(driver.Id, DateTime.UtcNow, CancellationToken.None);
        await _driverRepository.TryApproveAsync(driver.Id, DateTime.UtcNow, CancellationToken.None);

        var vehicle = Vehicle.Register(
            Guid.NewGuid(), null, "Toyota", "Corolla", 2022, "White", "CC-321-DD", null, 0, FuelType.Petrol,
            TransmissionType.Automatic, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);
        await _vehicleRepository.AddAsync(vehicle, CancellationToken.None);

        await _recommendationRepository.ReplaceForRideAsync(
            rideId,
            [new RideDriverRecommendation(rideId, driver.Id, vehicle.Id, 2.0m, 5, 40m, "Proche", 1, DateTime.UtcNow)],
            CancellationToken.None);

        await _selectDriverHandler.Handle(new SelectDriverCommand(customerId, rideId, driver.Id, vehicle.Id), CancellationToken.None);
        await _acceptHandler.Handle(new DriverAcceptRideCommand(driverUserId, rideId), CancellationToken.None);
        await _enRouteHandler.Handle(new DriverEnRouteCommand(driverUserId, rideId), CancellationToken.None);
        await _arrivedHandler.Handle(new DriverArrivedCommand(driverUserId, rideId), CancellationToken.None);
        await _passengerOnBoardHandler.Handle(new PassengerOnBoardCommand(driverUserId, rideId), CancellationToken.None);
        await _startHandler.Handle(new StartRideCommand(driverUserId, rideId), CancellationToken.None);
        await _completeRideHandler.Handle(new CompleteRideCommand(driverUserId, rideId, 8.5m, 15), CancellationToken.None);

        var createPaymentResult = await _createPaymentHandler.Handle(
            new CreateRidePaymentCommand(customerId, rideId, PaymentMethod.Cash), CancellationToken.None);

        var result = await _refundHandler.Handle(
            new RefundPaymentCommand(Guid.NewGuid(), createPaymentResult.Value, RefundType.Full, null, "Test"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}

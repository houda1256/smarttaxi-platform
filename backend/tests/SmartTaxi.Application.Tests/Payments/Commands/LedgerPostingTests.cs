using SmartTaxi.Application.Payments;
using SmartTaxi.Application.Payments.Commands.ConfirmPayment;
using SmartTaxi.Application.Payments.Commands.CreateRidePayment;
using SmartTaxi.Application.Payments.Commands.RefundPayment;
using SmartTaxi.Application.Payments.Ledger;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Enums;
using SmartTaxi.Domain.Payments.Ledger.Enums;
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
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.Payments.Commands;

/// <summary>
/// Covers the Phase 5B ledger requirements: confirmed payments post balanced
/// ledger entries, duplicate confirmation never double-posts, and a refund
/// posts its own traceable entry. Uses the real LedgerPostingService composed
/// from the Fake account/ledger repositories, exactly like production wiring.
/// </summary>
public class LedgerPostingTests
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

    public LedgerPostingTests()
    {
        _ledgerRepository = new FakeFinancialLedgerRepository(_accountRepository);
        _createRideHandler = new CreateRideCommandHandler(_rideRepository);
        _selectDriverHandler = new SelectDriverCommandHandler(_rideRepository, _recommendationRepository, _holdRepository, _searchPolicy);
        _acceptHandler = new DriverAcceptRideCommandHandler(_rideRepository, _holdRepository, _driverRepository);
        _enRouteHandler = new DriverEnRouteCommandHandler(_rideRepository, _driverRepository);
        _arrivedHandler = new DriverArrivedCommandHandler(_rideRepository, _driverRepository);
        _passengerOnBoardHandler = new PassengerOnBoardCommandHandler(_rideRepository, _driverRepository);
        _startHandler = new StartRideCommandHandler(_rideRepository, _driverRepository);
        _completeRideHandler = new CompleteRideCommandHandler(_rideRepository, _driverRepository, _vehicleRepository, _fareCalculator, _dynamicPricingProvider);

        var revenueSharingCalculator = new RevenueSharingCalculator(_contractRepository, _commissionPolicy);
        var ledgerPostingService = new LedgerPostingService(_accountRepository, _ledgerRepository);
        _createPaymentHandler = new CreateRidePaymentCommandHandler(_paymentRepository, _rideRepository, _driverRepository, _vehicleRepository);
        _confirmHandler = new ConfirmPaymentCommandHandler(
            _paymentRepository, _driverRepository, revenueSharingCalculator, _rideRepository, _invoiceRepository,
            _receiptRepository, _invoicePdfGenerator, _receiptPdfGenerator, _taxPolicy, ledgerPostingService);
        _refundHandler = new RefundPaymentCommandHandler(_paymentRepository, _refundRecordRepository, ledgerPostingService);
    }

    private async Task<(Guid PaymentId, Guid CustomerId, Guid DriverUserId)> CreateConfirmedPaymentAsync()
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
            Guid.NewGuid(), null, "Toyota", "Corolla", 2022, "White", $"AA-{Guid.NewGuid().ToString()[..4]}", null, 0, FuelType.Petrol,
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

        return (createPaymentResult.Value, customerId, driverUserId);
    }

    [Fact]
    public async Task ConfirmPayment_PostsBalancedLedgerEntries()
    {
        var (paymentId, _, driverUserId) = await CreateConfirmedPaymentAsync();
        var payment = await _paymentRepository.GetByIdAsync(paymentId, CancellationToken.None);

        var entries = await _ledgerRepository.GetForSourceAsync("Payment", paymentId, CancellationToken.None);

        // No active Driver-Owner contract exists in this setup, so the full remainder after
        // commission goes to the driver — only PaymentCollected/PlatformCommission/DriverEarning
        // are expected (OwnerEarning is skipped, per LedgerPostingService's ownerAmount>0 guard).
        Assert.Equal(3, entries.Count);
        Assert.Contains(entries, e => e.EntryType == LedgerEntryType.PaymentCollected);
        Assert.Contains(entries, e => e.EntryType == LedgerEntryType.PlatformCommission);
        Assert.Contains(entries, e => e.EntryType == LedgerEntryType.DriverEarning);
        Assert.All(entries, e => Assert.Equal("Payment", e.SourceType));
        Assert.All(entries, e => Assert.Equal(paymentId, e.SourceId));

        var platformAccount = await _accountRepository.GetByTypeAndOwnerAsync(FinancialAccountType.Platform, null, CancellationToken.None);
        var driverAccount = await _accountRepository.GetByTypeAndOwnerAsync(FinancialAccountType.Driver, driverUserId, CancellationToken.None);

        Assert.NotNull(platformAccount);
        Assert.NotNull(driverAccount);

        // PaymentCollected credits Platform.Pending by the full fare, then commission and
        // driver-earning both debit Platform.Pending back out — it must net to exactly 0.
        Assert.Equal(0m, platformAccount!.PendingBalance);
        Assert.True(platformAccount.AvailableBalance > 0);
        Assert.Equal(platformAccount.AvailableBalance, entries.Single(e => e.EntryType == LedgerEntryType.PlatformCommission).Amount);
        Assert.Equal(driverAccount!.AvailableBalance, entries.Single(e => e.EntryType == LedgerEntryType.DriverEarning).Amount);
        Assert.Equal(payment!.FinalFareAmount, platformAccount.AvailableBalance + driverAccount.AvailableBalance);
    }

    [Fact]
    public async Task ConfirmPayment_CalledTwice_DoesNotPostDuplicateLedgerEntries()
    {
        var (paymentId, customerId, _) = await CreateConfirmedPaymentAsync();
        var entriesBefore = await _ledgerRepository.GetForSourceAsync("Payment", paymentId, CancellationToken.None);

        await _confirmHandler.Handle(new ConfirmPaymentCommand(customerId, paymentId), CancellationToken.None);

        var entriesAfter = await _ledgerRepository.GetForSourceAsync("Payment", paymentId, CancellationToken.None);

        Assert.Equal(entriesBefore.Count, entriesAfter.Count);
    }

    [Fact]
    public async Task Refund_PostsRefundLedgerEntryAndReducesPlatformAvailableBalance()
    {
        var (paymentId, _, _) = await CreateConfirmedPaymentAsync();
        var platformAccount = await _accountRepository.GetByTypeAndOwnerAsync(FinancialAccountType.Platform, null, CancellationToken.None);
        var commissionEntry = (await _ledgerRepository.GetForSourceAsync("Payment", paymentId, CancellationToken.None))
            .Single(e => e.EntryType == LedgerEntryType.PlatformCommission);
        var availableBeforeRefund = platformAccount!.AvailableBalance;

        var refundResult = await _refundHandler.Handle(
            new RefundPaymentCommand(Guid.NewGuid(), paymentId, RefundType.Full, null, "Erreur de facturation"), CancellationToken.None);

        Assert.True(refundResult.IsSuccess);
        var refundEntries = await _ledgerRepository.GetForSourceAsync("Refund", refundResult.Value, CancellationToken.None);
        Assert.Single(refundEntries);
        Assert.Equal(LedgerEntryType.Refund, refundEntries.Single().EntryType);

        var platformAccountAfter = await _accountRepository.GetByIdAsync(platformAccount.Id, CancellationToken.None);
        Assert.Equal(availableBeforeRefund - refundEntries.Single().Amount, platformAccountAfter!.AvailableBalance);
        Assert.True(commissionEntry.Amount > 0);
    }
}

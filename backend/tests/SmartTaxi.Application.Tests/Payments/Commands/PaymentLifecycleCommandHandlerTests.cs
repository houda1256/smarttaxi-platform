using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.Commands.AuthorizePayment;
using SmartTaxi.Application.Payments.Commands.CancelPayment;
using SmartTaxi.Application.Payments.Commands.ConfirmPayment;
using SmartTaxi.Application.Payments.Commands.CreateRidePayment;
using SmartTaxi.Application.Payments.Commands.FailPayment;
using SmartTaxi.Application.Payments;
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
using SmartTaxi.Domain.Fleet.Contracts.Entities;
using SmartTaxi.Domain.Fleet.Contracts.Enums;
using SmartTaxi.Domain.Fleet.Drivers.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Payments.Enums;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.Payments.Commands;

public class PaymentLifecycleCommandHandlerTests
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
    private readonly AuthorizePaymentCommandHandler _authorizeHandler;
    private readonly ConfirmPaymentCommandHandler _confirmHandler;
    private readonly CancelPaymentCommandHandler _cancelHandler;
    private readonly FailPaymentCommandHandler _failHandler;
    private readonly RevenueSharingCalculator _revenueSharingCalculator;

    public PaymentLifecycleCommandHandlerTests()
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

        _revenueSharingCalculator = new RevenueSharingCalculator(_contractRepository, _commissionPolicy);
        _createPaymentHandler = new CreateRidePaymentCommandHandler(_paymentRepository, _rideRepository, _driverRepository, _vehicleRepository);
        _authorizeHandler = new AuthorizePaymentCommandHandler(_paymentRepository, _driverRepository);
        var ledgerPostingService = new LedgerPostingService(_accountRepository, _ledgerRepository);
        _confirmHandler = new ConfirmPaymentCommandHandler(
            _paymentRepository, _driverRepository, _revenueSharingCalculator, _rideRepository, _invoiceRepository,
            _receiptRepository, _invoicePdfGenerator, _receiptPdfGenerator, _taxPolicy, ledgerPostingService);
        _cancelHandler = new CancelPaymentCommandHandler(_paymentRepository, _driverRepository);
        _failHandler = new FailPaymentCommandHandler(_paymentRepository);
    }

    private async Task<(Guid CustomerId, Guid RideId, Guid DriverUserId, Guid OwnerId, Guid VehicleId)> CreateRideAwaitingPaymentAsync(
        bool independentDriver = false)
    {
        var customerId = Guid.NewGuid();
        var driverUserIdForOwnership = Guid.NewGuid();
        var ownerId = independentDriver ? driverUserIdForOwnership : Guid.NewGuid();
        var createResult = await _createRideHandler.Handle(
            new CreateRideCommand(
                customerId, RideType.Immediate, "A", 36.8, 10.1, "B", 36.9, 10.2, null, 1, 0, false, false, false,
                false, null, RidePaymentMethod.Cash, null),
            CancellationToken.None);
        var rideId = createResult.Value;

        await _rideRepository.TryTransitionAsync(rideId, RideStatus.Draft, RideStatus.Searching, null, null, DateTime.UtcNow, CancellationToken.None);
        await _rideRepository.TryTransitionAsync(rideId, RideStatus.Searching, RideStatus.DriversAvailable, null, null, DateTime.UtcNow, CancellationToken.None);

        var driverUserId = driverUserIdForOwnership;
        var driver = DriverProfile.Create(driverUserId, "LIC1", DateTime.UtcNow.AddYears(1), null, true, DateTime.UtcNow);
        await _driverRepository.AddAsync(driver, CancellationToken.None);
        await _driverRepository.TrySubmitForReviewAsync(driver.Id, DateTime.UtcNow, CancellationToken.None);
        await _driverRepository.TryApproveAsync(driver.Id, DateTime.UtcNow, CancellationToken.None);

        var vehicle = Vehicle.Register(
            ownerId, null, "Toyota", "Corolla", 2022, "White", "AA-123-BB", null, 0, FuelType.Petrol,
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

        return (customerId, rideId, driverUserId, ownerId, vehicle.Id);
    }

    [Fact]
    public async Task CreatePayment_ForAwaitingPaymentRide_Succeeds()
    {
        var (customerId, rideId, _, _, _) = await CreateRideAwaitingPaymentAsync();

        var result = await _createPaymentHandler.Handle(new CreateRidePaymentCommand(customerId, rideId, PaymentMethod.Cash), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var payment = await _paymentRepository.GetByIdAsync(result.Value, CancellationToken.None);
        Assert.Equal(PaymentStatus.Pending, payment!.Status);
        Assert.StartsWith("PAY-", payment.PaymentReference);
    }

    [Fact]
    public async Task CreatePayment_Duplicate_ReturnsConflict()
    {
        var (customerId, rideId, _, _, _) = await CreateRideAwaitingPaymentAsync();
        await _createPaymentHandler.Handle(new CreateRidePaymentCommand(customerId, rideId, PaymentMethod.Cash), CancellationToken.None);

        var result = await _createPaymentHandler.Handle(new CreateRidePaymentCommand(customerId, rideId, PaymentMethod.Cash), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task CreatePayment_ByNonParticipant_ReturnsForbidden()
    {
        var (_, rideId, _, _, _) = await CreateRideAwaitingPaymentAsync();

        var result = await _createPaymentHandler.Handle(new CreateRidePaymentCommand(Guid.NewGuid(), rideId, PaymentMethod.Cash), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task ConfirmPayment_IndependentDriver_CreditsDriverFullyMinusCommission()
    {
        var (customerId, rideId, _, _, _) = await CreateRideAwaitingPaymentAsync(independentDriver: true);

        var createResult = await _createPaymentHandler.Handle(new CreateRidePaymentCommand(customerId, rideId, PaymentMethod.Cash), CancellationToken.None);
        var confirmResult = await _confirmHandler.Handle(new ConfirmPaymentCommand(customerId, createResult.Value), CancellationToken.None);

        Assert.True(confirmResult.IsSuccess);
        var payment = await _paymentRepository.GetByIdAsync(createResult.Value, CancellationToken.None);
        var expectedCommission = Math.Round(payment!.FinalFareAmount * _commissionPolicy.CommissionPercentage / 100m, 2);
        Assert.Equal(expectedCommission, payment.PlatformCommissionAmount);
        Assert.Equal(payment.FinalFareAmount - expectedCommission, payment.DriverAmount);
        Assert.Equal(0m, payment.OwnerAmount);
    }

    [Fact]
    public async Task ConfirmPayment_WithPercentageContract_SplitsRevenueAccordingToContract()
    {
        var (customerId, rideId, driverUserId, ownerId, vehicleId) = await CreateRideAwaitingPaymentAsync();
        var driver = await _driverRepository.GetByUserIdAsync(driverUserId, CancellationToken.None);

        var contract = DriverOwnerContract.CreateDraft(
            ownerId, driver!.Id, vehicleId, ContractType.PercentagePerRide, DateOnly.FromDateTime(DateTime.UtcNow), null,
            null, 70m, 30m, PaymentFrequency.PerRide, null, DateTime.UtcNow);
        await _contractRepository.AddAsync(contract, CancellationToken.None);
        await _contractRepository.TrySubmitAsync(contract.Id, DateTime.UtcNow, CancellationToken.None);
        await _contractRepository.TryActivateAsync(contract.Id, DateTime.UtcNow, CancellationToken.None);

        var createResult = await _createPaymentHandler.Handle(new CreateRidePaymentCommand(customerId, rideId, PaymentMethod.Cash), CancellationToken.None);
        var paymentId = createResult.Value;

        var confirmResult = await _confirmHandler.Handle(new ConfirmPaymentCommand(customerId, paymentId), CancellationToken.None);

        Assert.True(confirmResult.IsSuccess);
        var payment = await _paymentRepository.GetByIdAsync(paymentId, CancellationToken.None);
        Assert.Equal(PaymentStatus.Paid, payment!.Status);
        Assert.NotNull(payment.PlatformCommissionAmount);

        var remainder = payment.FinalFareAmount - payment.PlatformCommissionAmount!.Value;
        Assert.Equal(Math.Round(remainder * 0.7m, 2), payment.DriverAmount);
        Assert.Equal(Math.Round(remainder * 0.3m, 2), payment.OwnerAmount);

        var ride = await _rideRepository.GetByIdAsync(rideId, CancellationToken.None);
        Assert.Equal(RideStatus.Completed, ride!.Status);
    }

    [Fact]
    public async Task ConfirmPayment_CalledTwice_IsIdempotent()
    {
        var (customerId, rideId, _, _, _) = await CreateRideAwaitingPaymentAsync();
        var createResult = await _createPaymentHandler.Handle(new CreateRidePaymentCommand(customerId, rideId, PaymentMethod.Cash), CancellationToken.None);

        var first = await _confirmHandler.Handle(new ConfirmPaymentCommand(customerId, createResult.Value), CancellationToken.None);
        var second = await _confirmHandler.Handle(new ConfirmPaymentCommand(customerId, createResult.Value), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value, second.Value);
    }

    [Fact]
    public async Task AuthorizeThenConfirm_Succeeds()
    {
        var (customerId, rideId, _, _, _) = await CreateRideAwaitingPaymentAsync();
        var createResult = await _createPaymentHandler.Handle(new CreateRidePaymentCommand(customerId, rideId, PaymentMethod.Card), CancellationToken.None);

        var authorizeResult = await _authorizeHandler.Handle(new AuthorizePaymentCommand(customerId, createResult.Value), CancellationToken.None);
        var confirmResult = await _confirmHandler.Handle(new ConfirmPaymentCommand(customerId, createResult.Value), CancellationToken.None);

        Assert.True(authorizeResult.IsSuccess);
        Assert.True(confirmResult.IsSuccess);
    }

    [Fact]
    public async Task CancelPayment_WhilePending_Succeeds()
    {
        var (customerId, rideId, _, _, _) = await CreateRideAwaitingPaymentAsync();
        var createResult = await _createPaymentHandler.Handle(new CreateRidePaymentCommand(customerId, rideId, PaymentMethod.Cash), CancellationToken.None);

        var result = await _cancelHandler.Handle(new CancelPaymentCommand(customerId, createResult.Value, "Changement d'avis"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var payment = await _paymentRepository.GetByIdAsync(createResult.Value, CancellationToken.None);
        Assert.Equal(PaymentStatus.Cancelled, payment!.Status);
    }

    [Fact]
    public async Task CancelPayment_AfterConfirmed_ReturnsConflict()
    {
        var (customerId, rideId, _, _, _) = await CreateRideAwaitingPaymentAsync();
        var createResult = await _createPaymentHandler.Handle(new CreateRidePaymentCommand(customerId, rideId, PaymentMethod.Cash), CancellationToken.None);
        await _confirmHandler.Handle(new ConfirmPaymentCommand(customerId, createResult.Value), CancellationToken.None);

        var result = await _cancelHandler.Handle(new CancelPaymentCommand(customerId, createResult.Value, "Trop tard"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task FailPayment_WhilePending_Succeeds()
    {
        var (customerId, rideId, _, _, _) = await CreateRideAwaitingPaymentAsync();
        var createResult = await _createPaymentHandler.Handle(new CreateRidePaymentCommand(customerId, rideId, PaymentMethod.Card), CancellationToken.None);

        var result = await _failHandler.Handle(new FailPaymentCommand(createResult.Value, "Carte refusée"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var payment = await _paymentRepository.GetByIdAsync(createResult.Value, CancellationToken.None);
        Assert.Equal(PaymentStatus.Failed, payment!.Status);
    }
}

using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments;
using SmartTaxi.Application.Payments.Commands.ConfirmPayment;
using SmartTaxi.Application.Payments.Commands.CreateRidePayment;
using SmartTaxi.Application.Payments.Ledger;
using SmartTaxi.Application.Payments.Queries.GetAdminPayments;
using SmartTaxi.Application.Payments.Queries.GetDriverRevenueReport;
using SmartTaxi.Application.Payments.Queries.GetInvoiceByPaymentId;
using SmartTaxi.Application.Payments.Queries.GetOwnerRevenueReport;
using SmartTaxi.Application.Payments.Queries.GetPaymentById;
using SmartTaxi.Application.Payments.Queries.GetPaymentStatistics;
using SmartTaxi.Application.Payments.Queries.GetReceiptByPaymentId;
using SmartTaxi.Application.Payments.Queries.GetRevenueSummary;
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

namespace SmartTaxi.Application.Tests.Payments.Queries;

public class PaymentQueryHandlerTests
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
    private readonly ConfirmPaymentCommandHandler _confirmHandler;

    private readonly GetPaymentByIdQueryHandler _getByIdHandler;
    private readonly GetAdminPaymentsQueryHandler _getAdminPaymentsHandler;
    private readonly GetRevenueSummaryQueryHandler _getRevenueSummaryHandler;
    private readonly GetDriverRevenueReportQueryHandler _getDriverRevenueHandler;
    private readonly GetOwnerRevenueReportQueryHandler _getOwnerRevenueHandler;
    private readonly GetPaymentStatisticsQueryHandler _getStatisticsHandler;
    private readonly GetInvoiceByPaymentIdQueryHandler _getInvoiceHandler;
    private readonly GetReceiptByPaymentIdQueryHandler _getReceiptHandler;

    public PaymentQueryHandlerTests()
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

        _getByIdHandler = new GetPaymentByIdQueryHandler(_paymentRepository, _driverRepository);
        _getAdminPaymentsHandler = new GetAdminPaymentsQueryHandler(_paymentRepository);
        _getRevenueSummaryHandler = new GetRevenueSummaryQueryHandler(_paymentRepository);
        _getDriverRevenueHandler = new GetDriverRevenueReportQueryHandler(_paymentRepository);
        _getOwnerRevenueHandler = new GetOwnerRevenueReportQueryHandler(_paymentRepository);
        _getStatisticsHandler = new GetPaymentStatisticsQueryHandler(_paymentRepository);
        _getInvoiceHandler = new GetInvoiceByPaymentIdQueryHandler(_invoiceRepository);
        _getReceiptHandler = new GetReceiptByPaymentIdQueryHandler(_receiptRepository);
    }

    private async Task<(Guid CustomerId, Guid PaymentId, Guid DriverProfileId, Guid OwnerId)> CreateConfirmedPaymentAsync()
    {
        var customerId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
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
            ownerId, null, "Toyota", "Corolla", 2022, "White", $"AA-{Guid.NewGuid().ToString()[..4]}", null, 0, FuelType.Petrol,
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

        return (customerId, createPaymentResult.Value, driver.Id, ownerId);
    }

    [Fact]
    public async Task GetPaymentById_ByParticipant_Succeeds()
    {
        var (customerId, paymentId, _, _) = await CreateConfirmedPaymentAsync();

        var result = await _getByIdHandler.Handle(new GetPaymentByIdQuery(customerId, paymentId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Paid, result.Value!.Status);
    }

    [Fact]
    public async Task GetPaymentById_ByNonParticipant_ReturnsForbidden()
    {
        var (_, paymentId, _, _) = await CreateConfirmedPaymentAsync();

        var result = await _getByIdHandler.Handle(new GetPaymentByIdQuery(Guid.NewGuid(), paymentId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task GetAdminPayments_FiltersByStatus()
    {
        await CreateConfirmedPaymentAsync();
        await CreateConfirmedPaymentAsync();

        var result = await _getAdminPaymentsHandler.Handle(
            new GetAdminPaymentsQuery(null, null, null, PaymentStatus.Paid, null, null, 1, 20), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, p => Assert.Equal(PaymentStatus.Paid, p.Status));
    }

    [Fact]
    public async Task GetRevenueSummary_Daily_AggregatesConfirmedPayments()
    {
        var (_, _, _, _) = await CreateConfirmedPaymentAsync();
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddDays(1);

        var result = await _getRevenueSummaryHandler.Handle(
            new GetRevenueSummaryQuery(from, to, RevenueReportGranularity.Daily), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(1, result.Single().PaymentCount);
        Assert.True(result.Single().TotalRevenue > 0);
    }

    [Fact]
    public async Task GetDriverRevenueReport_ReturnsDriverAmountTotal()
    {
        var (_, _, driverProfileId, _) = await CreateConfirmedPaymentAsync();
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddDays(1);

        var report = await _getDriverRevenueHandler.Handle(new GetDriverRevenueReportQuery(driverProfileId, from, to), CancellationToken.None);

        Assert.Equal(1, report.PaymentCount);
        Assert.True(report.TotalEarned > 0);
    }

    [Fact]
    public async Task GetOwnerRevenueReport_ReturnsOwnerAmountTotal()
    {
        var (_, _, _, ownerId) = await CreateConfirmedPaymentAsync();
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddDays(1);

        var report = await _getOwnerRevenueHandler.Handle(new GetOwnerRevenueReportQuery(ownerId, from, to), CancellationToken.None);

        Assert.Equal(1, report.PaymentCount);
    }

    [Fact]
    public async Task GetPaymentStatistics_CountsByStatus()
    {
        await CreateConfirmedPaymentAsync();
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow.AddDays(1);

        var stats = await _getStatisticsHandler.Handle(new GetPaymentStatisticsQuery(from, to), CancellationToken.None);

        Assert.Equal(1, stats.TotalPayments);
        Assert.Equal(1, stats.PaidCount);
        Assert.True(stats.TotalRevenue > 0);
        Assert.True(stats.AverageFare > 0);
    }

    [Fact]
    public async Task GetInvoiceByPaymentId_AfterConfirmation_ReturnsInvoice()
    {
        var (_, paymentId, _, _) = await CreateConfirmedPaymentAsync();

        var result = await _getInvoiceHandler.Handle(new GetInvoiceByPaymentIdQuery(paymentId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.StartsWith("INV-", result.Value!.InvoiceNumber);
    }

    [Fact]
    public async Task GetReceiptByPaymentId_AfterConfirmation_ReturnsReceipt()
    {
        var (_, paymentId, _, _) = await CreateConfirmedPaymentAsync();

        var result = await _getReceiptHandler.Handle(new GetReceiptByPaymentIdQuery(paymentId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.StartsWith("REC-", result.Value!.ReceiptNumber);
    }

    [Fact]
    public async Task GetInvoiceByPaymentId_ForUnknownPayment_ReturnsNotFound()
    {
        var result = await _getInvoiceHandler.Handle(new GetInvoiceByPaymentIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}

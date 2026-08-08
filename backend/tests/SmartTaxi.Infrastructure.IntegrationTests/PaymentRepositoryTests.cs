using Microsoft.EntityFrameworkCore;
using SmartTaxi.Domain.Payments.Entities;
using SmartTaxi.Domain.Payments.Enums;
using SmartTaxi.Infrastructure.Payments.Repositories;
using InvoiceRepository = SmartTaxi.Infrastructure.Payments.Repositories.InvoiceRepository;
using ReceiptRepository = SmartTaxi.Infrastructure.Payments.Repositories.ReceiptRepository;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves against a real PostgreSQL database that the Payments module's
/// atomic guards and unique constraints — the "no duplicate payment",
/// "idempotent payment confirmation", and "concurrency protection"
/// requirements — actually hold under real concurrent access.
/// </summary>
[Collection("SharedPostgres")]
public class PaymentRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public PaymentRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static Payment NewPayment(Guid rideId, decimal finalFare = 50m) => Payment.Create(
        rideId, "RD-20260101-ABCDEF12", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.Cash,
        null, finalFare, "TND", DateTime.UtcNow);

    [Fact]
    public async Task AddAndGetById_RoundTripsAllFields()
    {
        var payment = NewPayment(Guid.NewGuid());

        await using (var writeContext = _fixture.CreateContext())
        {
            var added = await new PaymentRepository(writeContext).TryAddAsync(payment, CancellationToken.None);
            Assert.True(added);
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new PaymentRepository(readContext).GetByIdAsync(payment.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(payment.PaymentReference, reloaded!.PaymentReference);
        Assert.Equal(PaymentStatus.Pending, reloaded.Status);
        Assert.Equal(50m, reloaded.FinalFareAmount);
    }

    [Fact]
    public async Task ConcurrentTryAddAsync_ForSameRide_OnlyOneSucceeds()
    {
        var rideId = Guid.NewGuid();
        var paymentA = NewPayment(rideId);
        var paymentB = NewPayment(rideId);

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new PaymentRepository(contextA).TryAddAsync(paymentA, CancellationToken.None),
            new PaymentRepository(contextB).TryAddAsync(paymentB, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var count = await readContext.Payments.CountAsync(p => p.RideId == rideId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ConcurrentTryConfirmAsync_OnlyOneSucceeds_AndWritesExactlyOneHistoryRow()
    {
        var payment = NewPayment(Guid.NewGuid());

        await using (var writeContext = _fixture.CreateContext())
        {
            await new PaymentRepository(writeContext).TryAddAsync(payment, CancellationToken.None);
        }

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();
        var utcNow = DateTime.UtcNow;

        var results = await Task.WhenAll(
            new PaymentRepository(contextA).TryConfirmAsync(payment.Id, 5m, 40m, 5m, utcNow, CancellationToken.None),
            new PaymentRepository(contextB).TryConfirmAsync(payment.Id, 5m, 40m, 5m, utcNow, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new PaymentRepository(readContext).GetByIdAsync(payment.Id, CancellationToken.None);
        Assert.Equal(PaymentStatus.Paid, reloaded!.Status);

        var historyCount = await readContext.PaymentTransactionHistories.CountAsync(h => h.PaymentId == payment.Id);
        Assert.Equal(1, historyCount);
    }

    [Fact]
    public async Task ConcurrentTryApplyRefundAsync_TogetherExceedingFinalFare_OnlyOneSucceeds()
    {
        var payment = NewPayment(Guid.NewGuid(), finalFare: 100m);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new PaymentRepository(writeContext).TryAddAsync(payment, CancellationToken.None);
            await new PaymentRepository(writeContext).TryConfirmAsync(payment.Id, 0m, 100m, 0m, DateTime.UtcNow, CancellationToken.None);
        }

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();
        var utcNow = DateTime.UtcNow;

        // Two concurrent 60-unit refunds against a 100-unit Payment: only one may succeed, since together they would exceed the final fare.
        var results = await Task.WhenAll(
            new PaymentRepository(contextA).TryApplyRefundAsync(payment.Id, 60m, PaymentStatus.PartiallyRefunded, utcNow, CancellationToken.None),
            new PaymentRepository(contextB).TryApplyRefundAsync(payment.Id, 60m, PaymentStatus.PartiallyRefunded, utcNow, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new PaymentRepository(readContext).GetByIdAsync(payment.Id, CancellationToken.None);
        Assert.Equal(60m, reloaded!.RefundedAmount);
    }

    [Fact]
    public async Task TwoSequentialPartialRefunds_SummingExactlyToFinalFare_MovesToFullyRefunded()
    {
        var payment = NewPayment(Guid.NewGuid(), finalFare: 100m);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new PaymentRepository(writeContext).TryAddAsync(payment, CancellationToken.None);
            await new PaymentRepository(writeContext).TryConfirmAsync(payment.Id, 0m, 100m, 0m, DateTime.UtcNow, CancellationToken.None);
        }

        await using (var firstContext = _fixture.CreateContext())
        {
            var firstApplied = await new PaymentRepository(firstContext)
                .TryApplyRefundAsync(payment.Id, 40m, PaymentStatus.PartiallyRefunded, DateTime.UtcNow, CancellationToken.None);
            Assert.True(firstApplied);
        }

        await using (var secondContext = _fixture.CreateContext())
        {
            var secondApplied = await new PaymentRepository(secondContext)
                .TryApplyRefundAsync(payment.Id, 60m, PaymentStatus.Refunded, DateTime.UtcNow, CancellationToken.None);
            Assert.True(secondApplied);
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new PaymentRepository(readContext).GetByIdAsync(payment.Id, CancellationToken.None);
        Assert.Equal(PaymentStatus.Refunded, reloaded!.Status);
        Assert.Equal(100m, reloaded.RefundedAmount);
    }

    [Fact]
    public async Task Invoice_And_Receipt_RoundTripAndEnforceUniquePaymentId()
    {
        var payment = NewPayment(Guid.NewGuid());

        await using (var writeContext = _fixture.CreateContext())
        {
            await new PaymentRepository(writeContext).TryAddAsync(payment, CancellationToken.None);
        }

        var invoice = Invoice.Generate(payment.Id, payment.RideNumber, payment.CustomerId, payment.DriverId, payment.OwnerId, 45m, 5m, "TND", PaymentMethod.Cash, DateTime.UtcNow);
        var receipt = Receipt.Issue(payment.Id, 50m, "TND", DateTime.UtcNow);

        await using (var writeContext2 = _fixture.CreateContext())
        {
            await new Infrastructure.Payments.Repositories.InvoiceRepository(writeContext2).AddAsync(invoice, CancellationToken.None);
            await new Infrastructure.Payments.Repositories.ReceiptRepository(writeContext2).AddAsync(receipt, CancellationToken.None);
        }

        var duplicateInvoice = Invoice.Generate(payment.Id, payment.RideNumber, payment.CustomerId, payment.DriverId, payment.OwnerId, 45m, 5m, "TND", PaymentMethod.Cash, DateTime.UtcNow);

        await using var duplicateContext = _fixture.CreateContext();
        await Assert.ThrowsAsync<DbUpdateException>(
            () => new Infrastructure.Payments.Repositories.InvoiceRepository(duplicateContext).AddAsync(duplicateInvoice, CancellationToken.None));
    }
}

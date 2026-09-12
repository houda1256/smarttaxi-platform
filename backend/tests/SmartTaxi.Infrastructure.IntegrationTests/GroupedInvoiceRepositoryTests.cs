using Microsoft.EntityFrameworkCore;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Enums;
using SmartTaxi.Infrastructure.Payments.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves against a real PostgreSQL database that the "a Ride cannot appear in
/// two grouped invoices" guarantee — a unique index on GroupedInvoiceLines.RideId —
/// actually holds, including under concurrency, and that invoice status
/// transitions are correctly guarded.
/// </summary>
[Collection("SharedPostgres")]
public class GroupedInvoiceRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public GroupedInvoiceRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static GroupedInvoice NewInvoice(Guid businessCustomerId, DateTime issueDate) => GroupedInvoice.Generate(
        businessCustomerId, GroupedInvoicePeriodType.Weekly, DateOnly.FromDateTime(issueDate), DateOnly.FromDateTime(issueDate.AddDays(6)),
        100m, 10m, "TND", issueDate, issueDate.AddDays(15));

    [Fact]
    public async Task AddAndGetById_RoundTripsAllFields()
    {
        var invoice = NewInvoice(Guid.NewGuid(), DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new GroupedInvoiceRepository(writeContext).AddAsync(invoice, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new GroupedInvoiceRepository(readContext).GetByIdAsync(invoice.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(GroupedInvoiceStatus.Issued, reloaded!.Status);
        Assert.Equal(110m, reloaded.TotalAmount);
    }

    [Fact]
    public async Task TryAddRangeAsync_ForRideAlreadyInvoiced_RejectsTheWholeBatch()
    {
        var businessCustomerId = Guid.NewGuid();
        var sharedRideId = Guid.NewGuid();
        var firstInvoice = NewInvoice(businessCustomerId, DateTime.UtcNow);
        var secondInvoice = NewInvoice(businessCustomerId, DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            var invoiceRepository = new GroupedInvoiceRepository(writeContext);
            await invoiceRepository.AddAsync(firstInvoice, CancellationToken.None);
            await invoiceRepository.AddAsync(secondInvoice, CancellationToken.None);

            var lineRepository = new GroupedInvoiceLineRepository(writeContext);
            var firstAdded = await lineRepository.TryAddRangeAsync(
                [new GroupedInvoiceLine(firstInvoice.Id, sharedRideId, "RD-001", 50m, DateTime.UtcNow)], CancellationToken.None);
            Assert.True(firstAdded);
        }

        await using (var conflictContext = _fixture.CreateContext())
        {
            var lineRepository = new GroupedInvoiceLineRepository(conflictContext);

            // Same Ride, different (second) invoice, plus one never-before-seen Ride — the whole batch must
            // be rejected, not just the conflicting line.
            var secondAdded = await lineRepository.TryAddRangeAsync(
                [
                    new GroupedInvoiceLine(secondInvoice.Id, sharedRideId, "RD-001", 50m, DateTime.UtcNow),
                    new GroupedInvoiceLine(secondInvoice.Id, Guid.NewGuid(), "RD-002", 30m, DateTime.UtcNow)
                ],
                CancellationToken.None);
            Assert.False(secondAdded);
        }

        await using var readContext = _fixture.CreateContext();
        var linesForSecondInvoice = await new GroupedInvoiceLineRepository(readContext).GetForInvoiceAsync(secondInvoice.Id, CancellationToken.None);
        Assert.Empty(linesForSecondInvoice);
    }

    [Fact]
    public async Task ConcurrentTryAddRangeAsync_ForSameRide_OnlyOneSucceeds()
    {
        var invoiceA = NewInvoice(Guid.NewGuid(), DateTime.UtcNow);
        var invoiceB = NewInvoice(Guid.NewGuid(), DateTime.UtcNow);
        var sharedRideId = Guid.NewGuid();

        await using (var writeContext = _fixture.CreateContext())
        {
            var invoiceRepository = new GroupedInvoiceRepository(writeContext);
            await invoiceRepository.AddAsync(invoiceA, CancellationToken.None);
            await invoiceRepository.AddAsync(invoiceB, CancellationToken.None);
        }

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new GroupedInvoiceLineRepository(contextA).TryAddRangeAsync(
                [new GroupedInvoiceLine(invoiceA.Id, sharedRideId, "RD-100", 20m, DateTime.UtcNow)], CancellationToken.None),
            new GroupedInvoiceLineRepository(contextB).TryAddRangeAsync(
                [new GroupedInvoiceLine(invoiceB.Id, sharedRideId, "RD-100", 20m, DateTime.UtcNow)], CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var count = await readContext.GroupedInvoiceLines.CountAsync(l => l.RideId == sharedRideId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AnyAlreadyInvoicedAsync_DetectsExistingRideAmongMultipleCandidates()
    {
        var invoice = NewInvoice(Guid.NewGuid(), DateTime.UtcNow);
        var invoicedRideId = Guid.NewGuid();

        await using (var writeContext = _fixture.CreateContext())
        {
            await new GroupedInvoiceRepository(writeContext).AddAsync(invoice, CancellationToken.None);
            await new GroupedInvoiceLineRepository(writeContext).TryAddRangeAsync(
                [new GroupedInvoiceLine(invoice.Id, invoicedRideId, "RD-200", 40m, DateTime.UtcNow)], CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var anyInvoiced = await new GroupedInvoiceLineRepository(readContext)
            .AnyAlreadyInvoicedAsync([Guid.NewGuid(), invoicedRideId, Guid.NewGuid()], CancellationToken.None);

        Assert.True(anyInvoiced);
    }

    [Fact]
    public async Task MarkPaidThenCancel_OnlyFirstTransitionSucceeds()
    {
        var invoice = NewInvoice(Guid.NewGuid(), DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new GroupedInvoiceRepository(writeContext).AddAsync(invoice, CancellationToken.None);
        }

        await using (var paidContext = _fixture.CreateContext())
        {
            var markedPaid = await new GroupedInvoiceRepository(paidContext).TryMarkPaidAsync(invoice.Id, DateTime.UtcNow, CancellationToken.None);
            Assert.True(markedPaid);
        }

        await using (var cancelContext = _fixture.CreateContext())
        {
            var cancelled = await new GroupedInvoiceRepository(cancelContext).TryCancelAsync(invoice.Id, DateTime.UtcNow, CancellationToken.None);
            Assert.False(cancelled);
        }
    }

    [Fact]
    public async Task GetOverdueAsync_ReturnsOnlyIssuedInvoicesPastTheirDueDate()
    {
        var businessCustomerId = Guid.NewGuid();
        var issueDate = DateTime.UtcNow.AddDays(-30);
        var overdueInvoice = GroupedInvoice.Generate(
            businessCustomerId, GroupedInvoicePeriodType.Monthly, DateOnly.FromDateTime(issueDate), DateOnly.FromDateTime(issueDate.AddDays(29)),
            200m, 20m, "TND", issueDate, issueDate.AddDays(5));
        var notYetDueInvoice = NewInvoice(businessCustomerId, DateTime.UtcNow);

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new GroupedInvoiceRepository(writeContext);
            await repository.AddAsync(overdueInvoice, CancellationToken.None);
            await repository.AddAsync(notYetDueInvoice, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var overdue = await new GroupedInvoiceRepository(readContext).GetOverdueAsync(DateTime.UtcNow, CancellationToken.None);

        Assert.Contains(overdue, i => i.Id == overdueInvoice.Id);
        Assert.DoesNotContain(overdue, i => i.Id == notYetDueInvoice.Id);
    }
}

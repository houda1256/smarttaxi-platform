using Microsoft.EntityFrameworkCore;
using SmartTaxi.Domain.Payments.CashDeclarations.Entities;
using SmartTaxi.Domain.Payments.CashDeclarations.Enums;
using SmartTaxi.Infrastructure.Payments.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

/// <summary>
/// Proves against a real PostgreSQL database that CashDeclarationRepository's
/// atomic status guards match the exact from-state rules the Application layer
/// depends on (e.g. approve/dispute are valid from Submitted OR UnderReview;
/// settle is valid only from Approved OR Disputed).
/// </summary>
[Collection("SharedPostgres")]
public class CashDeclarationRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public CashDeclarationRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static CashDeclaration NewDeclaration(Guid driverId, decimal expected = 100m, decimal declared = 100m) =>
        CashDeclaration.Submit(
            driverId, null, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 7), expected, declared,
            CashDeclarationOperatingModel.DriverKeepsCashOwesShare, DateTime.UtcNow);

    [Fact]
    public async Task AddAndGetById_RoundTripsAllFields()
    {
        var driverId = Guid.NewGuid();
        var declaration = NewDeclaration(driverId, 150m, 130m);

        await using (var writeContext = _fixture.CreateContext())
        {
            await new CashDeclarationRepository(writeContext).AddAsync(declaration, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await new CashDeclarationRepository(readContext).GetByIdAsync(declaration.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(CashDeclarationStatus.Submitted, reloaded!.Status);
        Assert.Equal(-20m, reloaded.Difference);
    }

    [Fact]
    public async Task ApproveDirectlyFromSubmitted_SkippingReview_Succeeds()
    {
        var declaration = NewDeclaration(Guid.NewGuid());

        await using (var writeContext = _fixture.CreateContext())
        {
            await new CashDeclarationRepository(writeContext).AddAsync(declaration, CancellationToken.None);
        }

        await using var approveContext = _fixture.CreateContext();
        var approved = await new CashDeclarationRepository(approveContext)
            .TryApproveAsync(declaration.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);

        Assert.True(approved);
    }

    [Fact]
    public async Task SettleBeforeApprovalOrDispute_Fails()
    {
        var declaration = NewDeclaration(Guid.NewGuid());

        await using (var writeContext = _fixture.CreateContext())
        {
            await new CashDeclarationRepository(writeContext).AddAsync(declaration, CancellationToken.None);
        }

        await using var settleContext = _fixture.CreateContext();
        var settled = await new CashDeclarationRepository(settleContext).TrySettleAsync(declaration.Id, DateTime.UtcNow, CancellationToken.None);

        Assert.False(settled);
    }

    [Fact]
    public async Task FullLifecycle_SubmittedToUnderReviewToDisputedToSettled_TransitionsCorrectly()
    {
        var declaration = NewDeclaration(Guid.NewGuid(), expected: 200m, declared: 150m);

        await using (var addContext = _fixture.CreateContext())
        {
            await new CashDeclarationRepository(addContext).AddAsync(declaration, CancellationToken.None);
        }

        await using (var reviewContext = _fixture.CreateContext())
        {
            Assert.True(await new CashDeclarationRepository(reviewContext).TryStartReviewAsync(declaration.Id, DateTime.UtcNow, CancellationToken.None));
        }

        await using (var disputeContext = _fixture.CreateContext())
        {
            Assert.True(await new CashDeclarationRepository(disputeContext)
                .TryDisputeAsync(declaration.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None));
        }

        await using (var settleContext = _fixture.CreateContext())
        {
            Assert.True(await new CashDeclarationRepository(settleContext).TrySettleAsync(declaration.Id, DateTime.UtcNow, CancellationToken.None));
        }

        await using var readContext = _fixture.CreateContext();
        var reloaded = await readContext.CashDeclarations.SingleAsync(d => d.Id == declaration.Id);
        Assert.Equal(CashDeclarationStatus.Settled, reloaded.Status);
    }

    [Fact]
    public async Task ConcurrentApproveAndDispute_OnlyOneSucceeds()
    {
        var declaration = NewDeclaration(Guid.NewGuid());

        await using (var addContext = _fixture.CreateContext())
        {
            await new CashDeclarationRepository(addContext).AddAsync(declaration, CancellationToken.None);
        }

        await using var approveContext = _fixture.CreateContext();
        await using var disputeContext = _fixture.CreateContext();

        var results = await Task.WhenAll(
            new CashDeclarationRepository(approveContext).TryApproveAsync(declaration.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None),
            new CashDeclarationRepository(disputeContext).TryDisputeAsync(declaration.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None));

        Assert.Single(results, succeeded => succeeded);
    }

    [Fact]
    public async Task GetForDriverAsync_FiltersByStatus()
    {
        var driverId = Guid.NewGuid();
        var declarationOne = NewDeclaration(driverId);
        var declarationTwo = NewDeclaration(driverId);

        await using (var writeContext = _fixture.CreateContext())
        {
            var repository = new CashDeclarationRepository(writeContext);
            await repository.AddAsync(declarationOne, CancellationToken.None);
            await repository.AddAsync(declarationTwo, CancellationToken.None);
        }

        await using (var approveContext = _fixture.CreateContext())
        {
            await new CashDeclarationRepository(approveContext).TryApproveAsync(declarationOne.Id, Guid.NewGuid(), DateTime.UtcNow, CancellationToken.None);
        }

        await using var readContext = _fixture.CreateContext();
        var submittedOnly = await new CashDeclarationRepository(readContext)
            .GetForDriverAsync(driverId, CashDeclarationStatus.Submitted, 1, 10, CancellationToken.None);

        Assert.Equal(1, submittedOnly.TotalCount);
        Assert.Equal(declarationTwo.Id, submittedOnly.Items.Single().Id);
    }
}

using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Advertising.Contracts;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Infrastructure.Advertising;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Advertising.Repositories;

/// <summary>
/// TryRecordAsync bundles the idempotency check, the campaign's atomic budget
/// consumption (via AdvertisingBudgetConsumer — includes the Active-status
/// guard), and the impression insert into one transaction — two concurrent
/// duplicate-keyed impressions can never both consume budget, and a
/// budget-exhausted or non-Active campaign can never accept one.
/// </summary>
internal sealed class AdvertisingImpressionRepository : IAdvertisingImpressionRepository
{
    private readonly ApplicationDbContext _context;

    public AdvertisingImpressionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<AdvertisingImpression?> GetByIdAsync(Guid impressionId, CancellationToken cancellationToken) =>
        _context.AdvertisingImpressions.FirstOrDefaultAsync(impression => impression.Id == impressionId, cancellationToken);

    public Task<int> CountForCampaignAsync(Guid campaignId, CancellationToken cancellationToken) =>
        _context.AdvertisingImpressions.CountAsync(impression => impression.CampaignId == campaignId, cancellationToken);

    public async Task<AdvertisingFactRecordResult> TryRecordAsync(
        Guid campaignId, Guid placementId, string idempotencyKey, decimal operationalCost, DateTime occurredAtUtc, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var existing = await _context.AdvertisingImpressions
            .FirstOrDefaultAsync(impression => impression.IdempotencyKey == idempotencyKey, cancellationToken);

        if (existing is not null)
        {
            return new AdvertisingFactRecordResult(AdvertisingFactOutcome.Replayed, existing.Id);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var budgetOutcome = await AdvertisingBudgetConsumer.TryConsumeAsync(_context, campaignId, operationalCost, utcNow, cancellationToken);

        if (budgetOutcome != BudgetConsumptionOutcome.Consumed)
        {
            await transaction.RollbackAsync(cancellationToken);
            var factOutcome = budgetOutcome == BudgetConsumptionOutcome.BudgetExceeded
                ? AdvertisingFactOutcome.BudgetExceeded
                : AdvertisingFactOutcome.CampaignNotActive;
            return new AdvertisingFactRecordResult(factOutcome, null);
        }

        var impression = AdvertisingImpression.Record(campaignId, placementId, idempotencyKey, operationalCost, occurredAtUtc, utcNow);

        try
        {
            await _context.AdvertisingImpressions.AddAsync(impression, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            var winner = await _context.AdvertisingImpressions
                .FirstOrDefaultAsync(i => i.IdempotencyKey == idempotencyKey, cancellationToken);
            return new AdvertisingFactRecordResult(AdvertisingFactOutcome.Replayed, winner?.Id);
        }

        await transaction.CommitAsync(cancellationToken);
        return new AdvertisingFactRecordResult(AdvertisingFactOutcome.Created, impression.Id);
    }
}

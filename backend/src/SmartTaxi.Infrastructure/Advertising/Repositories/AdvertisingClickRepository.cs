using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Advertising.Contracts;
using SmartTaxi.Domain.Advertising.Entities;
using SmartTaxi.Infrastructure.Advertising;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Advertising.Repositories;

/// <summary>Same atomicity guarantee as AdvertisingImpressionRepository.TryRecordAsync, applied to clicks.</summary>
internal sealed class AdvertisingClickRepository : IAdvertisingClickRepository
{
    private readonly ApplicationDbContext _context;

    public AdvertisingClickRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<int> CountForCampaignAsync(Guid campaignId, CancellationToken cancellationToken) =>
        _context.AdvertisingClicks.CountAsync(click => click.CampaignId == campaignId, cancellationToken);

    public async Task<AdvertisingFactRecordResult> TryRecordAsync(
        Guid campaignId, Guid placementId, Guid? impressionId, string idempotencyKey, decimal operationalCost, DateTime occurredAtUtc,
        DateTime utcNow, CancellationToken cancellationToken)
    {
        var existing = await _context.AdvertisingClicks.FirstOrDefaultAsync(click => click.IdempotencyKey == idempotencyKey, cancellationToken);

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

        var click = AdvertisingClick.Record(campaignId, placementId, impressionId, idempotencyKey, operationalCost, occurredAtUtc, utcNow);

        try
        {
            await _context.AdvertisingClicks.AddAsync(click, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            var winner = await _context.AdvertisingClicks.FirstOrDefaultAsync(c => c.IdempotencyKey == idempotencyKey, cancellationToken);
            return new AdvertisingFactRecordResult(AdvertisingFactOutcome.Replayed, winner?.Id);
        }

        await transaction.CommitAsync(cancellationToken);
        return new AdvertisingFactRecordResult(AdvertisingFactOutcome.Created, click.Id);
    }
}

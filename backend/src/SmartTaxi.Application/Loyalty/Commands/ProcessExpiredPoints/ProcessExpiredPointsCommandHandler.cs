using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Application.Loyalty.Contracts;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Application.Loyalty.Commands.ProcessExpiredPoints;

/// <summary>
/// Per-lot expiration: each Earn entry due for expiration is expired by
/// exactly its OWN remaining amount (LoyaltyPointLedgerEntry.RemainingAmount),
/// never by the account's aggregate balance — see the Module 7 audit finding
/// #2 that replaced the previous Min(entry.Points, account.CurrentRewardPoints)
/// approach, which could incorrectly expire a different, untouched lot's
/// still-valid points. The authoritative remaining amount is derived and
/// consumed atomically inside ILoyaltyPointLedgerRepository.TryDebitAsync
/// itself (SourceId = the Earn entry's own Id), so this handler only needs to
/// find due lots and ask for each to be expired. SourceId's idempotency guard
/// means the very same lot can only ever be expired once, including a
/// legitimate zero-amount expiry (a fully-already-spent lot) — that zero-amount
/// row still permanently marks the lot processed so it is never reconsidered
/// on a later sweep.
/// </summary>
public sealed class ProcessExpiredPointsCommandHandler : ICommandHandler<ProcessExpiredPointsCommand, Result<int>>
{
    private const string LedgerEntrySourceType = "LoyaltyPointLedgerEntry";

    private readonly ILoyaltyPointLedgerRepository _ledgerRepository;

    public ProcessExpiredPointsCommandHandler(ILoyaltyPointLedgerRepository ledgerRepository)
    {
        _ledgerRepository = ledgerRepository;
    }

    public async Task<Result<int>> Handle(ProcessExpiredPointsCommand command, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var dueEntries = await _ledgerRepository.GetDueForExpirationAsync(utcNow, cancellationToken);
        var expiredCount = 0;

        foreach (var earnEntry in dueEntries)
        {
            // Points passed here is only a placeholder — TryDebitAsync ignores it for Expire entries and
            // derives the authoritative amount from the lot's own current RemainingAmount instead.
            var result = await _ledgerRepository.TryDebitAsync(
                new LoyaltyLedgerAppendRequest(
                    earnEntry.LoyaltyAccountId, earnEntry.UserId, LoyaltyPointType.RewardPoints, LoyaltyLedgerEntryType.Expire,
                    earnEntry.Points, LedgerEntrySourceType, earnEntry.Id, "Expiration des points de fidélité"),
                utcNow, cancellationToken);

            if (result is not null)
            {
                expiredCount++;
            }
        }

        return Result<int>.Success(expiredCount);
    }
}

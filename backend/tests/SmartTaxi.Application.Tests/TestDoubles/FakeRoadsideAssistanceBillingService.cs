using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>Mirrors FakeMaintenanceBillingService exactly — same settlement-recovery test surface.</summary>
public sealed class FakeRoadsideAssistanceBillingService : IRoadsideAssistanceBillingService
{
    private readonly HashSet<Guid> _settledRequestIds = [];

    public List<(Guid RequestId, Guid RequesterUserId, Guid PartnerUserId, decimal Amount)> SettledCalls { get; } = [];

    /// <summary>Simulates the settlement partial-failure scenario: the ledger entry exists (as if PostBatchAsync had already committed in a prior, crashed attempt) even though TrySettleRequestAsync will report "already settled" (false).</summary>
    public HashSet<Guid> RequestIdsWithOrphanedLedgerEntry { get; } = [];

    public Task<bool> TrySettleRequestAsync(
        Guid requestId, Guid requesterUserId, RoadsideRequesterRole requesterRole, Guid partnerUserId, decimal amount, string currency,
        DateTime utcNow, CancellationToken cancellationToken)
    {
        if (RequestIdsWithOrphanedLedgerEntry.Contains(requestId) || !_settledRequestIds.Add(requestId))
        {
            return Task.FromResult(false);
        }

        SettledCalls.Add((requestId, requesterUserId, partnerUserId, amount));
        return Task.FromResult(true);
    }

    public Task<bool> HasSettlementEntryAsync(Guid requestId, CancellationToken cancellationToken) =>
        Task.FromResult(_settledRequestIds.Contains(requestId) || RequestIdsWithOrphanedLedgerEntry.Contains(requestId));
}

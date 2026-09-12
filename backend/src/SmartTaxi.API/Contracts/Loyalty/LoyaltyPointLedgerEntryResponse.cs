using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.API.Contracts.Loyalty;

public sealed record LoyaltyPointLedgerEntryResponse(
    Guid Id, string PointType, string EntryType, int Points, int BalanceAfter, string SourceType, Guid SourceId, string Reason,
    DateTime? ExpirationAtUtc, DateTime CreatedAtUtc)
{
    public static LoyaltyPointLedgerEntryResponse FromEntity(LoyaltyPointLedgerEntry entry) => new(
        entry.Id, entry.PointType.ToString(), entry.EntryType.ToString(), entry.Points, entry.BalanceAfter, entry.SourceType, entry.SourceId,
        entry.Reason, entry.ExpirationAtUtc, entry.CreatedAtUtc);
}

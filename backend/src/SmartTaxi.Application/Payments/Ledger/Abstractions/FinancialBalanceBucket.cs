namespace SmartTaxi.Application.Payments.Ledger.Abstractions;

/// <summary>Names one of FinancialAccount's five balance columns — an Application-layer posting-policy concept, not a Domain invariant (the Domain entity exposes the buckets as plain properties, not a keyed dictionary).</summary>
public enum FinancialBalanceBucket
{
    Pending,
    Available,
    Reserved,
    PaidOut,
    Debt
}

/// <summary>One bucket adjustment on one account — Delta may be negative.</summary>
public sealed record FinancialBalanceEffect(FinancialBalanceBucket Bucket, decimal Delta);

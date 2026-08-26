namespace SmartTaxi.Application.Advertising.Contracts;

public enum AdvertisingFactOutcome
{
    Created,

    /// <summary>The same IdempotencyKey already produced a fact — no new budget consumption, the existing fact's Id is returned.</summary>
    Replayed,

    CampaignNotActive,

    BudgetExceeded,
}

public sealed record AdvertisingFactRecordResult(AdvertisingFactOutcome Outcome, Guid? FactId);

using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Advertising.Events;

/// <summary>Genuinely raised (unit-testable), same convention as every other module's Created event — no dispatcher/outbox consumes these; INotificationDispatcher is the real integration point Advertising's Application handlers call directly.</summary>
public sealed record AdvertiserProfileRegistered(Guid AdvertiserProfileId, Guid UserId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record AdCampaignCreated(Guid CampaignId, Guid AdvertiserUserId, DateTime OccurredAtUtc) : IDomainEvent;

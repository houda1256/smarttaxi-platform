using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.RecordImpression;

/// <summary>DeliveryToken (from RequestAdDeliveryCommand) replaces a bare client-supplied IdempotencyKey — see the Module 8 audit fix #1. OccurredAtUtc is informational only, never trusted for security/billing decisions.</summary>
public sealed record RecordImpressionCommand(Guid CampaignId, Guid PlacementId, Guid RequestingUserId, string DeliveryToken, DateTime OccurredAtUtc)
    : ICommand<Result<Guid>>;

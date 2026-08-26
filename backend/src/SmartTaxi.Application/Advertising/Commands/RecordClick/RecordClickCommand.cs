using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.RecordClick;

/// <summary>DeliveryToken (from RequestAdDeliveryCommand) replaces a bare client-supplied IdempotencyKey — see the Module 8 audit fix #1. ImpressionId is an optional analytics cross-reference only, never a security check. OccurredAtUtc is informational only, never trusted for security/billing decisions.</summary>
public sealed record RecordClickCommand(Guid CampaignId, Guid PlacementId, Guid RequestingUserId, Guid? ImpressionId, string DeliveryToken, DateTime OccurredAtUtc)
    : ICommand<Result<Guid>>;

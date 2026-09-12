using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Application.Loyalty.Commands.UpdateTierThreshold;

/// <summary>Upserts — the four tiers are a fixed catalog seeded by migration; admins only ever retune the threshold, never add/remove a tier.</summary>
public sealed record UpdateTierThresholdCommand(LoyaltyTier Tier, int MinimumStatusPoints) : ICommand<Result>;

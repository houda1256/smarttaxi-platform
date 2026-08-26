using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Application.Loyalty.Commands.AdjustPoints;

/// <summary>Points is signed: positive credits, negative debits. Each call is its own independent, audited adjustment (fresh SourceId) — deliberately not idempotency-guarded the way automated earning is, since a human admin issuing two separate corrections is a legitimate, distinct scenario, not a retry.</summary>
public sealed record AdjustPointsCommand(Guid AdminUserId, Guid UserId, LoyaltyPointType PointType, int Points, string Reason) : ICommand<Result>;

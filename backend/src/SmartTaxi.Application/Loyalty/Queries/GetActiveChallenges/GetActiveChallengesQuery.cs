using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Queries.GetActiveChallenges;

public sealed record GetActiveChallengesQuery(UserRole Role) : IQuery<IReadOnlyCollection<LoyaltyChallenge>>;

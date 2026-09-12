using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Queries.GetChallengesAdmin;

public sealed record GetChallengesAdminQuery : IQuery<IReadOnlyCollection<LoyaltyChallenge>>;

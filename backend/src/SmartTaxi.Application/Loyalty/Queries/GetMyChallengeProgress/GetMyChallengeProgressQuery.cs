using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Queries.GetMyChallengeProgress;

public sealed record GetMyChallengeProgressQuery(Guid UserId) : IQuery<IReadOnlyCollection<LoyaltyChallengeProgress>>;

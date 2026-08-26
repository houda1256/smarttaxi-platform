using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Queries.GetActiveChallenges;

public sealed class GetActiveChallengesQueryHandler : IQueryHandler<GetActiveChallengesQuery, IReadOnlyCollection<LoyaltyChallenge>>
{
    private readonly ILoyaltyChallengeRepository _challengeRepository;

    public GetActiveChallengesQueryHandler(ILoyaltyChallengeRepository challengeRepository)
    {
        _challengeRepository = challengeRepository;
    }

    public Task<IReadOnlyCollection<LoyaltyChallenge>> Handle(GetActiveChallengesQuery query, CancellationToken cancellationToken) =>
        _challengeRepository.GetActiveForRoleAsync(query.Role, DateTime.UtcNow, cancellationToken);
}

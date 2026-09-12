using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Queries.GetChallengesAdmin;

public sealed class GetChallengesAdminQueryHandler : IQueryHandler<GetChallengesAdminQuery, IReadOnlyCollection<LoyaltyChallenge>>
{
    private readonly ILoyaltyChallengeRepository _repository;

    public GetChallengesAdminQueryHandler(ILoyaltyChallengeRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyCollection<LoyaltyChallenge>> Handle(GetChallengesAdminQuery query, CancellationToken cancellationToken) =>
        _repository.GetAllAsync(cancellationToken);
}

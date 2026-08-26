using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Queries.GetMyChallengeProgress;

public sealed class GetMyChallengeProgressQueryHandler : IQueryHandler<GetMyChallengeProgressQuery, IReadOnlyCollection<LoyaltyChallengeProgress>>
{
    private readonly ILoyaltyChallengeProgressRepository _progressRepository;

    public GetMyChallengeProgressQueryHandler(ILoyaltyChallengeProgressRepository progressRepository)
    {
        _progressRepository = progressRepository;
    }

    public Task<IReadOnlyCollection<LoyaltyChallengeProgress>> Handle(GetMyChallengeProgressQuery query, CancellationToken cancellationToken) =>
        _progressRepository.GetForUserAsync(query.UserId, cancellationToken);
}

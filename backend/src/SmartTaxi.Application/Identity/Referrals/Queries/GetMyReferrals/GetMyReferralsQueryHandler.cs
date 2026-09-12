using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Referrals.Abstractions;
using SmartTaxi.Application.Identity.Referrals.Commands.EvaluateReferralActivation;
using SmartTaxi.Domain.Identity.Referrals.Enums;

namespace SmartTaxi.Application.Identity.Referrals.Queries.GetMyReferrals;

public sealed class GetMyReferralsQueryHandler : IQueryHandler<GetMyReferralsQuery, IReadOnlyCollection<ReferralSummary>>
{
    private readonly IReferralRepository _repository;
    private readonly EvaluateReferralActivationCommandHandler _activationHandler;

    public GetMyReferralsQueryHandler(IReferralRepository repository, EvaluateReferralActivationCommandHandler activationHandler)
    {
        _repository = repository;
        _activationHandler = activationHandler;
    }

    public async Task<IReadOnlyCollection<ReferralSummary>> Handle(GetMyReferralsQuery query, CancellationToken cancellationToken)
    {
        var referrals = await _repository.GetForReferrerAsync(query.UserId, cancellationToken);

        // Read-repair: opportunistically re-check activation conditions for
        // still-pending referrals, mirroring 2d's expiration sweep pattern.
        var pendingIds = referrals.Where(r => r.Status == ReferralStatus.PendingActivation).Select(r => r.Id).ToList();

        if (pendingIds.Count > 0)
        {
            foreach (var id in pendingIds)
            {
                await _activationHandler.Handle(new EvaluateReferralActivationCommand(id), cancellationToken);
            }

            referrals = await _repository.GetForReferrerAsync(query.UserId, cancellationToken);
        }

        return referrals.Select(ReferralSummary.FromEntity).ToList();
    }
}

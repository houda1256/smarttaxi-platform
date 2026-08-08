using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Referrals;

namespace SmartTaxi.Application.Identity.Referrals.Queries.GetMyReferrals;

public sealed record GetMyReferralsQuery(Guid UserId) : IQuery<IReadOnlyCollection<ReferralSummary>>;

using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Referrals;

namespace SmartTaxi.Application.Identity.Referrals.Queries.GetPendingReferralsAdmin;

public sealed record GetPendingReferralsAdminQuery : IQuery<IReadOnlyCollection<ReferralSummary>>;

using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Referrals.Queries.GetMyReferralCode;

public sealed record GetMyReferralCodeQuery(Guid UserId) : IQuery<Result<string>>;

using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Referrals.Commands.InvalidateReferral;

public sealed record InvalidateReferralCommand(Guid AdminId, Guid ReferralId) : ICommand<Result>;

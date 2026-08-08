using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Referrals.Commands.RegisterReferral;

/// <summary>
/// Called by the API layer right after a successful registration, if the
/// caller supplied a referral code. Kept as a separate command (rather than
/// folded into RegisterUserCommand) so referral registration can also be
/// invoked from other flows later without coupling to the base registration
/// handler's constructor.
/// </summary>
public sealed record RegisterReferralCommand(string ReferralCode, Guid RefereeUserId) : ICommand<Result>;

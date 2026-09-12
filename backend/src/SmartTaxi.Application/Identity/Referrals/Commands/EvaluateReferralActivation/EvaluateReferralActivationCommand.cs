using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Referrals.Commands.EvaluateReferralActivation;

public sealed record EvaluateReferralActivationCommand(Guid ReferralId) : ICommand<Result<bool>>;

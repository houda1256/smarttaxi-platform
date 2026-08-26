using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Subscriptions.Abstractions;
using SmartTaxi.Domain.Subscriptions.Enums;

namespace SmartTaxi.Application.Subscriptions.Commands.ChangeSubscriptionPlan;

/// <summary>
/// No proration/immediate re-charge in this phase — the new plan's price applies
/// starting at the next renewal, the current billing cycle just continues under
/// the new plan's features/limits. Flagged as a known limitation in the phase report.
/// </summary>
public sealed class ChangeSubscriptionPlanCommandHandler : ICommandHandler<ChangeSubscriptionPlanCommand, Result>
{
    private const string NotFoundError = "Abonnement introuvable.";
    private const string NotOwnerError = "Seul le titulaire peut changer ce plan.";
    private const string NotActiveError = "Seul un abonnement actif peut changer de plan.";
    private const string NewPlanNotFoundError = "Nouveau plan introuvable ou indisponible.";
    private const string RoleMismatchError = "Le nouveau plan doit cibler le même rôle que l'abonnement actuel.";
    private const string ChangeConflictError = "Impossible de changer de plan pour cet abonnement.";

    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionPlanRepository _planRepository;

    public ChangeSubscriptionPlanCommandHandler(
        ISubscriptionRepository subscriptionRepository, ISubscriptionPlanRepository planRepository)
    {
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
    }

    public async Task<Result> Handle(ChangeSubscriptionPlanCommand command, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(command.SubscriptionId, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (subscription.SubscriberId != command.RequestingUserId)
        {
            return Result.Failure(NotOwnerError, ErrorType.Forbidden);
        }

        if (subscription.Status != SubscriptionStatus.Active)
        {
            return Result.Failure(NotActiveError, ErrorType.Conflict);
        }

        var newPlan = await _planRepository.GetByIdAsync(command.NewPlanId, cancellationToken);

        if (newPlan is null || !newPlan.IsActive)
        {
            return Result.Failure(NewPlanNotFoundError, ErrorType.NotFound);
        }

        if (newPlan.TargetRole != subscription.TargetRole)
        {
            return Result.Failure(RoleMismatchError, ErrorType.Validation);
        }

        var changed = await _subscriptionRepository.TryChangePlanAsync(
            subscription.Id, newPlan.Id, DateTime.UtcNow, cancellationToken);

        return changed ? Result.Success() : Result.Failure(ChangeConflictError, ErrorType.Conflict);
    }
}

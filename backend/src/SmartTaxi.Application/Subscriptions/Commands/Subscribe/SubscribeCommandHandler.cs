using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.Payments.SubscriptionCharges.Abstractions;
using SmartTaxi.Application.Subscriptions.Abstractions;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Domain.Subscriptions;
using SmartTaxi.Domain.Subscriptions.Entities;

namespace SmartTaxi.Application.Subscriptions.Commands.Subscribe;

/// <summary>
/// Create -> validate plan -> validate no existing Pending/Active subscription
/// for the same role -> charge (skipped while in trial or for a free plan) ->
/// activate, exactly the workflow the module spec describes. Charging goes
/// through ISubscriptionChargeCollector — Subscription never touches
/// Payment/Invoice tables directly.
/// </summary>
public sealed class SubscribeCommandHandler : ICommandHandler<SubscribeCommand, Result<Guid>>
{
    private const string PlanNotFoundError = "Plan introuvable ou indisponible.";
    private const string RoleMismatchError = "Le rôle de l'utilisateur ne correspond pas au rôle cible de ce plan.";
    private const string AlreadySubscribedError = "Un abonnement est déjà actif ou en attente pour ce rôle.";
    private const string DuplicateSubscriptionError = "Un abonnement existe déjà pour ce rôle.";
    private const string ActivationConflictError = "Impossible d'activer l'abonnement.";

    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionChargeCollector _chargeCollector;
    private readonly INotificationDispatcher _notificationDispatcher;

    public SubscribeCommandHandler(
        ISubscriptionPlanRepository planRepository, ISubscriptionRepository subscriptionRepository,
        ISubscriptionChargeCollector chargeCollector, INotificationDispatcher notificationDispatcher)
    {
        _planRepository = planRepository;
        _subscriptionRepository = subscriptionRepository;
        _chargeCollector = chargeCollector;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result<Guid>> Handle(SubscribeCommand command, CancellationToken cancellationToken)
    {
        var plan = await _planRepository.GetByIdAsync(command.PlanId, cancellationToken);

        if (plan is null || !plan.IsActive)
        {
            return Result<Guid>.Failure(PlanNotFoundError, ErrorType.NotFound);
        }

        if (!command.CallerRoles.Contains(plan.TargetRole))
        {
            return Result<Guid>.Failure(RoleMismatchError, ErrorType.Forbidden);
        }

        var existing = await _subscriptionRepository.GetActiveOrPendingForSubscriberAsync(
            command.SubscriberId, plan.TargetRole, cancellationToken);

        if (existing is not null)
        {
            return Result<Guid>.Failure(AlreadySubscribedError, ErrorType.Conflict);
        }

        var utcNow = DateTime.UtcNow;
        var startDate = utcNow;
        var endDate = SubscriptionPeriodCalculator.AddBillingPeriod(startDate, plan.BillingPeriod);
        DateTime? trialEndsAt = plan.TrialPeriodDays > 0 ? startDate.AddDays(plan.TrialPeriodDays) : null;

        Subscription subscription;

        try
        {
            subscription = Subscription.CreatePending(
                command.SubscriberId, plan.Id, plan.TargetRole, startDate, endDate, command.AutoRenew, trialEndsAt, utcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        var added = await _subscriptionRepository.TryAddAsync(subscription, cancellationToken);

        if (!added)
        {
            return Result<Guid>.Failure(DuplicateSubscriptionError, ErrorType.Conflict);
        }

        var inTrial = trialEndsAt is not null && trialEndsAt > utcNow;

        if (plan.Price > 0 && !inTrial)
        {
            var chargeResult = await _chargeCollector.ChargeAsync(
                command.SubscriberId, subscription.Id, plan.Price, plan.Currency, utcNow, cancellationToken);

            if (!chargeResult.IsSuccess)
            {
                await _subscriptionRepository.TryCancelAsync(subscription.Id, utcNow, cancellationToken);
                return Result<Guid>.Failure(chargeResult.Error!, chargeResult.ErrorType!.Value);
            }
        }

        var activated = await _subscriptionRepository.TryActivateAsync(subscription.Id, utcNow, cancellationToken);

        if (!activated)
        {
            // The charge (if any) already succeeded above — cancel the stuck Pending row so it
            // doesn't block a retry and doesn't leave a confirmed charge with no delivered benefit.
            await _subscriptionRepository.TryCancelAsync(subscription.Id, utcNow, cancellationToken);
            return Result<Guid>.Failure(ActivationConflictError, ErrorType.Conflict);
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                command.SubscriberId, NotificationCategory.Subscription, "subscription.activated",
                new Dictionary<string, string> { ["PlanName"] = plan.Name },
                IsMandatory: false, SourceType: "Subscription", SourceId: subscription.Id),
            cancellationToken);

        return Result<Guid>.Success(subscription.Id);
    }
}

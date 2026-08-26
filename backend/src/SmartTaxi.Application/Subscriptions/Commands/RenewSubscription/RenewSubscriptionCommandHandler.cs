using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.Payments.SubscriptionCharges.Abstractions;
using SmartTaxi.Application.Subscriptions.Abstractions;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Domain.Subscriptions;
using SmartTaxi.Domain.Subscriptions.Enums;

namespace SmartTaxi.Application.Subscriptions.Commands.RenewSubscription;

/// <summary>Extends from the current EndDate (not "now") so a renewal never loses already-paid-for time.</summary>
public sealed class RenewSubscriptionCommandHandler : ICommandHandler<RenewSubscriptionCommand, Result>
{
    private const string NotFoundError = "Abonnement introuvable.";
    private const string NotOwnerError = "Seul le titulaire peut renouveler cet abonnement.";
    private const string NotActiveError = "Seul un abonnement actif peut être renouvelé.";
    private const string PlanNotFoundError = "Plan introuvable.";
    private const string RenewalConflictError = "Impossible de renouveler cet abonnement.";

    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly ISubscriptionChargeCollector _chargeCollector;
    private readonly INotificationDispatcher _notificationDispatcher;

    public RenewSubscriptionCommandHandler(
        ISubscriptionRepository subscriptionRepository, ISubscriptionPlanRepository planRepository,
        ISubscriptionChargeCollector chargeCollector, INotificationDispatcher notificationDispatcher)
    {
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
        _chargeCollector = chargeCollector;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(RenewSubscriptionCommand command, CancellationToken cancellationToken)
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

        var plan = await _planRepository.GetByIdAsync(subscription.PlanId, cancellationToken);

        if (plan is null)
        {
            return Result.Failure(PlanNotFoundError, ErrorType.NotFound);
        }

        var utcNow = DateTime.UtcNow;
        var previousEndDate = subscription.EndDate;
        var newEndDate = SubscriptionPeriodCalculator.AddBillingPeriod(previousEndDate, plan.BillingPeriod);

        // Reserve the renewal atomically (guarded on Status == Active AND EndDate == previousEndDate)
        // before charging, so a lost race — another renewal, a cancellation, or the expire-due sweep
        // touching this subscription first — is rejected here and never reaches the charge collector.
        var reserved = await _subscriptionRepository.TryRenewAsync(
            subscription.Id, previousEndDate, newEndDate, utcNow, cancellationToken);

        if (!reserved)
        {
            return Result.Failure(RenewalConflictError, ErrorType.Conflict);
        }

        if (plan.Price > 0)
        {
            var chargeResult = await _chargeCollector.ChargeAsync(
                subscription.SubscriberId, subscription.Id, plan.Price, plan.Currency, utcNow, cancellationToken);

            if (!chargeResult.IsSuccess)
            {
                // The reservation already landed but the charge never did — the collector only ever
                // persists a charge when it fully succeeds, so there is nothing to refund/void in
                // Payments here, only our own reservation to revert.
                await _subscriptionRepository.TryRenewAsync(subscription.Id, newEndDate, previousEndDate, utcNow, cancellationToken);
                return Result.Failure(chargeResult.Error!, chargeResult.ErrorType!.Value);
            }
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                subscription.SubscriberId, NotificationCategory.Subscription, "subscription.renewed",
                new Dictionary<string, string> { ["NewEndDate"] = newEndDate.ToString("yyyy-MM-dd") },
                IsMandatory: false, SourceType: "Subscription", SourceId: subscription.Id),
            cancellationToken);

        return Result.Success();
    }
}

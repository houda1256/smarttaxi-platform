using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.SubscriptionCharges.Abstractions;
using SmartTaxi.Application.Subscriptions.Abstractions;
using SmartTaxi.Application.Subscriptions.Commands.CancelSubscription;
using SmartTaxi.Application.Subscriptions.Commands.ChangeSubscriptionPlan;
using SmartTaxi.Application.Subscriptions.Commands.CreateSubscriptionPlan;
using SmartTaxi.Application.Subscriptions.Commands.RenewSubscription;
using SmartTaxi.Application.Subscriptions.Commands.Subscribe;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Subscriptions.Enums;

namespace SmartTaxi.Application.Tests.Subscriptions;

public class SubscriptionCommandHandlerTests
{
    private readonly FakeSubscriptionPlanRepository _planRepository = new();
    private readonly FakeSubscriptionRepository _subscriptionRepository = new();
    private readonly FakeSubscriptionChargeCollector _chargeCollector = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();

    private readonly CreateSubscriptionPlanCommandHandler _createPlanHandler;
    private readonly SubscribeCommandHandler _subscribeHandler;
    private readonly RenewSubscriptionCommandHandler _renewHandler;
    private readonly CancelSubscriptionCommandHandler _cancelHandler;
    private readonly ChangeSubscriptionPlanCommandHandler _changePlanHandler;

    public SubscriptionCommandHandlerTests()
    {
        _createPlanHandler = new CreateSubscriptionPlanCommandHandler(_planRepository);
        _subscribeHandler = new SubscribeCommandHandler(_planRepository, _subscriptionRepository, _chargeCollector, _notificationDispatcher);
        _renewHandler = new RenewSubscriptionCommandHandler(_subscriptionRepository, _planRepository, _chargeCollector, _notificationDispatcher);
        _cancelHandler = new CancelSubscriptionCommandHandler(_subscriptionRepository, _notificationDispatcher);
        _changePlanHandler = new ChangeSubscriptionPlanCommandHandler(_subscriptionRepository, _planRepository);
    }

    private async Task<Guid> CreatePlanAsync(decimal price = 29.90m, int trialDays = 0, UserRole role = UserRole.Driver)
    {
        var result = await _createPlanHandler.Handle(
            new CreateSubscriptionPlanCommand(
                "Driver Pro", $"DRIVER_PRO_{Guid.NewGuid():N}", "desc", role, price, "TND", BillingPeriod.Monthly, trialDays,
                ["PriorityDispatch"], new Dictionary<string, int> { ["VehiclesCount"] = 3 }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        return result.Value;
    }

    [Fact]
    public async Task CreatePlan_DuplicateCode_ReturnsConflict()
    {
        var code = $"DUP_{Guid.NewGuid():N}";
        var first = new CreateSubscriptionPlanCommand(
            "A", code, "desc", UserRole.Driver, 10m, "TND", BillingPeriod.Monthly, 0, [], new Dictionary<string, int>());
        var second = first with { Name = "B" };

        Assert.True((await _createPlanHandler.Handle(first, CancellationToken.None)).IsSuccess);
        var result = await _createPlanHandler.Handle(second, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Subscribe_ToPaidPlan_ChargesAndActivates()
    {
        var planId = await CreatePlanAsync(price: 29.90m);
        var subscriberId = Guid.NewGuid();

        var result = await _subscribeHandler.Handle(new SubscribeCommand(subscriberId, planId, true, [UserRole.Driver]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_chargeCollector.Charges);
        Assert.Equal(29.90m, _chargeCollector.Charges[0].Amount);

        var subscription = await _subscriptionRepository.GetByIdAsync(result.Value, CancellationToken.None);
        Assert.Equal(SubscriptionStatus.Active, subscription!.Status);
        Assert.Contains(
            _notificationDispatcher.DispatchedRequests,
            request => request.SourceId == subscription.Id && request.TemplateKey == "subscription.activated");
    }

    [Fact]
    public async Task Subscribe_ToFreePlan_ActivatesWithoutCharging()
    {
        var planId = await CreatePlanAsync(price: 0m);
        var subscriberId = Guid.NewGuid();

        var result = await _subscribeHandler.Handle(new SubscribeCommand(subscriberId, planId, false, [UserRole.Driver]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_chargeCollector.Charges);

        var subscription = await _subscriptionRepository.GetByIdAsync(result.Value, CancellationToken.None);
        Assert.Equal(SubscriptionStatus.Active, subscription!.Status);
    }

    [Fact]
    public async Task Subscribe_DuringTrial_SkipsChargeButActivates()
    {
        var planId = await CreatePlanAsync(price: 29.90m, trialDays: 14);
        var subscriberId = Guid.NewGuid();

        var result = await _subscribeHandler.Handle(new SubscribeCommand(subscriberId, planId, false, [UserRole.Driver]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_chargeCollector.Charges);
    }

    [Fact]
    public async Task Subscribe_WhenAlreadySubscribedForRole_ReturnsConflict()
    {
        var planId = await CreatePlanAsync();
        var subscriberId = Guid.NewGuid();

        await _subscribeHandler.Handle(new SubscribeCommand(subscriberId, planId, false, [UserRole.Driver]), CancellationToken.None);
        var second = await _subscribeHandler.Handle(new SubscribeCommand(subscriberId, planId, false, [UserRole.Driver]), CancellationToken.None);

        Assert.False(second.IsSuccess);
        Assert.Equal(ErrorType.Conflict, second.ErrorType);
    }

    [Fact]
    public async Task Subscribe_WhenChargeFails_CancelsSubscriptionAndReturnsFailure()
    {
        var planId = await CreatePlanAsync(price: 29.90m);
        var subscriberId = Guid.NewGuid();
        _chargeCollector.ShouldFail = true;

        var result = await _subscribeHandler.Handle(new SubscribeCommand(subscriberId, planId, false, [UserRole.Driver]), CancellationToken.None);

        Assert.False(result.IsSuccess);

        // No live (Pending/Active) subscription should remain for this subscriber+role.
        var stillActive = await _subscriptionRepository.GetActiveOrPendingForSubscriberAsync(subscriberId, UserRole.Driver, CancellationToken.None);
        Assert.Null(stillActive);
    }

    [Fact]
    public async Task Subscribe_ToUnknownPlan_ReturnsNotFound()
    {
        var result = await _subscribeHandler.Handle(new SubscribeCommand(Guid.NewGuid(), Guid.NewGuid(), false, [UserRole.Driver]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Subscribe_WhenCallerRoleMatchesPlanTargetRole_Succeeds()
    {
        var planId = await CreatePlanAsync(price: 29.90m, role: UserRole.Driver);
        var subscriberId = Guid.NewGuid();

        var result = await _subscribeHandler.Handle(
            new SubscribeCommand(subscriberId, planId, false, [UserRole.Driver]), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Subscribe_WhenCallerRoleDoesNotMatchPlanTargetRole_RejectsWithoutCreatingSubscriptionOrCharging()
    {
        var planId = await CreatePlanAsync(price: 29.90m, role: UserRole.Driver);
        var subscriberId = Guid.NewGuid();

        // Caller only holds Customer, but the plan targets Driver.
        var result = await _subscribeHandler.Handle(
            new SubscribeCommand(subscriberId, planId, false, [UserRole.Customer]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);

        // No subscription must have been created for this subscriber+role.
        var created = await _subscriptionRepository.GetActiveOrPendingForSubscriberAsync(subscriberId, UserRole.Driver, CancellationToken.None);
        Assert.Null(created);

        // No charge must have been attempted.
        Assert.Empty(_chargeCollector.Charges);
    }

    [Fact]
    public async Task Subscribe_WhenActivationFailsAfterSuccessfulCharge_CancelsSubscriptionInsteadOfStayingPending()
    {
        var planId = await CreatePlanAsync(price: 29.90m);
        var subscriberId = Guid.NewGuid();

        // Simulates another request cancelling the subscription while the charge round-trip is in flight,
        // so TryActivateAsync (guarded on Status == Pending) fails after the charge has already succeeded.
        var handler = new SubscribeCommandHandler(
            _planRepository, _subscriptionRepository, new CancellingSubscriptionChargeCollector(_subscriptionRepository),
            _notificationDispatcher);

        var result = await handler.Handle(new SubscribeCommand(subscriberId, planId, false, [UserRole.Driver]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);

        // Must not be left stuck Pending — that would permanently block retry via the partial unique index.
        var stillPendingOrActive = await _subscriptionRepository.GetActiveOrPendingForSubscriberAsync(subscriberId, UserRole.Driver, CancellationToken.None);
        Assert.Null(stillPendingOrActive);
    }

    [Fact]
    public async Task Renew_ActiveSubscription_ExtendsEndDateFromCurrentEndDate()
    {
        var planId = await CreatePlanAsync(price: 10m);
        var subscriberId = Guid.NewGuid();
        var subscribeResult = await _subscribeHandler.Handle(new SubscribeCommand(subscriberId, planId, false, [UserRole.Driver]), CancellationToken.None);
        var before = (await _subscriptionRepository.GetByIdAsync(subscribeResult.Value, CancellationToken.None))!.EndDate;

        var result = await _renewHandler.Handle(new RenewSubscriptionCommand(subscribeResult.Value, subscriberId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var after = (await _subscriptionRepository.GetByIdAsync(subscribeResult.Value, CancellationToken.None))!.EndDate;
        Assert.Equal(before.AddMonths(1), after);
        Assert.Contains(
            _notificationDispatcher.DispatchedRequests,
            request => request.SourceId == subscribeResult.Value && request.TemplateKey == "subscription.renewed");
    }

    [Fact]
    public async Task Renew_ByNonOwner_ReturnsForbidden()
    {
        var planId = await CreatePlanAsync(price: 10m);
        var subscriberId = Guid.NewGuid();
        var subscribeResult = await _subscribeHandler.Handle(new SubscribeCommand(subscriberId, planId, false, [UserRole.Driver]), CancellationToken.None);

        var result = await _renewHandler.Handle(new RenewSubscriptionCommand(subscribeResult.Value, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Renew_ReservationGuard_RejectsStaleExpectedEndDate()
    {
        var planId = await CreatePlanAsync(price: 10m);
        var subscriberId = Guid.NewGuid();
        var subscribeResult = await _subscribeHandler.Handle(new SubscribeCommand(subscriberId, planId, false, [UserRole.Driver]), CancellationToken.None);
        var subscription = (await _subscriptionRepository.GetByIdAsync(subscribeResult.Value, CancellationToken.None))!;
        var staleExpectedEndDate = subscription.EndDate.AddDays(-1);

        // Represents the loser of a race: another operation already moved EndDate away from what this
        // caller read, so the compare-and-swap reservation must be rejected before any charge occurs.
        var reserved = await _subscriptionRepository.TryRenewAsync(
            subscription.Id, staleExpectedEndDate, subscription.EndDate.AddMonths(1), DateTime.UtcNow, CancellationToken.None);

        Assert.False(reserved);
    }

    [Fact]
    public async Task Renew_WhenChargeFails_RevertsEndDateReservationAndReturnsFailure()
    {
        var planId = await CreatePlanAsync(price: 10m);
        var subscriberId = Guid.NewGuid();
        var subscribeResult = await _subscribeHandler.Handle(new SubscribeCommand(subscriberId, planId, false, [UserRole.Driver]), CancellationToken.None);
        var originalEndDate = (await _subscriptionRepository.GetByIdAsync(subscribeResult.Value, CancellationToken.None))!.EndDate;
        _chargeCollector.ShouldFail = true;

        var result = await _renewHandler.Handle(new RenewSubscriptionCommand(subscribeResult.Value, subscriberId), CancellationToken.None);

        Assert.False(result.IsSuccess);

        // The reservation must be reverted — no free extension without a successful charge.
        var afterEndDate = (await _subscriptionRepository.GetByIdAsync(subscribeResult.Value, CancellationToken.None))!.EndDate;
        Assert.Equal(originalEndDate, afterEndDate);
    }

    [Fact]
    public async Task Cancel_ActiveSubscription_Succeeds()
    {
        var planId = await CreatePlanAsync(price: 10m);
        var subscriberId = Guid.NewGuid();
        var subscribeResult = await _subscribeHandler.Handle(new SubscribeCommand(subscriberId, planId, false, [UserRole.Driver]), CancellationToken.None);

        var result = await _cancelHandler.Handle(new CancelSubscriptionCommand(subscribeResult.Value, subscriberId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var subscription = await _subscriptionRepository.GetByIdAsync(subscribeResult.Value, CancellationToken.None);
        Assert.Equal(SubscriptionStatus.Cancelled, subscription!.Status);
        Assert.Contains(
            _notificationDispatcher.DispatchedRequests,
            request => request.SourceId == subscribeResult.Value && request.TemplateKey == "subscription.cancelled");
    }

    [Fact]
    public async Task Cancel_AlreadyCancelled_ReturnsConflict()
    {
        var planId = await CreatePlanAsync(price: 10m);
        var subscriberId = Guid.NewGuid();
        var subscribeResult = await _subscribeHandler.Handle(new SubscribeCommand(subscriberId, planId, false, [UserRole.Driver]), CancellationToken.None);
        await _cancelHandler.Handle(new CancelSubscriptionCommand(subscribeResult.Value, subscriberId), CancellationToken.None);

        var result = await _cancelHandler.Handle(new CancelSubscriptionCommand(subscribeResult.Value, subscriberId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task ChangePlan_ToPlanWithDifferentRole_ReturnsValidationError()
    {
        var planId = await CreatePlanAsync(price: 10m, role: UserRole.Driver);
        var otherRolePlanId = await CreatePlanAsync(price: 10m, role: UserRole.TaxiOwner);
        var subscriberId = Guid.NewGuid();
        var subscribeResult = await _subscribeHandler.Handle(new SubscribeCommand(subscriberId, planId, false, [UserRole.Driver]), CancellationToken.None);

        var result = await _changePlanHandler.Handle(
            new ChangeSubscriptionPlanCommand(subscribeResult.Value, subscriberId, otherRolePlanId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task ChangePlan_ToSameRolePlan_Succeeds()
    {
        var planId = await CreatePlanAsync(price: 10m, role: UserRole.Driver);
        var upgradedPlanId = await CreatePlanAsync(price: 20m, role: UserRole.Driver);
        var subscriberId = Guid.NewGuid();
        var subscribeResult = await _subscribeHandler.Handle(new SubscribeCommand(subscriberId, planId, false, [UserRole.Driver]), CancellationToken.None);

        var result = await _changePlanHandler.Handle(
            new ChangeSubscriptionPlanCommand(subscribeResult.Value, subscriberId, upgradedPlanId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var subscription = await _subscriptionRepository.GetByIdAsync(subscribeResult.Value, CancellationToken.None);
        Assert.Equal(upgradedPlanId, subscription!.PlanId);
    }

    /// <summary>Test-only collector that cancels the subscription mid-charge, simulating another request winning a race during the charge round-trip.</summary>
    private sealed class CancellingSubscriptionChargeCollector : ISubscriptionChargeCollector
    {
        private readonly ISubscriptionRepository _subscriptionRepository;

        public CancellingSubscriptionChargeCollector(ISubscriptionRepository subscriptionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<Result<Guid>> ChargeAsync(
            Guid subscriberId, Guid subscriptionId, decimal amount, string currency, DateTime utcNow, CancellationToken cancellationToken)
        {
            await _subscriptionRepository.TryCancelAsync(subscriptionId, utcNow, cancellationToken);
            return Result<Guid>.Success(Guid.NewGuid());
        }
    }
}

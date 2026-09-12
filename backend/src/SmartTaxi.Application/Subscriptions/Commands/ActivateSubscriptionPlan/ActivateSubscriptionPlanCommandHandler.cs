using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Subscriptions.Abstractions;

namespace SmartTaxi.Application.Subscriptions.Commands.ActivateSubscriptionPlan;

public sealed class ActivateSubscriptionPlanCommandHandler : ICommandHandler<ActivateSubscriptionPlanCommand, Result>
{
    private const string NotFoundError = "Plan introuvable ou déjà actif.";

    private readonly ISubscriptionPlanRepository _planRepository;

    public ActivateSubscriptionPlanCommandHandler(ISubscriptionPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<Result> Handle(ActivateSubscriptionPlanCommand command, CancellationToken cancellationToken)
    {
        var activated = await _planRepository.TryActivateAsync(command.PlanId, DateTime.UtcNow, cancellationToken);

        return activated ? Result.Success() : Result.Failure(NotFoundError, ErrorType.Conflict);
    }
}

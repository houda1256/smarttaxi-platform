using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Subscriptions.Abstractions;

namespace SmartTaxi.Application.Subscriptions.Commands.DeactivateSubscriptionPlan;

public sealed class DeactivateSubscriptionPlanCommandHandler : ICommandHandler<DeactivateSubscriptionPlanCommand, Result>
{
    private const string NotFoundError = "Plan introuvable ou déjà inactif.";

    private readonly ISubscriptionPlanRepository _planRepository;

    public DeactivateSubscriptionPlanCommandHandler(ISubscriptionPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<Result> Handle(DeactivateSubscriptionPlanCommand command, CancellationToken cancellationToken)
    {
        var deactivated = await _planRepository.TryDeactivateAsync(command.PlanId, DateTime.UtcNow, cancellationToken);

        return deactivated ? Result.Success() : Result.Failure(NotFoundError, ErrorType.Conflict);
    }
}

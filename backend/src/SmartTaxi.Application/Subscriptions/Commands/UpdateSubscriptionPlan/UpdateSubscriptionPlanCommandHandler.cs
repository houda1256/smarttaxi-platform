using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Subscriptions.Abstractions;

namespace SmartTaxi.Application.Subscriptions.Commands.UpdateSubscriptionPlan;

public sealed class UpdateSubscriptionPlanCommandHandler : ICommandHandler<UpdateSubscriptionPlanCommand, Result>
{
    private const string NotFoundError = "Plan introuvable.";

    private readonly ISubscriptionPlanRepository _planRepository;

    public UpdateSubscriptionPlanCommandHandler(ISubscriptionPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<Result> Handle(UpdateSubscriptionPlanCommand command, CancellationToken cancellationToken)
    {
        var plan = await _planRepository.GetByIdAsync(command.PlanId, cancellationToken);

        if (plan is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        try
        {
            plan.UpdateDetails(command.Name, command.Description, command.Price, command.Features, command.Limits, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message, ErrorType.Validation);
        }

        await _planRepository.UpdateAsync(plan, cancellationToken);

        return Result.Success();
    }
}

using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Subscriptions.Abstractions;
using SmartTaxi.Domain.Subscriptions.Entities;

namespace SmartTaxi.Application.Subscriptions.Commands.CreateSubscriptionPlan;

public sealed class CreateSubscriptionPlanCommandHandler : ICommandHandler<CreateSubscriptionPlanCommand, Result<Guid>>
{
    private const string DuplicateCodeError = "Un plan avec ce code existe déjà.";

    private readonly ISubscriptionPlanRepository _planRepository;

    public CreateSubscriptionPlanCommandHandler(ISubscriptionPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<Result<Guid>> Handle(CreateSubscriptionPlanCommand command, CancellationToken cancellationToken)
    {
        var existing = await _planRepository.GetByCodeAsync(command.Code, cancellationToken);

        if (existing is not null)
        {
            return Result<Guid>.Failure(DuplicateCodeError, ErrorType.Conflict);
        }

        SubscriptionPlan plan;

        try
        {
            plan = SubscriptionPlan.Create(
                command.Name, command.Code, command.Description, command.TargetRole, command.Price, command.Currency,
                command.BillingPeriod, command.TrialPeriodDays, command.Features, command.Limits, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        var added = await _planRepository.TryAddAsync(plan, cancellationToken);

        return added ? Result<Guid>.Success(plan.Id) : Result<Guid>.Failure(DuplicateCodeError, ErrorType.Conflict);
    }
}

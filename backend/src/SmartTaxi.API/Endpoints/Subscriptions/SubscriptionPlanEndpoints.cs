using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Subscriptions;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Subscriptions.Queries.GetAvailableSubscriptionPlans;
using SmartTaxi.Application.Subscriptions.Queries.GetSubscriptionPlanById;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.API.Endpoints.Subscriptions;

public static class SubscriptionPlanEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionPlanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/subscriptions/plans").WithTags("Subscription Plans")
            .RequireAuthorization(Permissions.SubscriptionPlansRead);

        group.MapGet("/", GetAvailableAsync)
            .WithName("GetAvailableSubscriptionPlans").Produces<IReadOnlyCollection<SubscriptionPlanResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{planId:guid}", GetByIdAsync)
            .WithName("GetSubscriptionPlanById").Produces<SubscriptionPlanResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Ok<IReadOnlyCollection<SubscriptionPlanResponse>>> GetAvailableAsync(
        UserRole targetRole, GetAvailableSubscriptionPlansQueryHandler handler, CancellationToken cancellationToken)
    {
        var plans = await handler.Handle(new GetAvailableSubscriptionPlansQuery(targetRole), cancellationToken);
        IReadOnlyCollection<SubscriptionPlanResponse> response = plans.Select(SubscriptionPlanResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<SubscriptionPlanResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid planId, GetSubscriptionPlanByIdQueryHandler handler, CancellationToken cancellationToken)
    {
        var plan = await handler.Handle(new GetSubscriptionPlanByIdQuery(planId), cancellationToken);
        return plan is null
            ? TypedResults.Problem(detail: "Plan introuvable.", statusCode: StatusCodes.Status404NotFound)
            : TypedResults.Ok(SubscriptionPlanResponse.FromEntity(plan));
    }
}

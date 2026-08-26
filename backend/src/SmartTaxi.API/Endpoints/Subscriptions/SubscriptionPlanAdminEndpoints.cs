using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Subscriptions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Subscriptions.Commands.ActivateSubscriptionPlan;
using SmartTaxi.Application.Subscriptions.Commands.CreateSubscriptionPlan;
using SmartTaxi.Application.Subscriptions.Commands.DeactivateSubscriptionPlan;
using SmartTaxi.Application.Subscriptions.Commands.UpdateSubscriptionPlan;
using SmartTaxi.Application.Subscriptions.Queries.GetAllSubscriptionPlans;

namespace SmartTaxi.API.Endpoints.Subscriptions;

public static class SubscriptionPlanAdminEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionPlanAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/subscriptions/plans").WithTags("Subscription Plans")
            .RequireAuthorization(Permissions.SubscriptionPlansManage);

        group.MapPost("/", CreateAsync)
            .WithName("CreateSubscriptionPlan").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{planId:guid}", UpdateAsync)
            .WithName("UpdateSubscriptionPlan").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", GetAllAsync)
            .WithName("GetAllSubscriptionPlans").Produces<PagedResult<SubscriptionPlanResponse>>(StatusCodes.Status200OK);

        group.MapPost("/{planId:guid}/activate", ActivateAsync)
            .WithName("ActivateSubscriptionPlan").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{planId:guid}/deactivate", DeactivateAsync)
            .WithName("DeactivateSubscriptionPlan").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> CreateAsync(
        CreateSubscriptionPlanRequest request, CreateSubscriptionPlanCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CreateSubscriptionPlanCommand(
                request.Name, request.Code, request.Description, request.TargetRole, request.Price, request.Currency,
                request.BillingPeriod, request.TrialPeriodDays, request.Features, request.Limits),
            cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> UpdateAsync(
        Guid planId, UpdateSubscriptionPlanRequest request, UpdateSubscriptionPlanCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new UpdateSubscriptionPlanCommand(planId, request.Name, request.Description, request.Price, request.Features, request.Limits),
            cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Ok<PagedResult<SubscriptionPlanResponse>>> GetAllAsync(
        int pageNumber, int pageSize, GetAllSubscriptionPlansQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetAllSubscriptionPlansQuery(pageNumber, pageSize), cancellationToken);
        var response = new PagedResult<SubscriptionPlanResponse>(
            result.Items.Select(SubscriptionPlanResponse.FromEntity).ToList(), result.TotalCount, result.PageNumber, result.PageSize);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ActivateAsync(
        Guid planId, ActivateSubscriptionPlanCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ActivateSubscriptionPlanCommand(planId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeactivateAsync(
        Guid planId, DeactivateSubscriptionPlanCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new DeactivateSubscriptionPlanCommand(planId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Subscriptions;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Subscriptions.Commands.CancelSubscription;
using SmartTaxi.Application.Subscriptions.Commands.ChangeSubscriptionPlan;
using SmartTaxi.Application.Subscriptions.Commands.RenewSubscription;
using SmartTaxi.Application.Subscriptions.Commands.Subscribe;
using SmartTaxi.Application.Subscriptions.Queries.GetMySubscription;
using SmartTaxi.Application.Subscriptions.Queries.GetSubscriptionHistory;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.API.Endpoints.Subscriptions;

public static class SubscriptionEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/subscriptions").WithTags("Subscriptions");

        group.MapPost("/", SubscribeAsync).RequireAuthorization(Permissions.SubscriptionCreate)
            .WithName("Subscribe").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/me", GetMineAsync).RequireAuthorization(Permissions.SubscriptionReadOwn)
            .WithName("GetMySubscription").Produces<SubscriptionResponse?>(StatusCodes.Status200OK);

        group.MapGet("/me/history", GetMyHistoryAsync).RequireAuthorization(Permissions.SubscriptionReadOwn)
            .WithName("GetMySubscriptionHistory").Produces<IReadOnlyCollection<SubscriptionResponse>>(StatusCodes.Status200OK);

        group.MapPost("/{subscriptionId:guid}/renew", RenewAsync).RequireAuthorization(Permissions.SubscriptionRenewOwn)
            .WithName("RenewSubscription").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{subscriptionId:guid}/cancel", CancelAsync).RequireAuthorization(Permissions.SubscriptionCancelOwn)
            .WithName("CancelSubscription").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{subscriptionId:guid}/change-plan", ChangePlanAsync).RequireAuthorization(Permissions.SubscriptionCreate)
            .WithName("ChangeSubscriptionPlan").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static IReadOnlyCollection<UserRole> CurrentUserRoles(ClaimsPrincipal currentUser) =>
        currentUser.FindAll("role").Select(claim => Enum.Parse<UserRole>(claim.Value)).ToList();

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> SubscribeAsync(
        SubscribeRequest request, ClaimsPrincipal currentUser, SubscribeCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new SubscribeCommand(CurrentUserId(currentUser), request.PlanId, request.AutoRenew, CurrentUserRoles(currentUser)),
            cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Ok<SubscriptionResponse?>> GetMineAsync(
        UserRole targetRole, ClaimsPrincipal currentUser, GetMySubscriptionQueryHandler handler, CancellationToken cancellationToken)
    {
        var subscription = await handler.Handle(new GetMySubscriptionQuery(CurrentUserId(currentUser), targetRole), cancellationToken);
        SubscriptionResponse? response = subscription is null ? null : SubscriptionResponse.FromEntity(subscription);
        return TypedResults.Ok<SubscriptionResponse?>(response);
    }

    private static async Task<Ok<IReadOnlyCollection<SubscriptionResponse>>> GetMyHistoryAsync(
        ClaimsPrincipal currentUser, GetSubscriptionHistoryQueryHandler handler, CancellationToken cancellationToken)
    {
        var history = await handler.Handle(new GetSubscriptionHistoryQuery(CurrentUserId(currentUser)), cancellationToken);
        IReadOnlyCollection<SubscriptionResponse> response = history.Select(SubscriptionResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RenewAsync(
        Guid subscriptionId, ClaimsPrincipal currentUser, RenewSubscriptionCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new RenewSubscriptionCommand(subscriptionId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CancelAsync(
        Guid subscriptionId, ClaimsPrincipal currentUser, CancelSubscriptionCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CancelSubscriptionCommand(subscriptionId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ChangePlanAsync(
        Guid subscriptionId, ChangeSubscriptionPlanRequest request, ClaimsPrincipal currentUser,
        ChangeSubscriptionPlanCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new ChangeSubscriptionPlanCommand(subscriptionId, CurrentUserId(currentUser), request.NewPlanId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}

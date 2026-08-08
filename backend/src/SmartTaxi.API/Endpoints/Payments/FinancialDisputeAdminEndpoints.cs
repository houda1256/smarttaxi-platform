using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Payments;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Payments.Disputes.Commands.EscalateFinancialDispute;
using SmartTaxi.Application.Payments.Disputes.Commands.RejectFinancialDispute;
using SmartTaxi.Application.Payments.Disputes.Commands.ResolveFinancialDispute;
using SmartTaxi.Application.Payments.Disputes.Commands.StartReviewFinancialDispute;
using SmartTaxi.Application.Payments.Disputes.Queries.GetFinancialDisputeById;
using SmartTaxi.Application.Payments.Disputes.Queries.GetFinancialDisputesForReview;
using SmartTaxi.Domain.Payments.Disputes.Enums;

namespace SmartTaxi.API.Endpoints.Payments;

public static class FinancialDisputeAdminEndpoints
{
    public static IEndpointRouteBuilder MapFinancialDisputeAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/finance/disputes").WithTags("Financial Dispute Administration").RequireAuthorization(Permissions.FinanceDisputesManage);

        group.MapGet("/", GetForReviewAsync)
            .WithName("GetFinancialDisputesForReview").Produces<IReadOnlyCollection<FinancialDisputeResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{disputeId:guid}", GetByIdAsync)
            .WithName("GetFinancialDisputeByIdAdmin").Produces<FinancialDisputeResponse>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{disputeId:guid}/start-review", StartReviewAsync)
            .WithName("StartReviewFinancialDispute").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{disputeId:guid}/resolve", ResolveAsync)
            .WithName("ResolveFinancialDispute").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{disputeId:guid}/reject", RejectAsync)
            .WithName("RejectFinancialDispute").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{disputeId:guid}/escalate", EscalateAsync)
            .WithName("EscalateFinancialDispute").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Ok<IReadOnlyCollection<FinancialDisputeResponse>>> GetForReviewAsync(
        FinancialDisputeStatus? status, int pageNumber, int pageSize, GetFinancialDisputesForReviewQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetFinancialDisputesForReviewQuery(status, pageNumber, pageSize), cancellationToken);
        IReadOnlyCollection<FinancialDisputeResponse> response = result.Items.Select(FinancialDisputeResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<FinancialDisputeResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid disputeId, GetFinancialDisputeByIdQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetFinancialDisputeByIdQuery(disputeId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(FinancialDisputeResponse.FromEntity(result.Value!)) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> StartReviewAsync(
        Guid disputeId, ClaimsPrincipal currentUser, StartReviewFinancialDisputeCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new StartReviewFinancialDisputeCommand(CurrentUserId(currentUser), disputeId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ResolveAsync(
        Guid disputeId, ResolveFinancialDisputeRequest request, ClaimsPrincipal currentUser, ResolveFinancialDisputeCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new ResolveFinancialDisputeCommand(CurrentUserId(currentUser), disputeId, request.Resolution, request.Outcome), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RejectAsync(
        Guid disputeId, RejectFinancialDisputeRequest request, ClaimsPrincipal currentUser, RejectFinancialDisputeCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new RejectFinancialDisputeCommand(CurrentUserId(currentUser), disputeId, request.Resolution), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> EscalateAsync(
        Guid disputeId, ClaimsPrincipal currentUser, EscalateFinancialDisputeCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new EscalateFinancialDisputeCommand(CurrentUserId(currentUser), disputeId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}

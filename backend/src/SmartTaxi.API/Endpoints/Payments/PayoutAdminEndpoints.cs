using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Payments;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Payments.Payouts.Commands.ApprovePayout;
using SmartTaxi.Application.Payments.Payouts.Commands.CancelPayout;
using SmartTaxi.Application.Payments.Payouts.Commands.CompletePayout;
using SmartTaxi.Application.Payments.Payouts.Commands.FailPayout;
using SmartTaxi.Application.Payments.Payouts.Commands.RejectPayout;
using SmartTaxi.Application.Payments.Payouts.Commands.StartProcessingPayout;
using SmartTaxi.Application.Payments.Payouts.Queries.GetAdminPayouts;
using SmartTaxi.Application.Payments.Payouts.Queries.GetPayoutById;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Payouts.Enums;

namespace SmartTaxi.API.Endpoints.Payments;

public static class PayoutAdminEndpoints
{
    public static IEndpointRouteBuilder MapPayoutAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/finance/payouts").WithTags("Payout Administration").RequireAuthorization(Permissions.FinancePayoutsManage);

        group.MapGet("/", GetAllAsync)
            .WithName("GetAdminPayouts").Produces<IReadOnlyCollection<PayoutResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{payoutId:guid}", GetByIdAsync)
            .WithName("GetPayoutByIdAdmin").Produces<PayoutResponse>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{payoutId:guid}/approve", ApproveAsync)
            .WithName("ApprovePayout").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{payoutId:guid}/reject", RejectAsync)
            .WithName("RejectPayout").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{payoutId:guid}/start-processing", StartProcessingAsync)
            .WithName("StartProcessingPayout").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{payoutId:guid}/complete", CompleteAsync)
            .WithName("CompletePayout").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{payoutId:guid}/fail", FailAsync)
            .WithName("FailPayout").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{payoutId:guid}/cancel", CancelAsync)
            .WithName("CancelPayoutAdmin").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Ok<IReadOnlyCollection<PayoutResponse>>> GetAllAsync(
        FinancialAccountType? beneficiaryType, PayoutStatus? status, DateTime? fromUtc, DateTime? toUtc, int pageNumber, int pageSize,
        GetAdminPayoutsQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new GetAdminPayoutsQuery(beneficiaryType, status, fromUtc, toUtc, pageNumber, pageSize), cancellationToken);
        IReadOnlyCollection<PayoutResponse> response = result.Items.Select(PayoutResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<PayoutResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid payoutId, GetPayoutByIdQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetPayoutByIdQuery(payoutId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(PayoutResponse.FromEntity(result.Value!)) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ApproveAsync(
        Guid payoutId, ClaimsPrincipal currentUser, ApprovePayoutCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ApprovePayoutCommand(CurrentUserId(currentUser), payoutId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RejectAsync(
        Guid payoutId, ClaimsPrincipal currentUser, RejectPayoutCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new RejectPayoutCommand(CurrentUserId(currentUser), payoutId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> StartProcessingAsync(
        Guid payoutId, ClaimsPrincipal currentUser, StartProcessingPayoutCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new StartProcessingPayoutCommand(CurrentUserId(currentUser), payoutId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CompleteAsync(
        Guid payoutId, ClaimsPrincipal currentUser, CompletePayoutCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CompletePayoutCommand(CurrentUserId(currentUser), payoutId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> FailAsync(
        Guid payoutId, FailPayoutRequest request, ClaimsPrincipal currentUser, FailPayoutCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new FailPayoutCommand(CurrentUserId(currentUser), payoutId, request.Reason), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CancelAsync(
        Guid payoutId, ClaimsPrincipal currentUser, CancelPayoutCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CancelPayoutCommand(CurrentUserId(currentUser), payoutId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Payments;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Payments.CashDeclarations.Commands.ApproveCashDeclaration;
using SmartTaxi.Application.Payments.CashDeclarations.Commands.DisputeCashDeclaration;
using SmartTaxi.Application.Payments.CashDeclarations.Commands.SettleCashDeclaration;
using SmartTaxi.Application.Payments.CashDeclarations.Commands.StartReviewCashDeclaration;
using SmartTaxi.Application.Payments.CashDeclarations.Queries.GetCashDeclarationById;
using SmartTaxi.Application.Payments.CashDeclarations.Queries.GetCashDeclarationsForReview;
using SmartTaxi.Domain.Payments.CashDeclarations.Enums;

namespace SmartTaxi.API.Endpoints.Payments;

public static class CashDeclarationAdminEndpoints
{
    public static IEndpointRouteBuilder MapCashDeclarationAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/finance/cash-declarations").WithTags("Cash Declaration Review").RequireAuthorization(Permissions.FinanceCashDeclarationsReview);

        group.MapGet("/", GetForReviewAsync)
            .WithName("GetCashDeclarationsForReview").Produces<IReadOnlyCollection<CashDeclarationResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{declarationId:guid}", GetByIdAsync)
            .WithName("GetCashDeclarationByIdAdmin").Produces<CashDeclarationResponse>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{declarationId:guid}/start-review", StartReviewAsync)
            .WithName("StartReviewCashDeclaration").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{declarationId:guid}/approve", ApproveAsync)
            .WithName("ApproveCashDeclaration").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{declarationId:guid}/dispute", DisputeAsync)
            .WithName("DisputeCashDeclaration").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{declarationId:guid}/settle", SettleAsync)
            .WithName("SettleCashDeclaration").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Ok<IReadOnlyCollection<CashDeclarationResponse>>> GetForReviewAsync(
        CashDeclarationStatus? status, int pageNumber, int pageSize, GetCashDeclarationsForReviewQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetCashDeclarationsForReviewQuery(status, pageNumber, pageSize), cancellationToken);
        IReadOnlyCollection<CashDeclarationResponse> response = result.Items.Select(CashDeclarationResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<CashDeclarationResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid declarationId, GetCashDeclarationByIdQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetCashDeclarationByIdQuery(declarationId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(CashDeclarationResponse.FromEntity(result.Value!)) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> StartReviewAsync(
        Guid declarationId, ClaimsPrincipal currentUser, StartReviewCashDeclarationCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new StartReviewCashDeclarationCommand(CurrentUserId(currentUser), declarationId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ApproveAsync(
        Guid declarationId, ClaimsPrincipal currentUser, ApproveCashDeclarationCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ApproveCashDeclarationCommand(CurrentUserId(currentUser), declarationId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DisputeAsync(
        Guid declarationId, ClaimsPrincipal currentUser, DisputeCashDeclarationCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new DisputeCashDeclarationCommand(CurrentUserId(currentUser), declarationId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> SettleAsync(
        Guid declarationId, ClaimsPrincipal currentUser, SettleCashDeclarationCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new SettleCashDeclarationCommand(CurrentUserId(currentUser), declarationId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}

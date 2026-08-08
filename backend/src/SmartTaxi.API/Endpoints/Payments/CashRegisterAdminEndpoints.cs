using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Payments;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Payments.CashRegister.Commands.DisputeCashRegisterSession;
using SmartTaxi.Application.Payments.CashRegister.Commands.ReconcileCashRegisterSession;
using SmartTaxi.Application.Payments.CashRegister.Queries.GetCashRegisterSessionById;
using SmartTaxi.Application.Payments.CashRegister.Queries.GetMovementsForSession;

namespace SmartTaxi.API.Endpoints.Payments;

public static class CashRegisterAdminEndpoints
{
    public static IEndpointRouteBuilder MapCashRegisterAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/finance/cash-register").WithTags("Cash Register Administration").RequireAuthorization(Permissions.FinanceCashRegisterAudit);

        group.MapGet("/sessions/{sessionId:guid}", GetSessionByIdAsync)
            .WithName("GetCashRegisterSessionByIdAdmin").Produces<CashRegisterSessionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/sessions/{sessionId:guid}/movements", GetMovementsAsync)
            .WithName("GetCashMovementsForSessionAdmin").Produces<IReadOnlyCollection<CashMovementResponse>>(StatusCodes.Status200OK);

        group.MapPost("/sessions/{sessionId:guid}/reconcile", ReconcileAsync)
            .WithName("ReconcileCashRegisterSession").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/sessions/{sessionId:guid}/dispute", DisputeAsync)
            .WithName("DisputeCashRegisterSession").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Results<Ok<CashRegisterSessionResponse>, ProblemHttpResult>> GetSessionByIdAsync(
        Guid sessionId, GetCashRegisterSessionByIdQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetCashRegisterSessionByIdQuery(sessionId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(CashRegisterSessionResponse.FromEntity(result.Value!)) : result.ToProblem();
    }

    private static async Task<Ok<IReadOnlyCollection<CashMovementResponse>>> GetMovementsAsync(
        Guid sessionId, GetMovementsForSessionQueryHandler handler, CancellationToken cancellationToken)
    {
        var movements = await handler.Handle(new GetMovementsForSessionQuery(sessionId), cancellationToken);
        IReadOnlyCollection<CashMovementResponse> response = movements.Select(CashMovementResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ReconcileAsync(
        Guid sessionId, ClaimsPrincipal currentUser, ReconcileCashRegisterSessionCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ReconcileCashRegisterSessionCommand(sessionId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DisputeAsync(
        Guid sessionId, DisputeCashRegisterSessionRequest request, ClaimsPrincipal currentUser, DisputeCashRegisterSessionCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new DisputeCashRegisterSessionCommand(sessionId, CurrentUserId(currentUser), request.Reason), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}

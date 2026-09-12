using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Payments;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Payments.CashRegister.Abstractions;
using SmartTaxi.Application.Payments.CashRegister.Commands.CloseCashRegisterSession;
using SmartTaxi.Application.Payments.CashRegister.Commands.OpenCashRegisterSession;
using SmartTaxi.Application.Payments.CashRegister.Commands.RecordCashMovement;
using SmartTaxi.Application.Payments.CashRegister.Commands.RegisterCashRegister;
using SmartTaxi.Application.Payments.CashRegister.Queries.GetCashRegisterSessionById;
using SmartTaxi.Application.Payments.CashRegister.Queries.GetMovementsForSession;
using SmartTaxi.Application.Payments.CashRegister.Queries.GetSessionsForOwner;
using SmartTaxi.Domain.Payments.CashRegister.Enums;

namespace SmartTaxi.API.Endpoints.Payments;

/// <summary>
/// Self-service cash register management, scoped to the caller's own
/// CashRegisterBox(es) — every session/movement action re-verifies ownership
/// through CashRegisterBox.OwnerId before acting, since none of the
/// Application-layer handlers check ownership themselves (they trust the
/// caller, exactly like PaymentEndpoints/PayoutEndpoints).
/// </summary>
public static class CashRegisterEndpoints
{
    public static IEndpointRouteBuilder MapCashRegisterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/finance/cash-register").WithTags("Cash Register").RequireAuthorization(Permissions.FinanceCashRegisterManageOwn);

        group.MapPost("/registers", RegisterAsync)
            .WithName("RegisterCashRegister").Produces<Guid>(StatusCodes.Status200OK);

        group.MapGet("/registers/mine", GetMyRegistersAsync)
            .WithName("GetMyCashRegisters").Produces<IReadOnlyCollection<CashRegisterResponse>>(StatusCodes.Status200OK);

        group.MapPost("/sessions", OpenSessionAsync)
            .WithName("OpenCashRegisterSession").Produces<Guid>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/sessions/mine", GetMySessionsAsync)
            .WithName("GetMyCashRegisterSessions").Produces<IReadOnlyCollection<CashRegisterSessionResponse>>(StatusCodes.Status200OK);

        group.MapGet("/sessions/{sessionId:guid}", GetSessionByIdAsync)
            .WithName("GetCashRegisterSessionById").Produces<CashRegisterSessionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden).ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/sessions/{sessionId:guid}/movements", GetMovementsAsync)
            .WithName("GetCashMovementsForSession").Produces<IReadOnlyCollection<CashMovementResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden).ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/sessions/{sessionId:guid}/movements", RecordMovementAsync)
            .WithName("RecordCashMovement").Produces<Guid>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/sessions/{sessionId:guid}/close", CloseSessionAsync)
            .WithName("CloseCashRegisterSession").Produces<decimal>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden).ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Ok<Guid>> RegisterAsync(
        RegisterCashRegisterRequest request, ClaimsPrincipal currentUser, RegisterCashRegisterCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new RegisterCashRegisterCommand(CurrentUserId(currentUser), request.Label), cancellationToken);
        return TypedResults.Ok(result.Value);
    }

    private static async Task<Ok<IReadOnlyCollection<CashRegisterResponse>>> GetMyRegistersAsync(
        ClaimsPrincipal currentUser, ICashRegisterRepository cashRegisterRepository, CancellationToken cancellationToken)
    {
        var registers = await cashRegisterRepository.GetForOwnerAsync(CurrentUserId(currentUser), cancellationToken);
        IReadOnlyCollection<CashRegisterResponse> response = registers.Select(CashRegisterResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> OpenSessionAsync(
        OpenCashRegisterSessionRequest request, ClaimsPrincipal currentUser, ICashRegisterRepository cashRegisterRepository,
        OpenCashRegisterSessionCommandHandler handler, CancellationToken cancellationToken)
    {
        var forbidden = await VerifyRegisterOwnershipAsync(request.CashRegisterId, CurrentUserId(currentUser), cashRegisterRepository, cancellationToken);

        if (forbidden is not null)
        {
            return forbidden;
        }

        var result = await handler.Handle(
            new OpenCashRegisterSessionCommand(request.CashRegisterId, CurrentUserId(currentUser), request.OpeningBalance), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Ok<IReadOnlyCollection<CashRegisterSessionResponse>>> GetMySessionsAsync(
        CashRegisterSessionStatus? status, int pageNumber, int pageSize, ClaimsPrincipal currentUser,
        GetSessionsForOwnerQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new GetSessionsForOwnerQuery(CurrentUserId(currentUser), status, pageNumber, pageSize), cancellationToken);
        IReadOnlyCollection<CashRegisterSessionResponse> response = result.Items.Select(CashRegisterSessionResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<CashRegisterSessionResponse>, ProblemHttpResult>> GetSessionByIdAsync(
        Guid sessionId, ClaimsPrincipal currentUser, ICashRegisterRepository cashRegisterRepository,
        GetCashRegisterSessionByIdQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetCashRegisterSessionByIdQuery(sessionId), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ToProblem();
        }

        var forbidden = await VerifyRegisterOwnershipAsync(result.Value!.CashRegisterId, CurrentUserId(currentUser), cashRegisterRepository, cancellationToken);
        return forbidden is not null ? forbidden : TypedResults.Ok(CashRegisterSessionResponse.FromEntity(result.Value));
    }

    private static async Task<Results<Ok<IReadOnlyCollection<CashMovementResponse>>, ProblemHttpResult>> GetMovementsAsync(
        Guid sessionId, ClaimsPrincipal currentUser, ICashRegisterRepository cashRegisterRepository, GetCashRegisterSessionByIdQueryHandler sessionHandler,
        GetMovementsForSessionQueryHandler handler, CancellationToken cancellationToken)
    {
        var forbidden = await VerifySessionOwnershipAsync(sessionId, CurrentUserId(currentUser), cashRegisterRepository, sessionHandler, cancellationToken);

        if (forbidden is not null)
        {
            return forbidden;
        }

        var movements = await handler.Handle(new GetMovementsForSessionQuery(sessionId), cancellationToken);
        IReadOnlyCollection<CashMovementResponse> response = movements.Select(CashMovementResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> RecordMovementAsync(
        Guid sessionId, RecordCashMovementRequest request, ClaimsPrincipal currentUser, ICashRegisterRepository cashRegisterRepository,
        GetCashRegisterSessionByIdQueryHandler sessionHandler, RecordCashMovementCommandHandler handler, CancellationToken cancellationToken)
    {
        var forbidden = await VerifySessionOwnershipAsync(sessionId, CurrentUserId(currentUser), cashRegisterRepository, sessionHandler, cancellationToken);

        if (forbidden is not null)
        {
            return forbidden;
        }

        var result = await handler.Handle(
            new RecordCashMovementCommand(sessionId, request.MovementType, request.Amount, request.Description, CurrentUserId(currentUser)),
            cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<decimal>, ProblemHttpResult>> CloseSessionAsync(
        Guid sessionId, CloseCashRegisterSessionRequest request, ClaimsPrincipal currentUser, ICashRegisterRepository cashRegisterRepository,
        GetCashRegisterSessionByIdQueryHandler sessionHandler, CloseCashRegisterSessionCommandHandler handler, CancellationToken cancellationToken)
    {
        var forbidden = await VerifySessionOwnershipAsync(sessionId, CurrentUserId(currentUser), cashRegisterRepository, sessionHandler, cancellationToken);

        if (forbidden is not null)
        {
            return forbidden;
        }

        var result = await handler.Handle(
            new CloseCashRegisterSessionCommand(sessionId, CurrentUserId(currentUser), request.ClosingActualBalance, request.DifferenceReason),
            cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<ProblemHttpResult?> VerifyRegisterOwnershipAsync(
        Guid cashRegisterId, Guid requestingUserId, ICashRegisterRepository cashRegisterRepository, CancellationToken cancellationToken)
    {
        var cashRegister = await cashRegisterRepository.GetByIdAsync(cashRegisterId, cancellationToken);

        if (cashRegister is null)
        {
            return TypedResults.Problem(detail: "Caisse introuvable.", statusCode: StatusCodes.Status404NotFound);
        }

        return cashRegister.OwnerId == requestingUserId
            ? null
            : TypedResults.Problem(detail: "Seul le propriétaire peut gérer cette caisse.", statusCode: StatusCodes.Status403Forbidden);
    }

    private static async Task<ProblemHttpResult?> VerifySessionOwnershipAsync(
        Guid sessionId, Guid requestingUserId, ICashRegisterRepository cashRegisterRepository, GetCashRegisterSessionByIdQueryHandler sessionHandler,
        CancellationToken cancellationToken)
    {
        var sessionResult = await sessionHandler.Handle(new GetCashRegisterSessionByIdQuery(sessionId), cancellationToken);

        if (!sessionResult.IsSuccess)
        {
            return sessionResult.ToProblem();
        }

        return await VerifyRegisterOwnershipAsync(sessionResult.Value!.CashRegisterId, requestingUserId, cashRegisterRepository, cancellationToken);
    }
}

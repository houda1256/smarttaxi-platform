using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Payments;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Payments.Ledger.Abstractions;
using SmartTaxi.Application.Payments.Payouts.Commands.CancelPayout;
using SmartTaxi.Application.Payments.Payouts.Commands.RequestPayout;
using SmartTaxi.Application.Payments.Payouts.Queries.GetMyPayouts;
using SmartTaxi.Application.Payments.Payouts.Queries.GetPayoutById;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Payouts.Enums;

namespace SmartTaxi.API.Endpoints.Payments;

/// <summary>
/// Self-service payout requests, always scoped to the caller's own
/// FinancialAccount — BeneficiaryOwnerReferenceId is always the current
/// user's id, never a client-supplied one, so cross-owner draining is
/// impossible by construction.
/// </summary>
public static class PayoutEndpoints
{
    public static IEndpointRouteBuilder MapPayoutEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/finance/payouts").WithTags("Payouts");

        group.MapPost("/", RequestAsync).RequireAuthorization(Permissions.FinancePayoutsRequestOwn)
            .WithName("RequestPayout").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/mine", GetMineAsync).RequireAuthorization(Permissions.FinancePayoutsReadOwn)
            .WithName("GetMyPayouts").Produces<IReadOnlyCollection<PayoutResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{payoutId:guid}", GetByIdAsync).RequireAuthorization(Permissions.FinancePayoutsReadOwn)
            .WithName("GetPayoutById").Produces<PayoutResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden).ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{payoutId:guid}/cancel", CancelAsync).RequireAuthorization(Permissions.FinancePayoutsRequestOwn)
            .WithName("CancelPayout").Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden).ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> RequestAsync(
        RequestPayoutRequest request, ClaimsPrincipal currentUser, RequestPayoutCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId(currentUser);
        var result = await handler.Handle(
            new RequestPayoutCommand(userId, request.BeneficiaryType, userId, request.Amount, request.Currency, request.Method, request.Frequency),
            cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<IReadOnlyCollection<PayoutResponse>>, ProblemHttpResult>> GetMineAsync(
        FinancialAccountType beneficiaryType, PayoutStatus? status, int pageNumber, int pageSize, ClaimsPrincipal currentUser,
        IFinancialAccountRepository accountRepository, GetMyPayoutsQueryHandler handler, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByTypeAndOwnerAsync(beneficiaryType, CurrentUserId(currentUser), cancellationToken);

        if (account is null)
        {
            return TypedResults.Ok<IReadOnlyCollection<PayoutResponse>>([]);
        }

        var result = await handler.Handle(new GetMyPayoutsQuery(account.Id, status, pageNumber, pageSize), cancellationToken);
        IReadOnlyCollection<PayoutResponse> response = result.Items.Select(PayoutResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<PayoutResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid payoutId, ClaimsPrincipal currentUser, IFinancialAccountRepository accountRepository, GetPayoutByIdQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetPayoutByIdQuery(payoutId), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ToProblem();
        }

        var forbidden = await VerifyOwnershipAsync(result.Value!.BeneficiaryAccountId, CurrentUserId(currentUser), accountRepository, cancellationToken);
        return forbidden is not null ? forbidden : TypedResults.Ok(PayoutResponse.FromEntity(result.Value));
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CancelAsync(
        Guid payoutId, ClaimsPrincipal currentUser, IFinancialAccountRepository accountRepository, GetPayoutByIdQueryHandler getHandler,
        CancelPayoutCommandHandler cancelHandler, CancellationToken cancellationToken)
    {
        var payoutResult = await getHandler.Handle(new GetPayoutByIdQuery(payoutId), cancellationToken);

        if (!payoutResult.IsSuccess)
        {
            return payoutResult.ToProblem();
        }

        var forbidden = await VerifyOwnershipAsync(payoutResult.Value!.BeneficiaryAccountId, CurrentUserId(currentUser), accountRepository, cancellationToken);

        if (forbidden is not null)
        {
            return forbidden;
        }

        var result = await cancelHandler.Handle(new CancelPayoutCommand(CurrentUserId(currentUser), payoutId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<ProblemHttpResult?> VerifyOwnershipAsync(
        Guid beneficiaryAccountId, Guid requestingUserId, IFinancialAccountRepository accountRepository, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(beneficiaryAccountId, cancellationToken);

        if (account is null)
        {
            return TypedResults.Problem(detail: "Compte financier introuvable.", statusCode: StatusCodes.Status404NotFound);
        }

        return account.OwnerReferenceId == requestingUserId
            ? null
            : TypedResults.Problem(detail: "Seul le bénéficiaire peut consulter ce versement.", statusCode: StatusCodes.Status403Forbidden);
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Payments;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Payments.Disputes.Commands.OpenFinancialDispute;
using SmartTaxi.Application.Payments.Disputes.Queries.GetFinancialDisputeById;
using SmartTaxi.Application.Payments.Disputes.Queries.GetMyFinancialDisputes;
using SmartTaxi.Domain.Payments.Disputes.Enums;

namespace SmartTaxi.API.Endpoints.Payments;

/// <summary>Self-service dispute filing — RaisedBy always comes from the caller's own id, never client-supplied.</summary>
public static class FinancialDisputeEndpoints
{
    public static IEndpointRouteBuilder MapFinancialDisputeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/finance/disputes").WithTags("Financial Disputes").RequireAuthorization(Permissions.FinanceDisputesOpenOwn);

        group.MapPost("/", OpenAsync)
            .WithName("OpenFinancialDispute").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/mine", GetMineAsync)
            .WithName("GetMyFinancialDisputes").Produces<IReadOnlyCollection<FinancialDisputeResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{disputeId:guid}", GetByIdAsync)
            .WithName("GetMyFinancialDisputeById").Produces<FinancialDisputeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden).ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> OpenAsync(
        OpenFinancialDisputeRequest request, ClaimsPrincipal currentUser, OpenFinancialDisputeCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new OpenFinancialDisputeCommand(
                CurrentUserId(currentUser), request.Category, request.RelatedPaymentId, request.RelatedInvoiceId, request.RelatedPayoutId,
                request.DisputedAmount, request.Currency, request.Description, request.EvidenceReference),
            cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Ok<IReadOnlyCollection<FinancialDisputeResponse>>> GetMineAsync(
        FinancialDisputeStatus? status, int pageNumber, int pageSize, ClaimsPrincipal currentUser, GetMyFinancialDisputesQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetMyFinancialDisputesQuery(CurrentUserId(currentUser), status, pageNumber, pageSize), cancellationToken);
        IReadOnlyCollection<FinancialDisputeResponse> response = result.Items.Select(FinancialDisputeResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<FinancialDisputeResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid disputeId, ClaimsPrincipal currentUser, GetFinancialDisputeByIdQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetFinancialDisputeByIdQuery(disputeId), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ToProblem();
        }

        if (result.Value!.RaisedBy != CurrentUserId(currentUser))
        {
            return TypedResults.Problem(detail: "Seul l'auteur du litige peut le consulter.", statusCode: StatusCodes.Status403Forbidden);
        }

        return TypedResults.Ok(FinancialDisputeResponse.FromEntity(result.Value));
    }
}

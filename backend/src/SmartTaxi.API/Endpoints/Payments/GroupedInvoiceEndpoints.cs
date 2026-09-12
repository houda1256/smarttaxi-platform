using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Payments;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Payments.GroupedInvoicing.Queries.GetGroupedInvoiceById;
using SmartTaxi.Application.Payments.GroupedInvoicing.Queries.GetGroupedInvoiceLines;
using SmartTaxi.Application.Payments.GroupedInvoicing.Queries.GetGroupedInvoicesForBusinessCustomer;
using SmartTaxi.Application.Payments.BusinessCustomers.Queries.GetMyBusinessCustomer;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Enums;

namespace SmartTaxi.API.Endpoints.Payments;

/// <summary>Self-service invoice visibility for a BusinessCustomer's own authorized employees — every lookup is scoped through the caller's own company, resolved via GetMyBusinessCustomer, never a client-supplied BusinessCustomerId.</summary>
public static class GroupedInvoiceEndpoints
{
    public static IEndpointRouteBuilder MapGroupedInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/finance/grouped-invoices").WithTags("Grouped Invoicing").RequireAuthorization(Permissions.FinanceBusinessCustomersReadOwn);

        group.MapGet("/mine", GetMineAsync)
            .WithName("GetMyGroupedInvoices").Produces<IReadOnlyCollection<GroupedInvoiceResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{groupedInvoiceId:guid}", GetByIdAsync)
            .WithName("GetMyGroupedInvoiceById").Produces<GroupedInvoiceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden).ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{groupedInvoiceId:guid}/lines", GetLinesAsync)
            .WithName("GetMyGroupedInvoiceLines").Produces<IReadOnlyCollection<GroupedInvoiceLineResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden).ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Results<Ok<IReadOnlyCollection<GroupedInvoiceResponse>>, ProblemHttpResult>> GetMineAsync(
        GroupedInvoiceStatus? status, int pageNumber, int pageSize, ClaimsPrincipal currentUser, GetMyBusinessCustomerQueryHandler customerHandler,
        GetGroupedInvoicesForBusinessCustomerQueryHandler invoicesHandler, CancellationToken cancellationToken)
    {
        var customerResult = await customerHandler.Handle(new GetMyBusinessCustomerQuery(CurrentUserId(currentUser)), cancellationToken);

        if (!customerResult.IsSuccess)
        {
            return customerResult.ToProblem();
        }

        var result = await invoicesHandler.Handle(
            new GetGroupedInvoicesForBusinessCustomerQuery(customerResult.Value!.Id, status, pageNumber, pageSize), cancellationToken);
        IReadOnlyCollection<GroupedInvoiceResponse> response = result.Items.Select(GroupedInvoiceResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<GroupedInvoiceResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid groupedInvoiceId, ClaimsPrincipal currentUser, GetMyBusinessCustomerQueryHandler customerHandler,
        GetGroupedInvoiceByIdQueryHandler invoiceHandler, CancellationToken cancellationToken)
    {
        var invoiceResult = await invoiceHandler.Handle(new GetGroupedInvoiceByIdQuery(groupedInvoiceId), cancellationToken);

        if (!invoiceResult.IsSuccess)
        {
            return invoiceResult.ToProblem();
        }

        var forbidden = await VerifyOwnershipAsync(invoiceResult.Value!.BusinessCustomerId, CurrentUserId(currentUser), customerHandler, cancellationToken);
        return forbidden is not null ? forbidden : TypedResults.Ok(GroupedInvoiceResponse.FromEntity(invoiceResult.Value));
    }

    private static async Task<Results<Ok<IReadOnlyCollection<GroupedInvoiceLineResponse>>, ProblemHttpResult>> GetLinesAsync(
        Guid groupedInvoiceId, ClaimsPrincipal currentUser, GetMyBusinessCustomerQueryHandler customerHandler,
        GetGroupedInvoiceByIdQueryHandler invoiceHandler, GetGroupedInvoiceLinesQueryHandler linesHandler, CancellationToken cancellationToken)
    {
        var invoiceResult = await invoiceHandler.Handle(new GetGroupedInvoiceByIdQuery(groupedInvoiceId), cancellationToken);

        if (!invoiceResult.IsSuccess)
        {
            return invoiceResult.ToProblem();
        }

        var forbidden = await VerifyOwnershipAsync(invoiceResult.Value!.BusinessCustomerId, CurrentUserId(currentUser), customerHandler, cancellationToken);

        if (forbidden is not null)
        {
            return forbidden;
        }

        var lines = await linesHandler.Handle(new GetGroupedInvoiceLinesQuery(groupedInvoiceId), cancellationToken);
        IReadOnlyCollection<GroupedInvoiceLineResponse> response = lines.Select(GroupedInvoiceLineResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<ProblemHttpResult?> VerifyOwnershipAsync(
        Guid businessCustomerId, Guid requestingUserId, GetMyBusinessCustomerQueryHandler customerHandler, CancellationToken cancellationToken)
    {
        var customerResult = await customerHandler.Handle(new GetMyBusinessCustomerQuery(requestingUserId), cancellationToken);

        if (!customerResult.IsSuccess || customerResult.Value!.Id != businessCustomerId)
        {
            return TypedResults.Problem(detail: "Seul le client entreprise concerné peut consulter cette facture.", statusCode: StatusCodes.Status403Forbidden);
        }

        return null;
    }
}

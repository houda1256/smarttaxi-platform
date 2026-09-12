using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Payments;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Payments.GroupedInvoicing.Commands.CancelGroupedInvoice;
using SmartTaxi.Application.Payments.GroupedInvoicing.Commands.GenerateGroupedInvoice;
using SmartTaxi.Application.Payments.GroupedInvoicing.Commands.MarkGroupedInvoicePaid;
using SmartTaxi.Application.Payments.GroupedInvoicing.Queries.GetGroupedInvoiceById;
using SmartTaxi.Application.Payments.GroupedInvoicing.Queries.GetGroupedInvoiceLines;
using SmartTaxi.Application.Payments.GroupedInvoicing.Queries.GetGroupedInvoicesForBusinessCustomer;
using SmartTaxi.Application.Payments.GroupedInvoicing.Queries.GetOverdueGroupedInvoices;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Enums;

namespace SmartTaxi.API.Endpoints.Payments;

/// <summary>Generating/settling grouped invoices for a corporate client is billing-team work — reuses the finance.business-customers.manage permission rather than inventing a dedicated one not already defined for this phase.</summary>
public static class GroupedInvoiceAdminEndpoints
{
    public static IEndpointRouteBuilder MapGroupedInvoiceAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/finance/grouped-invoices").WithTags("Grouped Invoicing").RequireAuthorization(Permissions.FinanceBusinessCustomersManage);

        group.MapPost("/", GenerateAsync)
            .WithName("GenerateGroupedInvoice").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{groupedInvoiceId:guid}", GetByIdAsync)
            .WithName("GetGroupedInvoiceByIdAdmin").Produces<GroupedInvoiceResponse>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{groupedInvoiceId:guid}/lines", GetLinesAsync)
            .WithName("GetGroupedInvoiceLinesAdmin").Produces<IReadOnlyCollection<GroupedInvoiceLineResponse>>(StatusCodes.Status200OK);

        group.MapGet("/business-customer/{businessCustomerId:guid}", GetForBusinessCustomerAsync)
            .WithName("GetGroupedInvoicesForBusinessCustomer").Produces<IReadOnlyCollection<GroupedInvoiceResponse>>(StatusCodes.Status200OK);

        group.MapGet("/overdue", GetOverdueAsync)
            .WithName("GetOverdueGroupedInvoices").Produces<IReadOnlyCollection<GroupedInvoiceResponse>>(StatusCodes.Status200OK);

        group.MapPost("/{groupedInvoiceId:guid}/mark-paid", MarkPaidAsync)
            .WithName("MarkGroupedInvoicePaid").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{groupedInvoiceId:guid}/cancel", CancelAsync)
            .WithName("CancelGroupedInvoice").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> GenerateAsync(
        GenerateGroupedInvoiceRequest request, GenerateGroupedInvoiceCommandHandler handler, CancellationToken cancellationToken)
    {
        var rides = request.Rides.Select(r => new GroupedInvoiceRideLine(r.RideId, r.RideNumber, r.Amount)).ToList();
        var result = await handler.Handle(
            new GenerateGroupedInvoiceCommand(
                request.BusinessCustomerId, request.PeriodType, request.PeriodStart, request.PeriodEnd, request.Currency,
                request.DueDate, rides),
            cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<GroupedInvoiceResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid groupedInvoiceId, GetGroupedInvoiceByIdQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetGroupedInvoiceByIdQuery(groupedInvoiceId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(GroupedInvoiceResponse.FromEntity(result.Value!)) : result.ToProblem();
    }

    private static async Task<Ok<IReadOnlyCollection<GroupedInvoiceLineResponse>>> GetLinesAsync(
        Guid groupedInvoiceId, GetGroupedInvoiceLinesQueryHandler handler, CancellationToken cancellationToken)
    {
        var lines = await handler.Handle(new GetGroupedInvoiceLinesQuery(groupedInvoiceId), cancellationToken);
        IReadOnlyCollection<GroupedInvoiceLineResponse> response = lines.Select(GroupedInvoiceLineResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Ok<IReadOnlyCollection<GroupedInvoiceResponse>>> GetForBusinessCustomerAsync(
        Guid businessCustomerId, GroupedInvoiceStatus? status, int pageNumber, int pageSize,
        GetGroupedInvoicesForBusinessCustomerQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetGroupedInvoicesForBusinessCustomerQuery(businessCustomerId, status, pageNumber, pageSize), cancellationToken);
        IReadOnlyCollection<GroupedInvoiceResponse> response = result.Items.Select(GroupedInvoiceResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Ok<IReadOnlyCollection<GroupedInvoiceResponse>>> GetOverdueAsync(
        DateTime asOfUtc, GetOverdueGroupedInvoicesQueryHandler handler, CancellationToken cancellationToken)
    {
        var overdue = await handler.Handle(new GetOverdueGroupedInvoicesQuery(asOfUtc), cancellationToken);
        IReadOnlyCollection<GroupedInvoiceResponse> response = overdue.Select(GroupedInvoiceResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> MarkPaidAsync(
        Guid groupedInvoiceId, MarkGroupedInvoicePaidCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new MarkGroupedInvoicePaidCommand(groupedInvoiceId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CancelAsync(
        Guid groupedInvoiceId, CancelGroupedInvoiceCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CancelGroupedInvoiceCommand(groupedInvoiceId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Support;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Support.Commands.AddAdminTicketMessage;
using SmartTaxi.Application.Support.Commands.AddInternalNote;
using SmartTaxi.Application.Support.Commands.AssignSupportTicket;
using SmartTaxi.Application.Support.Commands.EscalateTicketToIncident;
using SmartTaxi.Application.Support.Commands.MarkWaitingForCustomer;
using SmartTaxi.Application.Support.Commands.ReassignSupportTicket;
using SmartTaxi.Application.Support.Commands.ResolveSupportTicket;
using SmartTaxi.Application.Support.Commands.StartSupportTicket;
using SmartTaxi.Application.Support.Queries.GetAllSupportTickets;
using SmartTaxi.Application.Support.Queries.GetSupportTicketDetailsAdmin;

namespace SmartTaxi.API.Endpoints.Support;

/// <summary>Admin-only surface. Close/Reopen are mapped here too, pointing at the exact same handlers as SupportTicketEndpoints — see that class's remarks for why.</summary>
public static class SupportTicketAdminEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapSupportTicketAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var tickets = app.MapGroup("/api/admin/support/tickets").WithTags("Support Admin - Tickets");

        tickets.MapGet("/", GetAllSupportTicketsAsync).RequireAuthorization(Permissions.SupportTicketsReadAll)
            .WithName("GetAllSupportTickets").Produces<PagedResult<SupportTicketResponse>>(StatusCodes.Status200OK);

        tickets.MapGet("/{ticketId:guid}", GetSupportTicketDetailsAdminAsync).RequireAuthorization(Permissions.SupportTicketsReadAll)
            .WithName("GetSupportTicketDetailsAdmin").Produces<SupportTicketDetailsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        tickets.MapPost("/{ticketId:guid}/assign", AssignSupportTicketAsync).RequireAuthorization(Permissions.SupportTicketsManageAll)
            .WithName("AssignSupportTicket").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        tickets.MapPost("/{ticketId:guid}/reassign", ReassignSupportTicketAsync).RequireAuthorization(Permissions.SupportTicketsManageAll)
            .WithName("ReassignSupportTicket").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        tickets.MapPost("/{ticketId:guid}/start", StartSupportTicketAsync).RequireAuthorization(Permissions.SupportTicketsManageAll)
            .WithName("StartSupportTicket").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        tickets.MapPost("/{ticketId:guid}/messages", AddAdminTicketMessageAsync).RequireAuthorization(Permissions.SupportTicketsManageAll)
            .WithName("AddAdminTicketMessage").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        tickets.MapPost("/{ticketId:guid}/internal-notes", AddInternalNoteAsync).RequireAuthorization(Permissions.SupportTicketsManageAll)
            .WithName("AddInternalNote").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        tickets.MapPost("/{ticketId:guid}/waiting-for-customer", MarkWaitingForCustomerAsync)
            .RequireAuthorization(Permissions.SupportTicketsManageAll).WithName("MarkWaitingForCustomer")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        tickets.MapPost("/{ticketId:guid}/resolve", ResolveSupportTicketAsync).RequireAuthorization(Permissions.SupportTicketsManageAll)
            .WithName("ResolveSupportTicket").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        tickets.MapPost("/{ticketId:guid}/close", SupportTicketEndpoints.CloseSupportTicketAsync)
            .RequireAuthorization(Permissions.SupportTicketsManageAll).WithName("CloseSupportTicketAdmin")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        tickets.MapPost("/{ticketId:guid}/reopen", SupportTicketEndpoints.ReopenSupportTicketAsync)
            .RequireAuthorization(Permissions.SupportTicketsManageAll).WithName("ReopenSupportTicketAdmin")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        tickets.MapPost("/{ticketId:guid}/escalate-to-incident", EscalateTicketToIncidentAsync)
            .RequireAuthorization(Permissions.SupportTicketsManageAll).WithName("EscalateTicketToIncident")
            .Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Ok<PagedResult<SupportTicketResponse>>> GetAllSupportTicketsAsync(
        GetAllSupportTicketsQueryHandler handler, CancellationToken cancellationToken, int pageNumber = 1, int pageSize = DefaultPageSize)
    {
        var result = await handler.Handle(new GetAllSupportTicketsQuery(pageNumber, pageSize), cancellationToken);
        var response = new PagedResult<SupportTicketResponse>(
            result.Items.Select(SupportTicketResponse.FromEntity).ToList(), result.TotalCount, result.PageNumber, result.PageSize);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<SupportTicketDetailsResponse>, ProblemHttpResult>> GetSupportTicketDetailsAdminAsync(
        Guid ticketId, GetSupportTicketDetailsAdminQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetSupportTicketDetailsAdminQuery(ticketId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(SupportTicketDetailsResponse.FromDto(result.Value!)) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> AssignSupportTicketAsync(
        Guid ticketId, ClaimsPrincipal currentUser, AssignSupportTicketCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new AssignSupportTicketCommand(ticketId, SupportTicketEndpoints.CurrentUserId(currentUser)), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ReassignSupportTicketAsync(
        Guid ticketId, ReassignSupportTicketRequest request, ReassignSupportTicketCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ReassignSupportTicketCommand(ticketId, request.NewAdminUserId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> StartSupportTicketAsync(
        Guid ticketId, ClaimsPrincipal currentUser, StartSupportTicketCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new StartSupportTicketCommand(ticketId, SupportTicketEndpoints.CurrentUserId(currentUser)), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> AddAdminTicketMessageAsync(
        Guid ticketId, AddTicketMessageRequest request, ClaimsPrincipal currentUser, AddAdminTicketMessageCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new AddAdminTicketMessageCommand(ticketId, SupportTicketEndpoints.CurrentUserId(currentUser), request.Body),
            cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> AddInternalNoteAsync(
        Guid ticketId, AddTicketMessageRequest request, ClaimsPrincipal currentUser, AddInternalNoteCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new AddInternalNoteCommand(ticketId, SupportTicketEndpoints.CurrentUserId(currentUser), request.Body), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> MarkWaitingForCustomerAsync(
        Guid ticketId, ClaimsPrincipal currentUser, MarkWaitingForCustomerCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new MarkWaitingForCustomerCommand(ticketId, SupportTicketEndpoints.CurrentUserId(currentUser)), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ResolveSupportTicketAsync(
        Guid ticketId, ResolveSupportTicketRequest request, ClaimsPrincipal currentUser, ResolveSupportTicketCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new ResolveSupportTicketCommand(ticketId, SupportTicketEndpoints.CurrentUserId(currentUser), request.Resolution),
            cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> EscalateTicketToIncidentAsync(
        Guid ticketId, EscalateTicketToIncidentRequest request, ClaimsPrincipal currentUser, EscalateTicketToIncidentCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new EscalateTicketToIncidentCommand(
                ticketId, SupportTicketEndpoints.CurrentUserId(currentUser), request.IncidentType, request.Severity,
                request.TitleOverride),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }
}

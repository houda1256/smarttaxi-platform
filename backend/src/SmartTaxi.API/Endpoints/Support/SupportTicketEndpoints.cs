using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Support;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Support.Commands.AddTicketMessage;
using SmartTaxi.Application.Support.Commands.CloseSupportTicket;
using SmartTaxi.Application.Support.Commands.CreateSupportTicket;
using SmartTaxi.Application.Support.Commands.ReopenSupportTicket;
using SmartTaxi.Application.Support.Queries.GetMySupportTickets;
using SmartTaxi.Application.Support.Queries.GetSupportTicketDetails;

namespace SmartTaxi.API.Endpoints.Support;

/// <summary>Requester self-service — every read/write here is scoped to the caller's own JWT subject claim. CloseSupportTicket/ReopenSupportTicket are also mapped under SupportTicketAdminEndpoints (same handler, same requester-or-admin check inside it) — see RoadsideAssistanceEndpoints' GetRoadsideAssistanceRequestDetails for the identical "one handler, two permission-gated routes" precedent.</summary>
public static class SupportTicketEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapSupportTicketEndpoints(this IEndpointRouteBuilder app)
    {
        var tickets = app.MapGroup("/api/support/tickets").WithTags("Support - Tickets");

        tickets.MapPost("/", CreateSupportTicketAsync).RequireAuthorization(Permissions.SupportTicketsCreateOwn)
            .WithName("CreateSupportTicket").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status403Forbidden);

        tickets.MapGet("/mine", GetMySupportTicketsAsync).RequireAuthorization(Permissions.SupportTicketsReadOwn)
            .WithName("GetMySupportTickets").Produces<PagedResult<SupportTicketResponse>>(StatusCodes.Status200OK);

        tickets.MapGet("/{ticketId:guid}", GetSupportTicketDetailsAsync).RequireAuthorization(Permissions.SupportTicketsReadOwn)
            .WithName("GetSupportTicketDetails").Produces<SupportTicketDetailsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        tickets.MapPost("/{ticketId:guid}/messages", AddTicketMessageAsync).RequireAuthorization(Permissions.SupportTicketsManageOwn)
            .WithName("AddTicketMessage").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        tickets.MapPost("/{ticketId:guid}/close", CloseSupportTicketAsync).RequireAuthorization(Permissions.SupportTicketsManageOwn)
            .WithName("CloseSupportTicket").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        tickets.MapPost("/{ticketId:guid}/reopen", ReopenSupportTicketAsync).RequireAuthorization(Permissions.SupportTicketsManageOwn)
            .WithName("ReopenSupportTicket").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    internal static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> CreateSupportTicketAsync(
        CreateSupportTicketRequest request, ClaimsPrincipal currentUser, CreateSupportTicketCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CreateSupportTicketCommand(
                CurrentUserId(currentUser), request.Category, request.Subject, request.Description, request.Priority,
                request.RelatedEntityType, request.RelatedEntityId),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Ok<PagedResult<SupportTicketResponse>>> GetMySupportTicketsAsync(
        ClaimsPrincipal currentUser, GetMySupportTicketsQueryHandler handler, CancellationToken cancellationToken, int pageNumber = 1,
        int pageSize = DefaultPageSize)
    {
        var result = await handler.Handle(new GetMySupportTicketsQuery(CurrentUserId(currentUser), pageNumber, pageSize), cancellationToken);
        var response = new PagedResult<SupportTicketResponse>(
            result.Items.Select(SupportTicketResponse.FromEntity).ToList(), result.TotalCount, result.PageNumber, result.PageSize);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<SupportTicketDetailsResponse>, ProblemHttpResult>> GetSupportTicketDetailsAsync(
        Guid ticketId, ClaimsPrincipal currentUser, GetSupportTicketDetailsQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetSupportTicketDetailsQuery(ticketId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(SupportTicketDetailsResponse.FromDto(result.Value!)) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> AddTicketMessageAsync(
        Guid ticketId, AddTicketMessageRequest request, ClaimsPrincipal currentUser, AddTicketMessageCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new AddTicketMessageCommand(ticketId, CurrentUserId(currentUser), request.Body), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    internal static async Task<Results<NoContent, ProblemHttpResult>> CloseSupportTicketAsync(
        Guid ticketId, ClaimsPrincipal currentUser, CloseSupportTicketCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CloseSupportTicketCommand(ticketId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    internal static async Task<Results<NoContent, ProblemHttpResult>> ReopenSupportTicketAsync(
        Guid ticketId, ClaimsPrincipal currentUser, ReopenSupportTicketCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ReopenSupportTicketCommand(ticketId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}

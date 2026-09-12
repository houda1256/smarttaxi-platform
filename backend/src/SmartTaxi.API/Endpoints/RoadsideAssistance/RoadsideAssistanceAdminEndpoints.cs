using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.RoadsideAssistance;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.RoadsideAssistance.Commands.DisputeRoadsideAssistanceRequest;
using SmartTaxi.Application.RoadsideAssistance.Commands.ExpireStaleRoadsideRequests;
using SmartTaxi.Application.RoadsideAssistance.Commands.ForceCancelRoadsideRequest;
using SmartTaxi.Application.RoadsideAssistance.Commands.SettleRoadsideAssistanceRequest;
using SmartTaxi.Application.RoadsideAssistance.Queries.GetAllRoadsideAssistanceRequests;

namespace SmartTaxi.API.Endpoints.RoadsideAssistance;

/// <summary>
/// Admin-only surface. Settlement is deliberately here, not under requester
/// self-service — mirrors Maintenance's SettleMaintenanceRequest (Admin-gated,
/// not self-service by the payer or payee); see the approved plan's
/// settlement-authorization decision (Q2).
/// </summary>
public static class RoadsideAssistanceAdminEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapRoadsideAssistanceAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var requests = app.MapGroup("/api/admin/roadside-requests").WithTags("Roadside Assistance Admin");

        requests.MapGet("/", GetAllRoadsideAssistanceRequestsAsync).RequireAuthorization(Permissions.RoadsideReadAll)
            .WithName("GetAllRoadsideAssistanceRequests").Produces<PagedResult<RoadsideAssistanceRequestResponse>>(StatusCodes.Status200OK);

        requests.MapPost("/{requestId:guid}/force-cancel", ForceCancelRoadsideRequestAsync).RequireAuthorization(Permissions.RoadsideManageAll)
            .WithName("ForceCancelRoadsideRequest").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapPost("/{requestId:guid}/settle", SettleRoadsideAssistanceRequestAsync).RequireAuthorization(Permissions.RoadsideManageAll)
            .WithName("SettleRoadsideAssistanceRequest").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapPost("/{requestId:guid}/dispute", DisputeRoadsideAssistanceRequestAsync).RequireAuthorization(Permissions.RoadsideManageAll)
            .WithName("DisputeRoadsideAssistanceRequest").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapPost("/expire-sweep", ExpireStaleRoadsideRequestsAsync).RequireAuthorization(Permissions.RoadsideManageAll)
            .WithName("ExpireStaleRoadsideRequests").Produces<int>(StatusCodes.Status200OK);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Ok<PagedResult<RoadsideAssistanceRequestResponse>>> GetAllRoadsideAssistanceRequestsAsync(
        GetAllRoadsideAssistanceRequestsQueryHandler handler, CancellationToken cancellationToken, int pageNumber = 1, int pageSize = DefaultPageSize)
    {
        var result = await handler.Handle(new GetAllRoadsideAssistanceRequestsQuery(pageNumber, pageSize), cancellationToken);
        var response = new PagedResult<RoadsideAssistanceRequestResponse>(
            result.Items.Select(RoadsideAssistanceRequestResponse.FromEntity).ToList(), result.TotalCount, result.PageNumber, result.PageSize);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ForceCancelRoadsideRequestAsync(
        Guid requestId, ForceCancelRoadsideRequestRequest request, ClaimsPrincipal currentUser, ForceCancelRoadsideRequestCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new ForceCancelRoadsideRequestCommand(requestId, CurrentUserId(currentUser), request.Reason), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> SettleRoadsideAssistanceRequestAsync(
        Guid requestId, ClaimsPrincipal currentUser, SettleRoadsideAssistanceRequestCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new SettleRoadsideAssistanceRequestCommand(requestId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DisputeRoadsideAssistanceRequestAsync(
        Guid requestId, ClaimsPrincipal currentUser, DisputeRoadsideAssistanceRequestCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new DisputeRoadsideAssistanceRequestCommand(requestId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Ok<int>> ExpireStaleRoadsideRequestsAsync(
        ExpireStaleRoadsideRequestsCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ExpireStaleRoadsideRequestsCommand(), cancellationToken);
        return TypedResults.Ok(result.Value);
    }
}

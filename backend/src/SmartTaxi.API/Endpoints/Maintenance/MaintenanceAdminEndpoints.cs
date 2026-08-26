using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Maintenance;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Maintenance.Commands.ForceCancelMaintenanceRequest;
using SmartTaxi.Application.Maintenance.Commands.GenerateMaintenanceReminders;
using SmartTaxi.Application.Maintenance.Commands.SettleMaintenanceRequest;
using SmartTaxi.Application.Maintenance.Queries.GetAllMaintenanceRequests;

namespace SmartTaxi.API.Endpoints.Maintenance;

/// <summary>
/// Admin-only surface. Settlement is deliberately here, not under owner
/// self-service — mirrors Advertising's SettleCampaignBudget (Admin-gated,
/// not self-service by the payer or payee); see the Module 9 plan's
/// settlement-authorization decision.
/// </summary>
public static class MaintenanceAdminEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapMaintenanceAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var requests = app.MapGroup("/api/admin/maintenance/requests").WithTags("Maintenance Admin - Requests");

        requests.MapGet("/", GetAllMaintenanceRequestsAsync).RequireAuthorization(Permissions.MaintenanceReadAll)
            .WithName("GetAllMaintenanceRequests").Produces<PagedResult<MaintenanceRequestResponse>>(StatusCodes.Status200OK);

        requests.MapPost("/{requestId:guid}/force-cancel", ForceCancelMaintenanceRequestAsync).RequireAuthorization(Permissions.MaintenanceManageAll)
            .WithName("ForceCancelMaintenanceRequest").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        requests.MapPost("/{requestId:guid}/settle", SettleMaintenanceRequestAsync).RequireAuthorization(Permissions.MaintenanceManageAll)
            .WithName("SettleMaintenanceRequest").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        var operations = app.MapGroup("/api/admin/maintenance/operations").WithTags("Maintenance Admin - Operations")
            .RequireAuthorization(Permissions.MaintenanceManageAll);

        operations.MapPost("/generate-reminders", GenerateMaintenanceRemindersAsync).WithName("GenerateMaintenanceReminders")
            .Produces<int>(StatusCodes.Status200OK);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Ok<PagedResult<MaintenanceRequestResponse>>> GetAllMaintenanceRequestsAsync(
        GetAllMaintenanceRequestsQueryHandler handler, CancellationToken cancellationToken, int pageNumber = 1, int pageSize = DefaultPageSize)
    {
        var result = await handler.Handle(new GetAllMaintenanceRequestsQuery(pageNumber, pageSize), cancellationToken);
        var response = new PagedResult<MaintenanceRequestResponse>(
            result.Items.Select(MaintenanceRequestResponse.FromEntity).ToList(), result.TotalCount, result.PageNumber, result.PageSize);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ForceCancelMaintenanceRequestAsync(
        Guid requestId, ForceCancelMaintenanceRequestRequest request, ClaimsPrincipal currentUser, ForceCancelMaintenanceRequestCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new ForceCancelMaintenanceRequestCommand(requestId, CurrentUserId(currentUser), request.Reason), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> SettleMaintenanceRequestAsync(
        Guid requestId, ClaimsPrincipal currentUser, SettleMaintenanceRequestCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new SettleMaintenanceRequestCommand(requestId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Ok<int>> GenerateMaintenanceRemindersAsync(
        GenerateMaintenanceRemindersCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GenerateMaintenanceRemindersCommand(), cancellationToken);
        return TypedResults.Ok(result.Value);
    }
}

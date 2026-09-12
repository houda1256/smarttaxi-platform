using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Administration;
using SmartTaxi.Application.Administration.Queries.GetAuditLogForTarget;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Domain.Administration.Enums;

namespace SmartTaxi.API.Endpoints.Administration;

public static class AuditLogEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapAuditLogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/users/{userId:guid}/audit-log", GetAuditLogForUserAsync)
            .WithName("GetAuditLogForUser")
            .WithTags("Administration - Audit Log")
            .RequireAuthorization(Permissions.AdminAuditLogRead)
            .Produces<PagedResult<AuditLogEntryResponse>>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<Ok<PagedResult<AuditLogEntryResponse>>> GetAuditLogForUserAsync(
        Guid userId, GetAuditLogForTargetQueryHandler handler, CancellationToken cancellationToken,
        int pageNumber = 1, int pageSize = DefaultPageSize)
    {
        var result = await handler.Handle(
            new GetAuditLogForTargetQuery(AuditTargetType.User, userId, pageNumber, pageSize), cancellationToken);

        var response = new PagedResult<AuditLogEntryResponse>(
            result.Items.Select(AuditLogEntryResponse.FromEntity).ToList(), result.TotalCount, result.PageNumber, result.PageSize);

        return TypedResults.Ok(response);
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Analytics;
using SmartTaxi.Application.Analytics.Commands.CreateScheduledReport;
using SmartTaxi.Application.Analytics.Commands.DeactivateScheduledReport;
using SmartTaxi.Application.Analytics.Commands.ExportAnalyticsReport;
using SmartTaxi.Application.Analytics.Commands.ProcessDueScheduledReports;
using SmartTaxi.Application.Analytics.Commands.UpdateScheduledReport;
using SmartTaxi.Application.Analytics.Queries.GetScheduledReports;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Authorization;

namespace SmartTaxi.API.Endpoints.Analytics;

/// <summary>
/// Admin-only. Processing is explicit/ops-triggered only — POST /process-due
/// is called manually (or by an external scheduler outside this codebase);
/// no BackgroundService/Hangfire/Quartz is wired here.
/// </summary>
public static class ScheduledReportEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapScheduledReportEndpoints(this IEndpointRouteBuilder app)
    {
        var reports = app.MapGroup("/api/admin/analytics/scheduled-reports").WithTags("Analytics - Scheduled Reports")
            .RequireAuthorization(Permissions.AnalyticsReportsRead);

        reports.MapPost("/", CreateScheduledReportAsync).WithName("CreateScheduledReport")
            .Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status400BadRequest);

        reports.MapGet("/", GetScheduledReportsAsync).WithName("GetScheduledReports")
            .Produces<PagedResult<ScheduledReportDefinitionResponse>>(StatusCodes.Status200OK);

        reports.MapPut("/{id:guid}", UpdateScheduledReportAsync).WithName("UpdateScheduledReport")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound);

        reports.MapDelete("/{id:guid}", DeactivateScheduledReportAsync).WithName("DeactivateScheduledReport")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound);

        reports.MapPost("/process-due", ProcessDueScheduledReportsAsync).WithName("ProcessDueScheduledReports")
            .Produces<int>(StatusCodes.Status200OK);

        var exports = app.MapGroup("/api/admin/analytics/export").WithTags("Analytics - Export")
            .RequireAuthorization(Permissions.AnalyticsReportsExport);

        exports.MapPost("/", ExportAnalyticsReportAsync).WithName("ExportAnalyticsReport")
            .Produces<string>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> CreateScheduledReportAsync(
        CreateScheduledReportRequest request, CreateScheduledReportCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CreateScheduledReportCommand(request.Category, request.Frequency, request.RecipientUserId, request.FirstRunAtUtc),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Ok<PagedResult<ScheduledReportDefinitionResponse>>> GetScheduledReportsAsync(
        GetScheduledReportsQueryHandler handler, CancellationToken cancellationToken, int pageNumber = 1, int pageSize = DefaultPageSize)
    {
        var result = await handler.Handle(new GetScheduledReportsQuery(pageNumber, pageSize), cancellationToken);
        var response = new PagedResult<ScheduledReportDefinitionResponse>(
            result.Items.Select(ScheduledReportDefinitionResponse.FromEntity).ToList(), result.TotalCount, result.PageNumber,
            result.PageSize);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> UpdateScheduledReportAsync(
        Guid id, UpdateScheduledReportRequest request, UpdateScheduledReportCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new UpdateScheduledReportCommand(id, request.Category, request.Frequency, request.RecipientUserId), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeactivateScheduledReportAsync(
        Guid id, DeactivateScheduledReportCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new DeactivateScheduledReportCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Ok<int>> ProcessDueScheduledReportsAsync(
        ProcessDueScheduledReportsCommandHandler handler, CancellationToken cancellationToken)
    {
        var processedCount = await handler.Handle(new ProcessDueScheduledReportsCommand(), cancellationToken);
        return TypedResults.Ok(processedCount);
    }

    private static async Task<Results<Ok<string>, ProblemHttpResult>> ExportAnalyticsReportAsync(
        ExportAnalyticsReportRequest request, ClaimsPrincipal currentUser, ExportAnalyticsReportCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new ExportAnalyticsReportCommand(request.Category, request.FromUtc, request.ToUtc, CurrentUserId(currentUser), request.Format),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }
}

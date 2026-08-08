using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Payments;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Payments.Reports;
using SmartTaxi.Application.Payments.Reports.Abstractions;
using SmartTaxi.Application.Payments.Reports.Queries.ExportFinancialReport;
using SmartTaxi.Application.Payments.Reports.Queries.GetFinancialReport;

namespace SmartTaxi.API.Endpoints.Payments;

public static class FinancialReportAdminEndpoints
{
    public static IEndpointRouteBuilder MapFinancialReportAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/finance/reports").WithTags("Financial Reports").RequireAuthorization(Permissions.FinanceReportsRead);

        group.MapGet("/", GetReportAsync)
            .WithName("GetFinancialReport").Produces<FinancialReportResponse>(StatusCodes.Status200OK);

        group.MapGet("/export", ExportReportAsync)
            .WithName("ExportFinancialReport").Produces<string>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<Ok<FinancialReportResponse>> GetReportAsync(
        DateTime fromUtc, DateTime toUtc, Guid? actorId, Guid? vehicleId, Guid? fleetId, string? service, string? paymentMethod, string? status,
        GetFinancialReportQueryHandler handler, CancellationToken cancellationToken)
    {
        var filter = new FinancialReportFilter(fromUtc, toUtc, actorId, vehicleId, fleetId, service, paymentMethod, status);
        var result = await handler.Handle(new GetFinancialReportQuery(filter), cancellationToken);
        return TypedResults.Ok(FinancialReportResponse.FromResult(result));
    }

    private static async Task<Results<Ok<string>, ProblemHttpResult>> ExportReportAsync(
        DateTime fromUtc, DateTime toUtc, ReportExportFormat format, Guid? actorId, Guid? vehicleId, Guid? fleetId, string? service,
        string? paymentMethod, string? status, ExportFinancialReportQueryHandler handler, CancellationToken cancellationToken)
    {
        var filter = new FinancialReportFilter(fromUtc, toUtc, actorId, vehicleId, fleetId, service, paymentMethod, status);
        var storageKey = await handler.Handle(new ExportFinancialReportQuery(filter, format), cancellationToken);
        return TypedResults.Ok(storageKey);
    }
}

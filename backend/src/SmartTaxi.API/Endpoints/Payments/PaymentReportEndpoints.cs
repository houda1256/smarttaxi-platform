using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Payments.Queries.GetDriverRevenueReport;
using SmartTaxi.Application.Payments.Queries.GetOwnerRevenueReport;
using SmartTaxi.Application.Payments.Queries.GetPaymentStatistics;
using SmartTaxi.Application.Payments.Queries.GetRevenueSummary;

namespace SmartTaxi.API.Endpoints.Payments;

public static class PaymentReportEndpoints
{
    public static IEndpointRouteBuilder MapPaymentReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Financial Reports").RequireAuthorization(Permissions.PaymentsReport);

        group.MapGet("/revenue", GetRevenueSummaryAsync)
            .WithName("GetRevenueSummary").Produces<IReadOnlyCollection<RevenuePeriodSummary>>(StatusCodes.Status200OK);

        group.MapGet("/driver/{driverId:guid}", GetDriverRevenueAsync)
            .WithName("GetDriverRevenueReport").Produces<DriverRevenueReport>(StatusCodes.Status200OK);

        group.MapGet("/owner/{ownerId:guid}", GetOwnerRevenueAsync)
            .WithName("GetOwnerRevenueReport").Produces<OwnerRevenueReport>(StatusCodes.Status200OK);

        group.MapGet("/statistics", GetStatisticsAsync)
            .WithName("GetPaymentStatistics").Produces<PaymentStatisticsReport>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<Ok<IReadOnlyCollection<RevenuePeriodSummary>>> GetRevenueSummaryAsync(
        DateTime fromDate, DateTime toDate, RevenueReportGranularity granularity, GetRevenueSummaryQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetRevenueSummaryQuery(fromDate, toDate, granularity), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<DriverRevenueReport>> GetDriverRevenueAsync(
        Guid driverId, DateTime fromDate, DateTime toDate, GetDriverRevenueReportQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetDriverRevenueReportQuery(driverId, fromDate, toDate), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<OwnerRevenueReport>> GetOwnerRevenueAsync(
        Guid ownerId, DateTime fromDate, DateTime toDate, GetOwnerRevenueReportQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetOwnerRevenueReportQuery(ownerId, fromDate, toDate), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<PaymentStatisticsReport>> GetStatisticsAsync(
        DateTime fromDate, DateTime toDate, GetPaymentStatisticsQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetPaymentStatisticsQuery(fromDate, toDate), cancellationToken);
        return TypedResults.Ok(result);
    }
}

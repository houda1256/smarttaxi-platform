using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.Application.Analytics.Contracts;
using SmartTaxi.Application.Analytics.Queries.GetAdminDashboard;
using SmartTaxi.Application.Analytics.Queries.GetAdvertisingAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetFinancialAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetFleetAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetGrowthAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetMaintenanceAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetRideAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetRoadsideAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetSubscriptionAnalytics;
using SmartTaxi.Application.Analytics.Queries.GetSupportAnalytics;
using SmartTaxi.Application.Identity.Authorization;

namespace SmartTaxi.API.Endpoints.Analytics;

/// <summary>Entirely admin-only, read-only. Dashboard/Growth require analytics.dashboard.read; every per-domain bucket requires analytics.reports.read.</summary>
public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var dashboard = app.MapGroup("/api/admin/analytics").WithTags("Analytics")
            .RequireAuthorization(Permissions.AnalyticsDashboardRead);

        dashboard.MapGet("/dashboard", GetAdminDashboardAsync).WithName("GetAdminDashboard")
            .Produces<AdminDashboardSummary>(StatusCodes.Status200OK);

        dashboard.MapGet("/growth", GetGrowthAnalyticsAsync).WithName("GetGrowthAnalytics")
            .Produces<GrowthAnalyticsResult>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status400BadRequest);

        var reports = app.MapGroup("/api/admin/analytics").WithTags("Analytics").RequireAuthorization(Permissions.AnalyticsReportsRead);

        reports.MapGet("/rides", GetRideAnalyticsAsync).WithName("GetRideAnalytics")
            .Produces<RideAnalyticsSummary>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status400BadRequest);

        reports.MapGet("/fleet", GetFleetAnalyticsAsync).WithName("GetFleetAnalytics")
            .Produces<FleetAnalyticsSummary>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status400BadRequest);

        reports.MapGet("/subscriptions", GetSubscriptionAnalyticsAsync).WithName("GetSubscriptionAnalytics")
            .Produces<SubscriptionAnalyticsSummary>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status400BadRequest);

        reports.MapGet("/advertising", GetAdvertisingAnalyticsAsync).WithName("GetAdvertisingAnalytics")
            .Produces<AdvertisingAnalyticsSummary>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status400BadRequest);

        reports.MapGet("/maintenance", GetMaintenanceAnalyticsAsync).WithName("GetMaintenanceAnalytics")
            .Produces<MaintenanceAnalyticsSummary>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status400BadRequest);

        reports.MapGet("/roadside", GetRoadsideAnalyticsAsync).WithName("GetRoadsideAnalytics")
            .Produces<RoadsideAnalyticsSummary>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status400BadRequest);

        reports.MapGet("/support", GetSupportAnalyticsAsync).WithName("GetSupportAnalytics")
            .Produces<SupportAnalyticsSummary>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status400BadRequest);

        reports.MapGet("/financial", GetFinancialAnalyticsAsync).WithName("GetFinancialAnalytics")
            .Produces<FinancialAnalyticsSummary>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<Ok<AdminDashboardSummary>> GetAdminDashboardAsync(
        GetAdminDashboardQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetAdminDashboardQuery(), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<GrowthAnalyticsResult>, ProblemHttpResult>> GetGrowthAnalyticsAsync(
        DateTime fromUtc, DateTime toUtc, GetGrowthAnalyticsQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetGrowthAnalyticsQuery(fromUtc, toUtc), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<RideAnalyticsSummary>, ProblemHttpResult>> GetRideAnalyticsAsync(
        DateTime fromUtc, DateTime toUtc, GetRideAnalyticsQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetRideAnalyticsQuery(fromUtc, toUtc), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<FleetAnalyticsSummary>, ProblemHttpResult>> GetFleetAnalyticsAsync(
        DateTime fromUtc, DateTime toUtc, GetFleetAnalyticsQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetFleetAnalyticsQuery(fromUtc, toUtc), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<SubscriptionAnalyticsSummary>, ProblemHttpResult>> GetSubscriptionAnalyticsAsync(
        DateTime fromUtc, DateTime toUtc, GetSubscriptionAnalyticsQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetSubscriptionAnalyticsQuery(fromUtc, toUtc), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<AdvertisingAnalyticsSummary>, ProblemHttpResult>> GetAdvertisingAnalyticsAsync(
        DateTime fromUtc, DateTime toUtc, GetAdvertisingAnalyticsQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetAdvertisingAnalyticsQuery(fromUtc, toUtc), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<MaintenanceAnalyticsSummary>, ProblemHttpResult>> GetMaintenanceAnalyticsAsync(
        DateTime fromUtc, DateTime toUtc, GetMaintenanceAnalyticsQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetMaintenanceAnalyticsQuery(fromUtc, toUtc), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<RoadsideAnalyticsSummary>, ProblemHttpResult>> GetRoadsideAnalyticsAsync(
        DateTime fromUtc, DateTime toUtc, GetRoadsideAnalyticsQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetRoadsideAnalyticsQuery(fromUtc, toUtc), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<SupportAnalyticsSummary>, ProblemHttpResult>> GetSupportAnalyticsAsync(
        DateTime fromUtc, DateTime toUtc, GetSupportAnalyticsQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetSupportAnalyticsQuery(fromUtc, toUtc), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<FinancialAnalyticsSummary>, ProblemHttpResult>> GetFinancialAnalyticsAsync(
        DateTime fromUtc, DateTime toUtc, GetFinancialAnalyticsQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetFinancialAnalyticsQuery(fromUtc, toUtc), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }
}

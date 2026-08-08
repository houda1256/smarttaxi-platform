using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Fleet.UsageHistory;
using SmartTaxi.Application.Fleet.UsageHistory.Queries.GetDriverUsageHistory;
using SmartTaxi.Application.Fleet.UsageHistory.Queries.GetVehicleUsageHistory;
using SmartTaxi.Application.Identity.Authorization;

namespace SmartTaxi.API.Endpoints.Fleet;

public static class UsageHistoryEndpoints
{
    public static IEndpointRouteBuilder MapUsageHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fleet/usage-history")
            .WithTags("UsageHistory").RequireAuthorization(Permissions.FleetUsageReadOwn);

        group.MapGet("/vehicle/{vehicleId:guid}", GetForVehicleAsync)
            .WithName("GetVehicleUsageHistory")
            .Produces<IReadOnlyCollection<VehicleUsageRecordResponse>>(StatusCodes.Status200OK);

        group.MapGet("/driver/{driverId:guid}", GetForDriverAsync)
            .WithName("GetDriverUsageHistory")
            .Produces<IReadOnlyCollection<VehicleUsageRecordResponse>>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<Ok<IReadOnlyCollection<VehicleUsageRecordResponse>>> GetForVehicleAsync(
        Guid vehicleId, GetVehicleUsageHistoryQueryHandler handler, CancellationToken cancellationToken)
    {
        var records = await handler.Handle(new GetVehicleUsageHistoryQuery(vehicleId), cancellationToken);
        IReadOnlyCollection<VehicleUsageRecordResponse> response = records.Select(VehicleUsageRecordResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Ok<IReadOnlyCollection<VehicleUsageRecordResponse>>> GetForDriverAsync(
        Guid driverId, GetDriverUsageHistoryQueryHandler handler, CancellationToken cancellationToken)
    {
        var records = await handler.Handle(new GetDriverUsageHistoryQuery(driverId), cancellationToken);
        IReadOnlyCollection<VehicleUsageRecordResponse> response = records.Select(VehicleUsageRecordResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }
}

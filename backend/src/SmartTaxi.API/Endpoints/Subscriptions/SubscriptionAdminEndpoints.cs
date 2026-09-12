using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Subscriptions.Commands.ExpireDueSubscriptions;

namespace SmartTaxi.API.Endpoints.Subscriptions;

/// <summary>Manual expiration sweep — see ExpireDueSubscriptionsCommand's doc comment: no background scheduler exists yet in this codebase.</summary>
public static class SubscriptionAdminEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/subscriptions").WithTags("Subscriptions").RequireAuthorization(Permissions.SubscriptionManage);

        group.MapPost("/expire-due", ExpireDueAsync)
            .WithName("ExpireDueSubscriptions").Produces<int>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<Ok<int>> ExpireDueAsync(ExpireDueSubscriptionsCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ExpireDueSubscriptionsCommand(), cancellationToken);
        return TypedResults.Ok(result.Value);
    }
}
